using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> options,
        IWebHostEnvironment environment,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string? plainTextMessage = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail)
            || string.IsNullOrWhiteSpace(_settings.Username)
            || string.IsNullOrWhiteSpace(_settings.Password))
        {
            var fallbackMessage = plainTextMessage ?? htmlMessage;
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("SMTP chưa được cấu hình. Nội dung email test gửi tới {Email}: {Message}", toEmail, fallbackMessage);
                Console.WriteLine($"[EMAIL TEST] To: {toEmail}");
                Console.WriteLine($"[EMAIL TEST] Subject: {subject}");
                Console.WriteLine($"[EMAIL TEST] Message: {fallbackMessage}");
                return;
            }

            _logger.LogError("SMTP chưa được cấu hình đầy đủ nên không thể gửi email tới {Email}.", toEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await client.SendMailAsync(message);
    }
}
