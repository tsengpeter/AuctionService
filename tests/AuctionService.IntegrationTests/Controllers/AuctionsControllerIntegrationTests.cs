using System.Net.Http.Json;
using AuctionService.Api;
using AuctionService.Core.DTOs.Common;

namespace AuctionService.IntegrationTests.Controllers;

public class AuctionsControllerIntegrationTests : PostgreSqlTestContainer
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuctionsControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override the connection string for testing
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                        ["BiddingService:BaseUrl"] = "http://localhost:5002" // Mock or test server
                    });
                });

                // Ensure database is created and migrated
                builder.ConfigureServices(async services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
                    await dbContext.Database.MigrateAsync();
                });
            });
    }

    [Fact]
    public async Task CreateAuction_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateAuctionRequest
        {
            Name = "Test Auction",
            Description = "Test Description",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auctions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Title.Should().Be("Test Auction");
    }

    [Fact]
    public async Task GetAuctions_ReturnsActiveAuctions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a test auction first
        var createRequest = new CreateAuctionRequest
        {
            Name = "Active Auction",
            Description = "Active Description",
            StartingPrice = 50.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        var createResponse = await client.PostAsJsonAsync("/api/auctions", createRequest);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Act
        var response = await client.GetAsync("/api/auctions");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuctionListItemDto>>>();
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().Contain(item => item.Title == "Active Auction");
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ReturnsAuction()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a test auction first
        var createRequest = new CreateAuctionRequest
        {
            Name = "Detail Auction",
            Description = "Detail Description",
            StartingPrice = 75.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        var createResponse = await client.PostAsJsonAsync("/api/auctions", createRequest);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var createdResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        var auctionId = createdResult.Data.Id;

        // Act
        var response = await client.GetAsync($"/api/auctions/{auctionId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(auctionId);
        result.Data.Title.Should().Be("Detail Auction");
    }

    [Fact]
    public async Task GetAuctions_WithStatusActive_ExcludesEndedAuctions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create an active auction
        var activeRequest = new CreateAuctionRequest
        {
            Name = "Active Auction",
            Description = "Active",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        var activeResponse = await client.PostAsJsonAsync("/api/auctions", activeRequest);
        activeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Create an auction that will end soon
        var endedRequest = new CreateAuctionRequest
        {
            Name = "Soon Ended Auction",
            Description = "Will end soon",
            StartingPrice = 200.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddSeconds(2)
        };

        var endedResponse = await client.PostAsJsonAsync("/api/auctions", endedRequest);
        endedResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Wait for the auction to end
        await Task.Delay(3000);

        // Act - Get auctions with status=Active
        var response = await client.GetAsync("/api/auctions?status=Active");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuctionListItemDto>>>();
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Items.Should().Contain(item => item.Title == "Active Auction");
        result.Data.Items.Should().NotContain(item => item.Title == "Soon Ended Auction");
    }

    [Fact]
    public async Task CreateAuction_WithEndTimeInPast_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateAuctionRequest
        {
            Name = "Invalid Auction",
            Description = "Invalid",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddMinutes(-5) // End time in the past
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auctions", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StatusValidation_AuctionEndsAutomatically_AfterEndTime()
    {
        // Arrange
        var client = _factory.CreateClient();
        var endTime = DateTime.UtcNow.AddSeconds(2);

        var request = new CreateAuctionRequest
        {
            Name = "Short Auction",
            Description = "Ends quickly",
            StartingPrice = 50.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddMinutes(-1),
            EndTime = endTime
        };

        // Create auction
        var createResponse = await client.PostAsJsonAsync("/api/auctions", request);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var createdResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        var auctionId = createdResult.Data.Id;

        // Wait for auction to end
        await Task.Delay(3000); // Wait 3 seconds

        // Act - Get the auction
        var response = await client.GetAsync($"/api/auctions/{auctionId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        result.Data.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task StatusValidation_EndedAuctionsExcludedFromActiveFilter()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create an ended auction (EndTime in the past)
        var endedRequest = new CreateAuctionRequest
        {
            Name = "Ended Auction",
            Description = "Already ended",
            StartingPrice = 100.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow.AddMinutes(-30)
        };

        var endedResponse = await client.PostAsJsonAsync("/api/auctions", endedRequest);
        endedResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Act - Get auctions with status=Active
        var response = await client.GetAsync("/api/auctions?status=Active");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuctionListItemDto>>>();
        result.Data.Items.Should().NotContain(item => item.Name == "Ended Auction");
    }

    [Fact]
    public async Task StatusValidation_AuctionWithFutureStartTime_ReturnsPending()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new CreateAuctionRequest
        {
            Name = "Future Auction",
            Description = "Starts in future",
            StartingPrice = 75.00m,
            CategoryId = 1,
            StartTime = DateTime.UtcNow.AddHours(1), // Starts in 1 hour
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        // Create auction
        var createResponse = await client.PostAsJsonAsync("/api/auctions", request);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var createdResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        var auctionId = createdResult.Data.Id;

        // Act - Get the auction
        var response = await client.GetAsync($"/api/auctions/{auctionId}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuctionDetailDto>>();
        result.Data.Status.Should().Be(AuctionStatus.Pending);
    }
}