---
name: database-migrations
description: "Database migration best practices for AuctionService (EF Core 10 / PostgreSQL 16). Use when: creating or altering tables, adding columns or indexes, writing data migrations, planning zero-downtime schema changes, or running EF Core CLI commands for any module's DbContext."
argument-hint: "Optional: specify the module and change type (e.g. 'add BidderAlias column to Bidding module')"
---

# Database Migration Patterns — AuctionService (EF Core / PostgreSQL)

Safe, reversible database schema changes for this modular monolith.

## When to Activate

- Creating or altering database tables in any module
- Adding/removing columns or indexes
- Running data migrations (backfill, transform)
- Planning zero-downtime schema changes
- Running EF Core CLI commands

## Core Principles

1. **Every change is a migration** — never alter production databases manually
2. **Migrations are forward-only in production** — rollbacks use new forward migrations
3. **Schema and data migrations are separate** — never mix DDL and DML in one migration
4. **Test migrations against production-sized data** — a migration that works on 100 rows may lock on 10M
5. **Migrations are immutable once deployed** — never edit a migration that has run in production

## EF Core CLI Commands (Per Module)

Each module has its own `DbContext` and migrations folder. Always specify both `--project` and `--startup-project`.

```bash
# Add a migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Module>/<Module>.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj \
  --output-dir Infrastructure/Persistence/Migrations

# Apply migrations to database
dotnet ef database update \
  --project src/Modules/<Module>/<Module>.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# List migrations and applied status
dotnet ef migrations list \
  --project src/Modules/<Module>/<Module>.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj

# Remove last (unapplied) migration
dotnet ef migrations remove \
  --project src/Modules/<Module>/<Module>.csproj \
  --startup-project src/AuctionService.Api/AuctionService.Api.csproj
```

> Replace `<Module>` with `Member`, `Auction`, `Bidding`, `Ordering`, or `Notification`.

---

## Module DbContext — Schema Isolation

Each module's `DbContext` is scoped to its own PostgreSQL schema:

```csharp
public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction> Auctions => Set<Auction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("auction");  // ← schema isolation
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
```

| Module | PostgreSQL Schema |
|---|---|
| Member | `member` |
| Auction | `auction` |
| Bidding | `bidding` |
| Ordering | `ordering` |
| Notification | `notification` |

---

## Migration Safety Checklist

Before applying any migration:

- [ ] Migration was generated via `dotnet ef migrations add`, never hand-rolled
- [ ] No full table locks on large tables (use `CONCURRENTLY` for indexes via raw SQL)
- [ ] New `NOT NULL` columns have a `HasDefaultValue` or are added nullable + backfilled in a second migration
- [ ] Data backfill is a **separate** migration from the schema change
- [ ] Tested against a local Docker PostgreSQL instance (`docker compose up -d`)
- [ ] Rollback plan documented (forward migration that undoes the change)
- [ ] Never edit a migration that has been applied to any shared environment

## EF Core Migration Patterns

### Adding a Nullable Column (Safe)

```csharp
// In entity
public string? AvatarUrl { get; private set; }

// In IEntityTypeConfiguration
builder.Property(m => m.AvatarUrl).HasMaxLength(500);
```

Generated SQL (no lock, no table rewrite):
```sql
ALTER TABLE member.members ADD COLUMN avatar_url VARCHAR(500);
```

### Adding a NOT NULL Column Safely (Two-Migration Pattern)

```csharp
// Migration 1: Add as nullable
builder.Property(m => m.DisplayName).HasMaxLength(100).IsRequired(false);

// Migration 2 (after backfill): Make NOT NULL
migrationBuilder.Sql(
    "UPDATE member.members SET display_name = username WHERE display_name IS NULL");
migrationBuilder.AlterColumn<string>(
    name: "display_name", schema: "member", table: "members",
    nullable: false, oldNullable: true);
```

```
❌ BAD: Single migration — AddColumn NOT NULL with no default (rewrites every row)
✅ GOOD: nullable first → backfill → constraint in separate migrations
```

### Adding an Index Without Downtime

