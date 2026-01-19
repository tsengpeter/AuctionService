using System.Text.Json;
using MemberService.Domain.Interfaces;
using MemberService.Domain.ValueObjects;
using StackExchange.Redis;

namespace MemberService.Infrastructure.Services;

/// <summary>
/// Redis-based verification code service
/// Manages 6-digit verification codes with 5-minute TTL
/// </summary>
public class VerificationCodeService : IVerificationCodeService
{
    private readonly IConnectionMultiplexer _redis;
    private const int VerificationCodeTtlSeconds = 300; // 5 minutes
    private const int ResendCooldownSeconds = 60; // 1 minute

    public VerificationCodeService(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    /// <summary>
    /// Generate and store a new verification code in Redis
    /// </summary>
    public async Task<string> GenerateAndStoreAsync(
        long userId,
        string verificationType,
        string deliveryMethod,
        string target,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildRedisKey(userId, verificationType, deliveryMethod);

        // Check if existing code is in cooldown period
        var existingCode = await GetAsync(userId, verificationType, deliveryMethod, cancellationToken);
        if (existingCode != null && !existingCode.CanResend)
        {
            throw new InvalidOperationException(
                $"Please wait {existingCode.SecondsUntilCanResend} seconds before requesting a new verification code");
        }

        // Generate 6-digit random code
        var code = GenerateRandomCode();

        // Create verification code object
        var verificationCode = new VerificationCode(code, target);

        // Serialize to JSON
        var json = JsonSerializer.Serialize(new
        {
            verificationCode.Code,
            verificationCode.AttemptCount,
            verificationCode.CreatedAt,
            verificationCode.Target
        });

        // Store in Redis with 5-minute TTL
        await db.StringSetAsync(key, json, TimeSpan.FromSeconds(VerificationCodeTtlSeconds));

        return code;
    }

    /// <summary>
    /// Retrieve verification code from Redis
    /// </summary>
    public async Task<VerificationCode?> GetAsync(
        long userId,
        string verificationType,
        string deliveryMethod,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildRedisKey(userId, verificationType, deliveryMethod);

        var value = await db.StringGetAsync(key);

        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Deserialize<VerificationCodeDto>(value.ToString());
            if (json == null)
            {
                return null;
            }

            var code = new VerificationCode(json.Code, json.Target);
            // Set the internal state to match the stored value
            for (int i = 0; i < json.AttemptCount; i++)
            {
                code.IncrementAttempt();
            }

            return code;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete verification code from Redis
    /// </summary>
    public async Task<bool> DeleteAsync(
        long userId,
        string verificationType,
        string deliveryMethod,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildRedisKey(userId, verificationType, deliveryMethod);

        return await db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Increment verification code attempt count
    /// </summary>
    public async Task<VerificationCode?> IncrementAttemptAsync(
        long userId,
        string verificationType,
        string deliveryMethod,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildRedisKey(userId, verificationType, deliveryMethod);

        // Get current code
        var code = await GetAsync(userId, verificationType, deliveryMethod, cancellationToken);
        if (code == null)
        {
            return null;
        }

        // Increment attempt
        code.IncrementAttempt();

        // If max attempts exceeded, delete the code
        if (code.IsMaxAttemptsExceeded)
        {
            await db.KeyDeleteAsync(key);
            return code;
        }

        // Update in Redis
        var json = JsonSerializer.Serialize(new
        {
            code.Code,
            code.AttemptCount,
            code.CreatedAt,
            code.Target
        });

        // Get remaining TTL
        var ttl = await db.KeyTimeToLiveAsync(key);
        var timeSpan = ttl.HasValue ? ttl.Value : TimeSpan.FromSeconds(VerificationCodeTtlSeconds);

        await db.StringSetAsync(key, json, timeSpan);

        return code;
    }

    private string BuildRedisKey(long userId, string verificationType, string deliveryMethod)
    {
        return $"verification:{userId}:{verificationType.ToLower()}:{deliveryMethod.ToLower()}";
    }

    private string GenerateRandomCode()
    {
        var random = new Random();
        return random.Next(0, 1000000).ToString("D6");
    }

    /// <summary>
    /// DTO for serialization/deserialization
    /// </summary>
    private class VerificationCodeDto
    {
        public required string Code { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string Target { get; set; }
    }
}
