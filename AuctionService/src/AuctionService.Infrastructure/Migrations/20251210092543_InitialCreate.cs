using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AuctionService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponseCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MessageZhTw = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MessageEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StartingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auctions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Follows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Follows_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "電子產品" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "家具" },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "收藏品" },
                    { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "藝術品" },
                    { 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "服飾配件" },
                    { 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "書籍" },
                    { 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "運動用品" },
                    { 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 99, true, "其他" }
                });

            migrationBuilder.InsertData(
                table: "ResponseCodes",
                columns: new[] { "Code", "Category", "CreatedAt", "MessageEn", "MessageZhTw", "Name", "Severity" },
                values: new object[,]
                {
                    { "AUCTION_CREATED", "Success", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Auction created successfully", "商品建立成功", "商品已建立", "Info" },
                    { "AUCTION_DELETED", "Success", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Auction deleted successfully", "商品刪除成功", "商品已刪除", "Info" },
                    { "AUCTION_HAS_BIDS", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot edit or delete auction with existing bids", "已有出價的商品無法編輯或刪除", "商品已有出價", "Warning" },
                    { "AUCTION_INVALID_END_TIME", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "End time must be at least 1 hour from now", "結束時間必須至少在 1 小時之後", "結束時間無效", "Warning" },
                    { "AUCTION_NOT_FOUND", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Auction not found", "查無此商品", "商品不存在", "Warning" },
                    { "AUCTION_UNAUTHORIZED", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Unauthorized to access this auction", "無權限操作此商品", "無權限操作", "Error" },
                    { "AUCTION_UPDATED", "Success", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Auction updated successfully", "商品更新成功", "商品已更新", "Info" },
                    { "BIDDING_SERVICE_UNAVAILABLE", "ServerError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bidding service is unavailable", "競標服務暫時無法使用", "競標服務無法使用", "Error" },
                    { "FOLLOW_ALREADY_EXISTS", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Already following this auction", "已在追蹤清單中", "已追蹤", "Info" },
                    { "FOLLOW_LIMIT_EXCEEDED", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Follow limit exceeded (500)", "追蹤商品數量已達上限 (500)", "追蹤數量超過限制", "Warning" },
                    { "FOLLOW_OWN_AUCTION", "ClientError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cannot follow your own auction", "無法追蹤自己的商品", "無法追蹤自己的商品", "Warning" },
                    { "INTERNAL_SERVER_ERROR", "ServerError", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Internal server error, please try again later", "伺服器發生錯誤，請稍後再試", "內部伺服器錯誤", "Error" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_CategoryId",
                table: "Auctions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_EndTime",
                table: "Auctions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_UserId_CreatedAt",
                table: "Auctions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name_Unique",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Follows_AuctionId",
                table: "Follows",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Follows_UserId_AuctionId_Unique",
                table: "Follows",
                columns: new[] { "UserId", "AuctionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Follows_UserId_CreatedAt",
                table: "Follows",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Follows");

            migrationBuilder.DropTable(
                name: "ResponseCodes");

            migrationBuilder.DropTable(
                name: "Auctions");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
