using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NBomber.CSharp;

namespace AuctionService.LoadTest;

public class TestDataSeeder
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TestDataSeeder(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    public async Task SeedTestDataAsync()
    {
        Console.WriteLine("Starting test data seeding...");

        // Seed categories first (they are referenced by auctions)
        await SeedCategoriesAsync();

        // Seed users (for authentication, but we'll use mock JWT)
        var users = await SeedUsersAsync();

        // Seed auctions
        await SeedAuctionsAsync(users);

        // Seed follows
        await SeedFollowsAsync(users);

        Console.WriteLine("Test data seeding completed.");
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            new { Id = 1, Name = "電子產品", DisplayOrder = 1, IsActive = true },
            new { Id = 2, Name = "家具", DisplayOrder = 2, IsActive = true },
            new { Id = 3, Name = "收藏品", DisplayOrder = 3, IsActive = true },
            new { Id = 4, Name = "藝術品", DisplayOrder = 4, IsActive = true },
            new { Id = 5, Name = "服飾配件", DisplayOrder = 5, IsActive = true },
            new { Id = 6, Name = "書籍", DisplayOrder = 6, IsActive = true },
            new { Id = 7, Name = "運動用品", DisplayOrder = 7, IsActive = true },
            new { Id = 8, Name = "其他", DisplayOrder = 8, IsActive = true }
        };

        foreach (var category in categories)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/categories", category);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Seeded category: {category.Name}");
                }
                else
                {
                    Console.WriteLine($"Failed to seed category {category.Name}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding category {category.Name}: {ex.Message}");
            }
        }
    }

    private async Task<List<Guid>> SeedUsersAsync()
    {
        var users = new List<Guid>();
        var userCount = 1000;

        for (int i = 0; i < userCount; i++)
        {
            var userId = Guid.NewGuid();
            users.Add(userId);
            // Note: In a real scenario, you'd seed users through the user service
            // For load testing, we'll use mock JWT tokens
        }

        Console.WriteLine($"Generated {userCount} mock users");
        return users;
    }

    private async Task SeedAuctionsAsync(List<Guid> users)
    {
        var auctionCount = 10000;
        var categories = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var random = new Random();

        var tasks = new List<Task>();

        for (int i = 0; i < auctionCount; i++)
        {
            var userId = users[random.Next(users.Count)];
            var categoryId = categories[random.Next(categories.Length)];
            var startingPrice = random.Next(100, 10000);
            var startTime = DateTime.UtcNow.AddDays(random.Next(-30, 30));
            var endTime = startTime.AddDays(random.Next(1, 30));

            var auction = new
            {
                Name = $"Test Auction {i + 1}",
                Description = $"This is a test auction description for item {i + 1}",
                StartingPrice = startingPrice,
                CategoryId = categoryId,
                StartTime = startTime,
                EndTime = endTime,
                UserId = userId
            };

            tasks.Add(SeedAuctionAsync(auction, i + 1));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"Seeded {auctionCount} auctions");
    }

    private async Task SeedAuctionAsync(object auction, int index)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auctions", auction);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to seed auction {index}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding auction {index}: {ex.Message}");
        }
    }

    private async Task SeedFollowsAsync(List<Guid> users)
    {
        var followCount = 5000;
        var random = new Random();

        // First, get some auction IDs (this is simplified - in reality you'd query the database)
        var auctionIds = new List<Guid>();
        for (int i = 0; i < 1000; i++)
        {
            auctionIds.Add(Guid.NewGuid()); // Mock auction IDs
        }

        var tasks = new List<Task>();

        for (int i = 0; i < followCount; i++)
        {
            var userId = users[random.Next(users.Count)];
            var auctionId = auctionIds[random.Next(auctionIds.Count)];

            var follow = new
            {
                UserId = userId,
                AuctionId = auctionId
            };

            tasks.Add(SeedFollowAsync(follow, i + 1));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"Seeded {followCount} follows");
    }

    private async Task SeedFollowAsync(object follow, int index)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/follows", follow);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to seed follow {index}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding follow {index}: {ex.Message}");
        }
    }
}