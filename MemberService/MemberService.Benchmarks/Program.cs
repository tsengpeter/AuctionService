using BenchmarkDotNet.Running;
using MemberService.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("MemberService Performance Benchmarks");
        Console.WriteLine("====================================");

        Console.WriteLine("\nRunning JWT Validation Benchmarks...");
        var jwtSummary = BenchmarkRunner.Run<JwtValidationBenchmarks>();

        Console.WriteLine("\nRunning Password Hashing Benchmarks...");
        var passwordSummary = BenchmarkRunner.Run<PasswordHashingBenchmarks>();

        Console.WriteLine("\nRunning Snowflake ID Benchmarks...");
        var snowflakeSummary = BenchmarkRunner.Run<SnowflakeIdBenchmarks>();

        Console.WriteLine("\nBenchmarks completed. Check the BenchmarkDotNet results above.");
        Console.WriteLine("Expected performance targets:");
        Console.WriteLine("- JWT validation: <50ms p95");
        Console.WriteLine("- API endpoints: <200ms p95");
        Console.WriteLine("- Password hashing: ~250-350ms (BCrypt work factor 12)");
        Console.WriteLine("- Snowflake ID generation: <1ms");
    }
}
