using System.Text;
using AuctionService.Api.Middleware;
using AuctionService.Shared;
using Member;
using Member.Infrastructure.Persistence;
using Auction;
using Auction.Infrastructure.Persistence;
using Bidding;
using Bidding.Infrastructure.Persistence;
using Ordering;
using Ordering.Infrastructure.Persistence;
using Notification;
using Notification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── T017c: Structured Logging ────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole(opts =>
    {
        opts.JsonWriterOptions = new JsonWriterOptions { Indented = true };
    });
}

// ── Shared Services ──────────────────────────────────────────────────────
builder.Services.AddSharedServices();

// ── Module DI ────────────────────────────────────────────────────────────
builder.Services.AddMemberModule(builder.Configuration);
builder.Services.AddAuctionModule(builder.Configuration);
builder.Services.AddBiddingModule(builder.Configuration);
builder.Services.AddOrderingModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Configuration);

// ── T017b: JWT Authentication ────────────────────────────────────────────
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters.");

var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "AuctionService";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "AuctionService";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                var response = ApiResponseFactory.Fail("Unauthorized.", 401);
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await ctx.Response.WriteAsync(json);
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                var response = ApiResponseFactory.Fail("Forbidden.", 403);
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await ctx.Response.WriteAsync(json);
            }
        };
    });

builder.Services.AddAuthorization();

// ── Health Checks (T020) ─────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MemberDbContext>("member-db")
    .AddDbContextCheck<AuctionDbContext>("auction-db")
    .AddDbContextCheck<BiddingDbContext>("bidding-db")
    .AddDbContextCheck<OrderingDbContext>("ordering-db")
    .AddDbContextCheck<NotificationDbContext>("notification-db");

// ── Swagger (T048) — Non-Production only ─────────────────────────────────
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "AuctionService API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        });
        c.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });
}

builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ── T020: Health Check Endpoint ───────────────────────────────────────────
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description })
        };
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
});

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("AuctionService API starting in {Environment} environment", app.Environment.EnvironmentName);

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
