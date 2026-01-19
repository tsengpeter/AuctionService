using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using MemberService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemberService.Infrastructure.Services;

/// <summary>
/// AWS SNS SMS service implementation
/// </summary>
public class AwsSnsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSnsService> _logger;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly string _senderId;

    public AwsSnsService(IConfiguration configuration, ILogger<AwsSnsService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        var accessKey = configuration["Aws:AccessKey"] ?? throw new InvalidOperationException("Aws:AccessKey is not configured");
        var secretKey = configuration["Aws:SecretKey"] ?? throw new InvalidOperationException("Aws:SecretKey is not configured");
        var region = configuration["Aws:Region"] ?? "ap-northeast-1"; // Tokyo
        _senderId = configuration["Sms:SenderId"] ?? "AuctionSvc";

        // Create SNS client
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        _snsClient = new AmazonSimpleNotificationServiceClient(accessKey, secretKey, regionEndpoint);
    }

    /// <summary>
    /// Send verification code via SMS
    /// </summary>
    public async Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure phone number is in E.164 format (e.g., +886912345678)
            var formattedPhone = FormatPhoneNumber(phoneNumber);

            var message = BuildSmsMessage(code);

            var publishRequest = new PublishRequest
            {
                PhoneNumber = formattedPhone,
                Message = message,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "AWS.SNS.SMS.SenderID",
                        new MessageAttributeValue
                        {
                            StringValue = _senderId,
                            DataType = "String"
                        }
                    },
                    {
                        "AWS.SNS.SMS.SMSType",
                        new MessageAttributeValue
                        {
                            StringValue = "Transactional", // High priority for OTP
                            DataType = "String"
                        }
                    }
                }
            };

            var response = await _snsClient.PublishAsync(publishRequest, cancellationToken);

            _logger.LogInformation("SMS verification code sent successfully to {PhoneNumber}. MessageId: {MessageId}", 
                MaskPhoneNumber(formattedPhone), response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS verification code to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            throw new InvalidOperationException($"Failed to send SMS: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Format phone number to E.164 format
    /// </summary>
    private string FormatPhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // If doesn't start with +, assume Taiwan (+886) if starts with 0, otherwise add +
        if (!phoneNumber.StartsWith("+"))
        {
            if (digitsOnly.StartsWith("0"))
            {
                // Taiwan mobile: 0912345678 -> +886912345678
                digitsOnly = "886" + digitsOnly.Substring(1);
            }
            return "+" + digitsOnly;
        }

        return phoneNumber;
    }

    /// <summary>
    /// Build SMS message
    /// </summary>
    private string BuildSmsMessage(string code)
    {
        return $"Your Auction Service verification code is: {code}. Valid for 5 minutes. Do not share this code.";
    }

    /// <summary>
    /// Mask phone number for logging (show only last 4 digits)
    /// </summary>
    private string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
            return "****";

        return "****" + phoneNumber.Substring(phoneNumber.Length - 4);
    }
}
