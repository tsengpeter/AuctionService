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

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

// Register middlewares
builder.Services.AddScoped<GlobalExceptionHandler>();

// Add FluentValidation
// builder.Services.AddFluentValidationAutoValidation();
// builder.Services.AddValidatorsFromAssembly(typeof(MemberService.Application.Class1).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add custom middlewares
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.UseAuthorization();

app.MapControllers();

app.Run();
