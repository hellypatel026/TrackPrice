using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TrackPrice.Services
{
    public class EmailNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(
            IConfiguration configuration,
            ILogger<EmailNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPriceAlertEmailAsync(
            string recipientEmail,
            string productName,
            decimal targetPrice,
            decimal currentPrice,
            string? productUrl)
        {
            var smtpHost =
                _configuration["EmailSettings:SmtpHost"];

            var smtpPort =
                _configuration.GetValue<int>(
                    "EmailSettings:SmtpPort");

            var smtpUsername =
                _configuration["EmailSettings:SmtpUsername"];

            var smtpPassword =
                _configuration["EmailSettings:SmtpPassword"];

            var senderEmail =
                _configuration["EmailSettings:SenderEmail"];

            var senderName =
                _configuration["EmailSettings:SenderName"];

            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError(
                    "Email settings are not configured.");

                return;
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    senderName ?? "TrackPrice",
                    senderEmail));

            message.To.Add(
                new MailboxAddress(
                    recipientEmail,
                    recipientEmail));

            message.Subject =
                $"Price Alert Triggered - {productName}";

            var productLink = string.IsNullOrWhiteSpace(productUrl)
                ? ""
                : $"""
                    <p>
                        <a href="{productUrl}">
                            View Product
                        </a>
                    </p>
                    """;

            message.Body = new BodyBuilder
            {
                HtmlBody = $"""
                    <html>
                    <body>
                        <h2>Price Alert Triggered!</h2>

                        <p>
                            The price of
                            <strong>{productName}</strong>
                            has reached your target price.
                        </p>

                        <table cellpadding="8">
                            <tr>
                                <td><strong>Target Price:</strong></td>
                                <td>₹{targetPrice:N2}</td>
                            </tr>

                            <tr>
                                <td><strong>Current Price:</strong></td>
                                <td>₹{currentPrice:N2}</td>
                            </tr>
                        </table>

                        {productLink}

                        <p>
                            This notification was sent by TrackPrice.
                        </p>
                    </body>
                    </html>
                    """
            }.ToMessageBody();

            try
            {
                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    smtpHost,
                    smtpPort,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    smtpUsername,
                    smtpPassword);

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "Price alert email sent to {Email} " +
                    "for product {ProductName}.",
                    recipientEmail,
                    productName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send price alert email " +
                    "to {Email}.",
                    recipientEmail);
            }
        }
    }
}