using AuctionService.Api.Extensions;
using AuctionService.Shared.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 設定 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", // React 開發服務器
                "http://localhost:4200", // Angular 開發服務器
                "https://your-frontend-domain.com" // 生產前端域名
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // 如果需要發送認證 cookie
    });

    // 開發環境允許所有來源
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

// 設定 Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 設定 JWT 認證 (非測試環境)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AuctionService",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AuctionService",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultDevelopmentKey12345678901234567890")),
                ClockSkew = TimeSpan.FromMinutes(5) // 允許 5 分鐘的時鐘偏差
            };
        });

    builder.Services.AddAuthorization();
}
builder.Services.AddOpenApi();

// 註冊應用程式服務
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

// 設定 Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 設定 FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// 自動運行資料庫遷移 (開發環境)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuctionService.Infrastructure.Data.AuctionDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
// Swagger UI (傳統 OpenAPI 文檔介面)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AuctionService API v1");
        options.RoutePrefix = "swagger"; // Swagger UI 在 /swagger
        options.DocumentTitle = "AuctionService API 文檔";
    });
    
    // 同時保留 Scalar 文檔 (現代化替代方案，在 /scalar/v1)
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// 使用中介軟體
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// 設定 CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
}

// 安全標頭
app.UseSecurityHeaders();

// HTTPS 重定向 (可通過環境變數 DisableHttpsRedirection 禁用)
var disableHttpsRedirection = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");
if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// 提供 OpenAPI 規格檔案
app.MapGet("/openapi/v1/openapi.yaml", async () =>
{
    var openApiPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "specs", "002-auction-service", "contracts", "openapi.yaml");
    if (File.Exists(openApiPath))
    {
        var yamlContent = await File.ReadAllTextAsync(openApiPath);
        return Results.Text(yamlContent, "application/yaml");
    }
    return Results.NotFound();
});

app.MapControllers();

app.Run();
