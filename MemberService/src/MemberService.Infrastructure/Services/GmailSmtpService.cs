using MemberService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace MemberService.Infrastructure.Services;

/// <summary>
/// Gmail SMTP email service implementation
/// </summary>
public class GmailSmtpService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailSmtpService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly string _senderName;

    public GmailSmtpService(IConfiguration configuration, ILogger<GmailSmtpService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        _smtpServer = configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _senderEmail = configuration["Email:SenderEmail"] ?? throw new InvalidOperationException("Email:SenderEmail is not configured");
        _senderPassword = configuration["Email:SenderPassword"] ?? throw new InvalidOperationException("Email:SenderPassword is not configured");
        _senderName = configuration["Email:SenderName"] ?? "Auction Service";
    }

    /// <summary>
    /// Send verification code email
    /// </summary>
    public async Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_senderEmail, _senderPassword),
                Timeout = 30000 // 30 seconds
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail, _senderName),
                Subject = "Your Verification Code",
                Body = BuildEmailBody(code),
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Verification code email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code email to {Email}", toEmail);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Build HTML email body
    /// </summary>
    private string BuildEmailBody(string code)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .code {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; letter-spacing: 5px; margin: 20px 0; padding: 15px; background-color: #fff; border: 2px dashed #4CAF50; border-radius: 5px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ color: #f44336; font-size: 14px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Email Verification</h1>
        </div>
        <div class=""content"">
            <p>Hello,</p>
            <p>You have requested to verify your email address. Please use the following verification code:</p>
            <div class=""code"">{code}</div>
            <p>This code will expire in <strong>5 minutes</strong>.</p>
            <p class=""warning"">⚠️ If you didn't request this code, please ignore this email and ensure your account is secure.</p>
        </div>
        <div class=""footer"">
            <p>© 2026 Auction Service. All rights reserved.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}
