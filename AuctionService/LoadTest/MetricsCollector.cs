using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NBomber.Contracts;
using System.Threading;
using System.Net.Http;

namespace AuctionService.LoadTest;

public class MetricsCollector
{
    private readonly List<ResponseMetric> _responseMetrics = new();
    private readonly object _lock = new();
    private readonly Stopwatch _testStopwatch = new();

    public void StartCollection()
    {
        _testStopwatch.Start();
        Console.WriteLine("Started metrics collection");
    }

    public void StopCollection()
    {
        _testStopwatch.Stop();
        Console.WriteLine($"Stopped metrics collection after {_testStopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    public void RecordResponse(string scenarioName, Response<HttpResponseMessage> response)
    {
        // NBomber會自動收集所有Response的統計數據
        // 這裡只記錄額外的自定義指標
        var metric = new ResponseMetric
        {
            ScenarioName = scenarioName,
            Timestamp = DateTime.UtcNow,
            ResponseTime = response.SizeBytes, // 使用SizeBytes
            IsSuccess = !response.IsError,
            StatusCode = null, // StatusCode將從NBomber的統計報告中取得
            ErrorMessage = response.Message
        };

        lock (_lock)
        {
            _responseMetrics.Add(metric);
        }
    }

    public LoadTestResults GetResults()
    {
        lock (_lock)
        {
            var results = new LoadTestResults
            {
                TotalRequests = _responseMetrics.Count,
                SuccessfulRequests = _responseMetrics.Count(m => m.IsSuccess),
                FailedRequests = _responseMetrics.Count(m => !m.IsSuccess),
                TotalDuration = _testStopwatch.Elapsed,
                ResponseTimes = _responseMetrics.Select(m => m.ResponseTime).ToList()
            };

            if (results.ResponseTimes.Any())
            {
                results.P50ResponseTime = CalculatePercentile(results.ResponseTimes, 50);
                results.P95ResponseTime = CalculatePercentile(results.ResponseTimes, 95);
                results.P99ResponseTime = CalculatePercentile(results.ResponseTimes, 99);
                results.MinResponseTime = results.ResponseTimes.Min();
                results.MaxResponseTime = results.ResponseTimes.Max();
                results.AverageResponseTime = results.ResponseTimes.Average();
            }

            results.RequestsPerSecond = results.TotalRequests / results.TotalDuration.TotalSeconds;

            return results;
        }
    }

    public void PrintSummary()
    {
        var results = GetResults();

        Console.WriteLine("\n=== Load Test Results Summary ===");
        Console.WriteLine($"Total Requests: {results.TotalRequests}");
        Console.WriteLine($"Successful Requests: {results.SuccessfulRequests}");
        Console.WriteLine($"Failed Requests: {results.FailedRequests}");
        Console.WriteLine($"Success Rate: {results.SuccessRate:P2}");
        Console.WriteLine($"Total Duration: {results.TotalDuration.TotalSeconds:F2} seconds");
        Console.WriteLine($"Requests/Second: {results.RequestsPerSecond:F2}");
        Console.WriteLine("\nResponse Time Statistics:");
        Console.WriteLine($"Min: {results.MinResponseTime:F2} ms");
        Console.WriteLine($"P50: {results.P50ResponseTime:F2} ms");
        Console.WriteLine($"P95: {results.P95ResponseTime:F2} ms");
        Console.WriteLine($"P99: {results.P99ResponseTime:F2} ms");
        Console.WriteLine($"Max: {results.MaxResponseTime:F2} ms");
        Console.WriteLine($"Average: {results.AverageResponseTime:F2} ms");
    }

    private double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;

        var sorted = sortedValues.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}

public class ResponseMetric
{
    public required string ScenarioName { get; set; }
    public DateTime Timestamp { get; set; }
    public double ResponseTime { get; set; }
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoadTestResults
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<double> ResponseTimes { get; set; } = new();

    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    public double RequestsPerSecond { get; set; }

    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double AverageResponseTime { get; set; }
    public double P50ResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
}