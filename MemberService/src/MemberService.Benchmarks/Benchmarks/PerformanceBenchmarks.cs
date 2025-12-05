using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MemberService.Application.Services;
using MemberService.Domain.Entities;
using MemberService.Domain.ValueObjects;
using MemberService.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace MemberService.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class JwtValidationBenchmarks
{
    private JwtTokenGenerator _jwtTokenGenerator;
    private string _validToken;
    private string _secretKey;

    [GlobalSetup]
    public void Setup()
    {
        _secretKey = "your-super-secret-jwt-key-min-32-chars-long-for-hs256-algorithm";
        _jwtTokenGenerator = new JwtTokenGenerator(_secretKey);

        // Create a test user
        var userId = 1234567890123456789L; // Snowflake ID
        var email = "test@example.com";

        // Generate a valid token
        _validToken = _jwtTokenGenerator.GenerateAccessToken(userId, email);
    }

    [Benchmark]
    public bool ValidateValidToken()
    {
        return _jwtTokenGenerator.ValidateToken(_validToken);
    }

    [Benchmark]
    public bool ValidateInvalidToken()
    {
        return _jwtTokenGenerator.ValidateToken("invalid.jwt.token");
    }
}

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class PasswordHashingBenchmarks
{
    private BCryptPasswordHasher _passwordHasher;
    private string _password;
    private long _userId;

    [GlobalSetup]
    public void Setup()
    {
        _passwordHasher = new BCryptPasswordHasher();
        _password = "SecurePassword123";
        _userId = 1234567890123456789L;
    }

    [Benchmark]
    public string HashPassword()
    {
        return _passwordHasher.HashPassword(_password, _userId);
    }

    [Benchmark]
    public bool VerifyCorrectPassword()
    {
        var hash = _passwordHasher.HashPassword(_password, _userId);
        return _passwordHasher.VerifyPassword(_password, _userId, hash);
    }

    [Benchmark]
    public bool VerifyIncorrectPassword()
    {
        var hash = _passwordHasher.HashPassword(_password, _userId);
        return _passwordHasher.VerifyPassword("WrongPassword", _userId, hash);
    }
}

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class SnowflakeIdBenchmarks
{
    private IdGen.IdGenerator _idGenerator;

    [GlobalSetup]
    public void Setup()
    {
        _idGenerator = new IdGen.IdGenerator(1);
    }

    [Benchmark]
    public long GenerateId()
    {
        return _idGenerator.CreateId();
    }
}