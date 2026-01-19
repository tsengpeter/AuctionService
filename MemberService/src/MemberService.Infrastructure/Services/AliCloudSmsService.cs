using MemberService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MemberService.Infrastructure.Services;

/// <summary>
/// AliCloud (Aliyun) SMS service implementation
/// 阿里雲短信服務實現
/// </summary>
public class AliCloudSmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AliCloudSmsService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _accessKeyId;
    private readonly string _accessKeySecret;
    private readonly string _signName;
    private readonly string _templateCode;
    private readonly string _endpoint;

    public AliCloudSmsService(
        IConfiguration configuration, 
        ILogger<AliCloudSmsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        // Load configuration
        _accessKeyId = configuration["AliCloud:AccessKeyId"] ?? throw new InvalidOperationException("AliCloud:AccessKeyId is not configured");
        _accessKeySecret = configuration["AliCloud:AccessKeySecret"] ?? throw new InvalidOperationException("AliCloud:AccessKeySecret is not configured");
        _signName = configuration["AliCloud:Sms:SignName"] ?? throw new InvalidOperationException("AliCloud:Sms:SignName is not configured");
        _templateCode = configuration["AliCloud:Sms:TemplateCode"] ?? throw new InvalidOperationException("AliCloud:Sms:TemplateCode is not configured");
        _endpoint = configuration["AliCloud:Sms:Endpoint"] ?? "https://dysmsapi.aliyuncs.com";
    }

    /// <summary>
    /// Send verification code via SMS
    /// </summary>
    public async Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            // Format phone number (remove spaces and dashes)
            var formattedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Build request parameters
            var parameters = new SortedDictionary<string, string>
            {
                { "AccessKeyId", _accessKeyId },
                { "Action", "SendSms" },
                { "Format", "JSON" },
                { "PhoneNumbers", formattedPhone },
                { "RegionId", "cn-hangzhou" },
                { "SignName", _signName },
                { "SignatureMethod", "HMAC-SHA1" },
                { "SignatureNonce", Guid.NewGuid().ToString() },
                { "SignatureVersion", "1.0" },
                { "TemplateCode", _templateCode },
                { "TemplateParam", $"{{\"code\":\"{code}\"}}" },
                { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                { "Version", "2017-05-25" }
            };

            // Calculate signature
            var signature = CalculateSignature(parameters, "POST");
            parameters.Add("Signature", signature);

            // Build request URL
            var queryString = string.Join("&", parameters.Select(p => $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));
            var requestUrl = $"{_endpoint}/?{queryString}";

            // Send request
            var response = await _httpClient.PostAsync(requestUrl, null, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AliCloud SMS API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new InvalidOperationException($"AliCloud SMS API returned error: {response.StatusCode}");
            }

            _logger.LogInformation("SMS verification code sent successfully to {PhoneNumber} via AliCloud", 
                MaskPhoneNumber(formattedPhone));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS verification code to {PhoneNumber} via AliCloud", 
                MaskPhoneNumber(phoneNumber));
            throw new InvalidOperationException($"Failed to send SMS: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Calculate signature for AliCloud API
    /// </summary>
    private string CalculateSignature(SortedDictionary<string, string> parameters, string method)
    {
        // Build canonical query string
        var canonicalizedQueryString = string.Join("&", 
            parameters.Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}"));

        // Build string to sign
        var stringToSign = $"{method}&{PercentEncode("/")}&{PercentEncode(canonicalizedQueryString)}";

        // Calculate HMAC-SHA1
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_accessKeySecret + "&"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Percent encode for AliCloud signature
    /// </summary>
    private string PercentEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || 
                (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.' || c == '~')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%');
                sb.Append(((int)c).ToString("X2"));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Mask phone number for logging
    /// </summary>
    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
            return "****";

        return "****" + phoneNumber.Substring(phoneNumber.Length - 4);
    }
}
