using FluentAssertions;
using MemberService.Application.DTOs.Auth;
using MemberService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace MemberService.IntegrationTests.API;

/// <summary>
/// Integration tests for AuthController endpoints.
/// Tests complete flow using Testcontainers PostgreSQL database.
/// </summary>
[Collection("PostgreSQL")]
public class AuthControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public AuthControllerTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("memberservice_test")
            .WithUsername("testuser")
            .WithPassword("TestPassword123!")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _container.StartAsync();

        // Create WebApplicationFactory with test database
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var dbContextDescriptor = services.FirstOrDefault(d =>
                        d.ServiceType.Name == "DbContextOptions`1");

                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    // Add new DbContext with test connection
                    services.AddDbContext<MemberDbContext>(options =>
                        options.UseNpgsql(_container.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();

        // Apply migrations
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MemberDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            _client.Dispose();
        }

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }

        await _container.StopAsync();
    }

    [Fact]
    public async Task Register_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "ValidPassword123!",
            Username = "new user"
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.Email.Should().Be(request.Email);
        content.Username.Should().Be(request.Username);
        content.AccessToken.Should().NotBeEmpty();
        content.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var email = "duplicate@example.com";
        var password = "ValidPassword123!";
        var username = "test user";

        var request1 = new RegisterRequest { Email = email, Password = password, Username = username };
        var request2 = new RegisterRequest { Email = email, Password = "DifferentPassword123!", Username = "different user" };

        // Act - First registration should succeed
        var response1 = await _client!.PostAsJsonAsync("/api/auth/register", request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second registration with same email should fail
        var response2 = await _client!.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409 Conflict
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "not-an-email",
            Password = "ValidPassword123!",
            Username = "test user"
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturn400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Short1",
            Username = "test user"
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidUsername_ShouldReturn400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "ValidPassword123!",
            Username = "ab" // Too short
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200()
    {
        // Arrange - First register a user
        var email = "login@example.com";
        var password = "ValidPassword123!";
        var username = "login user";

        var registerRequest = new RegisterRequest { Email = email, Password = password, Username = username };
        await _client!.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Login with same credentials
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content.Email.Should().Be(email);
        content.Username.Should().Be(username);
        content.AccessToken.Should().NotBeEmpty();
        content.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_ShouldReturn401()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        // Arrange - First register a user
        var email = "wrongpass@example.com";
        var password = "CorrectPassword123!";
        var username = "test user";

        var registerRequest = new RegisterRequest { Email = email, Password = password, Username = username };
        await _client!.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Act - Login with wrong password
        var loginRequest = new LoginRequest { Email = email, Password = "WrongPassword123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturn400()
    {
        // Arrange
        var request = new LoginRequest { Email = "", Password = "Password123!" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturn200()
    {
        // Arrange - Register and login to get tokens
        var registerRequest = new RegisterRequest
        {
            Email = "refresh@example.com",
            Password = "ValidPassword123!",
            Username = "refresh user"
        };
        
        var registerResponse = await _client!.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act - Refresh token
        var refreshRequest = new RefreshTokenRequest { RefreshToken = authResponse!.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        content.Should().NotBeNull();
        content.AccessToken.Should().NotBeEmpty();
        content.AccessToken.Should().NotBe(authResponse.AccessToken); // Should be different token
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturn400()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid_token" };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturn204()
    {
        // Arrange - Register to get token
        var registerRequest = new RegisterRequest
        {
            Email = "logout@example.com",
            Password = "ValidPassword123!",
            Username = "logout user"
        };
        
        var registerResponse = await _client!.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Add authorization header for logout
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act - Logout
        var logoutRequest = new LogoutRequest { RefreshToken = authResponse.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked - refresh should fail
        var refreshRequest = new RefreshTokenRequest { RefreshToken = authResponse.RefreshToken };
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithoutAuthorization_ShouldReturn401()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "some_token" };

        // Act - No authorization header
        var response = await _client!.PostAsJsonAsync("/api/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterAndLogin_CompleteFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var email = "flow@example.com";
        var password = "ValidPassword123!";
        var username = "flow user";

        // Act 1 - Register
        var registerRequest = new RegisterRequest { Email = email, Password = password, Username = username };
        var registerResponse = await _client!.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerData = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Act 2 - Login with same credentials
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        registerData!.Email.Should().Be(loginData!.Email);
        registerData.Username.Should().Be(loginData.Username);
        registerData.UserId.Should().Be(loginData.UserId);
        // Tokens should be different
        registerData.AccessToken.Should().NotBe(loginData.AccessToken);
        registerData.RefreshToken.Should().NotBe(loginData.RefreshToken);
    }
}