EF Core's `HasIndex()` generates a plain `CREATE INDEX` which locks writes on large tables. For production-safe indexes, use raw SQL in the migration:

```csharp
// In the generated migration file — replace the HasIndex output:
public partial class AddMemberEmailIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ✅ Non-blocking — must NOT be inside a transaction
        migrationBuilder.Sql(
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_member_email ON member.members (email);",
            suppressTransaction: true);  // ← required for CONCURRENTLY
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS member.idx_member_email;");
    }
}
```

### Renaming a Column (Zero-Downtime)

Never rename a column directly in production. Use the expand-contract pattern across three deployments:

```
Migration 1: Add new column (nullable)
  → EF entity exposes BOTH old and new properties
Migration 2 (data): Backfill new column from old
  → migrationBuilder.Sql("UPDATE ... SET new_col = old_col WHERE new_col IS NULL")
Deploy: App reads new column, stops writing old column
Migration 3: Drop old column
```

### Removing a Column Safely

```csharp
// Step 1: Remove property from entity + configuration, regenerate migration
// EF generates: migrationBuilder.DropColumn("legacy_status", ...)

// Step 2: BUT first — remove all C# references to the property and deploy
// Step 3: THEN apply the DropColumn migration

// ✅ Correct order:
//   Remove code → deploy → apply migration
// ❌ Wrong order:
//   Apply migration → deploy (app references missing column → runtime error)
```

### Large Data Migrations (Batch Update)

Never update millions of rows in a single transaction — it locks the table.

```csharp
// In a migration's Up() method — batch via raw SQL
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Schema change first (separate migration)
    // Then backfill in batches within this migration
    migrationBuilder.Sql(@"
        DO $$
        DECLARE
            batch_size INT := 10000;
            rows_updated INT;
        BEGIN
            LOOP
                UPDATE auction.auctions
                SET normalized_title = LOWER(title)
                WHERE id IN (
                    SELECT id FROM auction.auctions
                    WHERE normalized_title IS NULL
                    LIMIT batch_size
                    FOR UPDATE SKIP LOCKED
                );
                GET DIAGNOSTICS rows_updated = ROW_COUNT;
                EXIT WHEN rows_updated = 0;
                COMMIT;
            END LOOP;
        END $$;
    ");
}
```

## Zero-Downtime Migration Strategy (Expand-Contract)

For critical production changes:

```
Phase 1 — EXPAND
  Migration: add new column (nullable or with default)
  EF entity: expose new property alongside old
  Deploy: app writes to both old and new

Phase 2 — BACKFILL
  Migration: batch-update existing rows (separate migration)
  Verify data consistency

Phase 3 — CONTRACT
  Deploy: app reads/writes new column only
  Migration: drop old column
```

### Timeline Example

```
Day 1:  Migration 1 — add new_status column (nullable)
Day 1:  Deploy — app writes to both status and new_status
Day 2:  Migration 2 — backfill new_status from status
Day 3:  Deploy — app reads new_status only
Day 7:  Migration 3 — drop old status column
```

## Anti-Patterns

| Anti-Pattern | Why It Fails | Better Approach |
|-------------|-------------|-----------------|
| Running `dotnet ef database update` in production without review | Irreversible, no approval gate | Script SQL via `dotnet ef migrations script` and review first |
| Editing a migration that has been applied | Causes schema drift across environments | Create a new migration instead |
| NOT NULL column without default on existing table | Locks table, rewrites all rows | Add nullable → backfill → add constraint (three migrations) |
| `CREATE INDEX` without `CONCURRENTLY` on large table | Blocks all writes during build | Use `migrationBuilder.Sql("CREATE INDEX CONCURRENTLY...", suppressTransaction: true)` |
| Schema change + data backfill in one migration | Long transaction, hard to rollback | Separate into two migrations |
| Dropping column before removing EF/C# references | Runtime error on missing column | Remove code → deploy → then apply DropColumn migration |
| Using `dotnet ef database update` across modules in wrong order | FK-like dependency violations | Always apply in dependency order: Member → Auction → Bidding → Ordering → Notification |
