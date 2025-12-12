using System.Diagnostics;
using System.Net.Http.Json;

namespace AuctionService.LoadTest;

class Program
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("AuctionService Load Test");
        Console.WriteLine("========================");

        // Wait for user to start the API
        Console.Write("Make sure AuctionService API is running on http://localhost:5106. Press Enter to start load test...");
        Console.ReadLine();

        // Warm up
        Console.WriteLine("Warming up...");
        for (int i = 0; i < 10; i++)
        {
            await _httpClient.GetAsync("http://localhost:5106/api/auctions");
        }

        // Load test configuration
        int concurrentUsers = 50;  // Number of concurrent users
        int testDurationSeconds = 60;  // Test duration in seconds
        int targetRequestsPerSecond = 100;  // Target RPS

        Console.WriteLine($"Starting load test with {concurrentUsers} concurrent users for {testDurationSeconds} seconds");
        Console.WriteLine($"Target: {targetRequestsPerSecond} requests/second");

        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();

        // Statistics
        int totalRequests = 0;
        int successfulRequests = 0;
        int failedRequests = 0;
        var responseTimes = new List<long>();

        // Semaphore to control concurrency
        var semaphore = new SemaphoreSlim(concurrentUsers);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(testDurationSeconds));

        // Start load test tasks
        for (int i = 0; i < concurrentUsers; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        Interlocked.Increment(ref totalRequests);
                        var requestStopwatch = Stopwatch.StartNew();

                        try
                        {
                            var response = await _httpClient.GetAsync("http://localhost:5106/api/auctions");
                            requestStopwatch.Stop();

                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Increment(ref successfulRequests);
                            }
                            else
                            {
                                Interlocked.Increment(ref failedRequests);
                            }

                            lock (responseTimes)
                            {
                                responseTimes.Add(requestStopwatch.ElapsedMilliseconds);
                            }
                        }
                        catch
                        {
                            requestStopwatch.Stop();
                            Interlocked.Increment(ref failedRequests);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    // Small delay to control request rate
                    await Task.Delay(10); // ~100 RPS per user would be too much, so throttle a bit
                }
            }));
        }

        // Wait for test duration
        await Task.Delay(testDurationSeconds * 1000);

        // Cancel all tasks
        cancellationTokenSource.Cancel();

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Calculate statistics
        double actualDurationSeconds = stopwatch.Elapsed.TotalSeconds;
        double actualRps = totalRequests / actualDurationSeconds;

        long avgResponseTime = responseTimes.Count > 0 ? (long)responseTimes.Average() : 0;
        long minResponseTime = responseTimes.Count > 0 ? responseTimes.Min() : 0;
        long maxResponseTime = responseTimes.Count > 0 ? responseTimes.Max() : 0;

        // Calculate percentiles
        responseTimes.Sort();
        long p50 = responseTimes.Count > 0 ? responseTimes[(int)(responseTimes.Count * 0.5)] : 0;
        long p95 = responseTimes.Count > 0 ? responseTimes[(int)(responseTimes.Count * 0.95)] : 0;
        long p99 = responseTimes.Count > 0 ? responseTimes[(int)(responseTimes.Count * 0.99)] : 0;

        // Results
        Console.WriteLine("\nLoad Test Results");
        Console.WriteLine("=================");
        Console.WriteLine($"Duration: {actualDurationSeconds:F2} seconds");
        Console.WriteLine($"Total Requests: {totalRequests}");
        Console.WriteLine($"Successful Requests: {successfulRequests}");
        Console.WriteLine($"Failed Requests: {failedRequests}");
        Console.WriteLine($"Actual RPS: {actualRps:F2}");
        Console.WriteLine($"Success Rate: {(double)successfulRequests / totalRequests * 100:F2}%");
        Console.WriteLine();
        Console.WriteLine("Response Times (ms):");
        Console.WriteLine($"  Average: {avgResponseTime}");
        Console.WriteLine($"  Min: {minResponseTime}");
        Console.WriteLine($"  Max: {maxResponseTime}");
        Console.WriteLine($"  50th percentile: {p50}");
        Console.WriteLine($"  95th percentile: {p95}");
        Console.WriteLine($"  99th percentile: {p99}");

        // Assessment
        Console.WriteLine("\nAssessment:");
        if (actualRps >= targetRequestsPerSecond && (double)successfulRequests / totalRequests >= 0.95)
        {
            Console.WriteLine("✅ PASSED: System can handle 100+ RPS with >95% success rate");
        }
        else
        {
            Console.WriteLine("❌ FAILED: System cannot handle the target load");
        }

        if (p95 <= 1000) // 1 second
        {
            Console.WriteLine("✅ PASSED: 95th percentile response time <= 1000ms");
        }
        else
        {
            Console.WriteLine("❌ FAILED: 95th percentile response time > 1000ms");
        }
    }
}