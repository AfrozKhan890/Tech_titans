using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using MarketLink.Areas.Admin.ViewModels;

namespace MarketLink.Areas.Admin.Services
{
    /// <summary>
    /// Persists a single PlatformSettings row and provides outbound SMTP
    /// for verification emails using the same row's credentials.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly MarketLinkDbContext _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(MarketLinkDbContext context, ILogger<SettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SettingsViewModel> GetSettingsAsync()
        {
            var settings = await GetOrCreateAsync();

            return new SettingsViewModel
            {
                PlatformName = settings.PlatformName,
                ContactEmail = settings.ContactEmail,
                ContactPhone = settings.ContactPhone,
                DefaultMapProvider = settings.DefaultMapProvider,
                EnableOrderNotifications = settings.EnableOrderNotifications,
                EnableReviewNotifications = settings.EnableReviewNotifications,
                EnableFarmerNotifications = settings.EnableFarmerNotifications,
                SmtpHost = settings.SmtpHost,
                SmtpPort = settings.SmtpPort,
                SmtpUsername = settings.SmtpUsername,
                SmtpPassword = settings.SmtpPassword,
                FromEmail = settings.FromEmail,
                FromName = settings.FromName,
                SmtpUseSsl = settings.SmtpUseSsl
            };
        }

        public async Task UpdateSettingsAsync(SettingsViewModel model)
        {
            var settings = await GetOrCreateAsync();

            settings.PlatformName = model.PlatformName;
            settings.ContactEmail = model.ContactEmail;
            settings.ContactPhone = model.ContactPhone;
            settings.DefaultMapProvider = model.DefaultMapProvider;
            settings.EnableOrderNotifications = model.EnableOrderNotifications;
            settings.EnableReviewNotifications = model.EnableReviewNotifications;
            settings.EnableFarmerNotifications = model.EnableFarmerNotifications;

            // Preserve the stored password if the admin left the field blank
            // (typical when the browser autofills or the field is untouched).
            if (!string.IsNullOrWhiteSpace(model.SmtpPassword))
            {
                settings.SmtpPassword = model.SmtpPassword;
            }

            settings.SmtpHost = model.SmtpHost;
            settings.SmtpPort = model.SmtpPort;
            settings.SmtpUsername = model.SmtpUsername;
            settings.FromEmail = model.FromEmail;
            settings.FromName = model.FromName;
            settings.SmtpUseSsl = model.SmtpUseSsl;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return false;
            }

            var settings = await GetOrCreateAsync();

            if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(settings.SmtpUsername) ||
                string.IsNullOrWhiteSpace(settings.SmtpPassword))
            {
                _logger.LogWarning("SMTP is not fully configured; test email aborted.");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = $"{settings.PlatformName} — SMTP verification";
                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <div style=""font-family:Segoe UI,Arial,sans-serif;max-width:520px;padding:24px;
                                    background:#F6F0D7;border-radius:12px;color:#4A4A4A;"">
                            <h2 style=""color:#3A4720;margin:0 0 8px;"">{settings.PlatformName}</h2>
                            <p style=""margin:0 0 12px;"">This is a test email sent from the
                            <strong>{settings.PlatformName} Admin Panel</strong> to verify that
                            SMTP configuration is working correctly.</p>
                            <p style=""margin:0;font-size:12px;color:#5C6F2B;"">
                                Sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                            </p>
                        </div>"
                };

                using var client = new SmtpClient();
                var socketOption = settings.SmtpUseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOption);
                await client.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("SMTP test email sent to {Recipient}.", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMTP test email to {Recipient}.", toEmail);
                return false;
            }
        }

        private async Task<PlatformSettings> GetOrCreateAsync()
        {
            var settings = await _context.PlatformSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                return settings;
            }

            settings = new PlatformSettings
            {
                PlatformName = "MarketLink",
                ContactEmail = "admin@marketlink.com",
                DefaultMapProvider = "OpenStreetMap",
                SmtpHost = "smtp.gmail.com",
                SmtpPort = 587,
                SmtpUsername = "afrozkhan43186@gmail.com",
                SmtpPassword = string.Empty,
                FromEmail = "afrozkhan43186@gmail.com",
                FromName = "MarketLink",
                SmtpUseSsl = true
            };

            _context.PlatformSettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }
    }
}