using MemberService.API.Controllers;
using MemberService.API.Middlewares;
using MemberService.Application.Services;
using MemberService.Domain.Interfaces;
using MemberService.Infrastructure.IdGeneration;
using MemberService.Infrastructure.Persistence;
using MemberService.Infrastructure.Persistence.Repositories;
using MemberService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"))
        )
    };
});

// Add database context
builder.Services.AddDbContext<MemberDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MemberDb")));

// Register domain services
builder.Services.AddScoped<IIdGenerator, SnowflakeIdGenerator>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenGenerator>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var jwtSection = config.GetSection("Jwt");
    return new JwtTokenGenerator(
        jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"),
        jwtSection["Issuer"] ?? "MemberService",
        jwtSection["Audience"] ?? "MemberService",
        int.Parse(jwtSection["ExpiryInMinutes"] ?? "60")
    );
});
builder.Services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Register middlewares
builder.Services.AddScoped<GlobalExceptionHandler>();

// Add FluentValidation
// builder.Services.AddValidatorsFromAssembly(typeof(MemberService.Application.Class1).Assembly);
// builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

// Run database migrations (skip in test environment)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MemberDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    if (!env.IsEnvironment("Testing"))
    {
        try
        {
            Console.WriteLine("Starting database migration...");
            dbContext.Database.Migrate();
            Console.WriteLine("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database migration failed: {ex.Message}");
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MemberService API V1");
        c.RoutePrefix = string.Empty; // 設定 Swagger UI 在根路徑
    });
}

// app.UseHttpsRedirection(); // Temporarily disabled for debugging

// Add custom middlewares
app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("Starting web server...");
Console.WriteLine($"Listening on http://localhost:5191");
Console.WriteLine("Navigate to http://localhost:5191 to view Swagger UI");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
