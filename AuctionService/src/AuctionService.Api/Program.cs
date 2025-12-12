using AuctionService.Shared.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.IO;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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
                ValidateIssuer = false, // 開發環境簡化設定
                ValidateAudience = false, // 開發環境簡化設定
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false, // 開發環境簡化設定
                // 在生產環境中應該設定適當的 Issuer, Audience 和 SigningKey
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
app.UseGlobalExceptionHandler();
app.UseRequestLogging();

app.UseHttpsRedirection();
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
