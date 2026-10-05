using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Infrastructure;

public sealed class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly SmtpSettings? settings;
    private readonly ILogger<PasswordResetEmailSender> logger;

    public PasswordResetEmailSender(
        IConfiguration configuration,
        ILogger<PasswordResetEmailSender> logger)
    {
        settings = SmtpSettings.FromConfiguration(configuration);
        this.logger = logger;
    }

    public bool IsConfigured => settings is not null;

    public async Task<bool> SendAsync(
        string email,
        string fullName,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (settings is null)
            return false;

        try
        {
            var resetLink = $"{settings.SiteOrigin}/login#resetToken={Uri.EscapeDataString(token)}";
            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromAddress, "StockFlow", Encoding.UTF8),
                Subject = "Reset password StockFlow",
                Body = $"Halo {fullName},\n\n" +
                    $"Gunakan tautan berikut dalam 30 menit untuk membuat password baru:\n{resetLink}\n\n" +
                    "Jika Anda tidak meminta reset password, abaikan email ini."
            };
            message.To.Add(new MailAddress(email));

#pragma warning disable SYSLIB0014 // Use the built-in authenticated STARTTLS client to avoid adding a mail package.
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                Timeout = 30_000
            };

            await client.SendMailAsync(message, cancellationToken);
#pragma warning restore SYSLIB0014
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                "Password reset email delivery failed ({ErrorType}).",
                exception.GetType().Name);
            return false;
        }
    }

    private sealed record SmtpSettings(
        string Host,
        int Port,
        string Username,
        string Password,
        string FromAddress,
        string SiteOrigin)
    {
        public static SmtpSettings? FromConfiguration(IConfiguration configuration)
        {
            var host = configuration["PasswordReset:Smtp:Host"]?.Trim();
            var username = configuration["PasswordReset:Smtp:Username"];
            var password = configuration["PasswordReset:Smtp:Password"];
            var fromAddress = configuration["PasswordReset:Smtp:FromAddress"]?.Trim();
            var siteOrigin = configuration["WebOrigin"]?.TrimEnd('/');
            var sslSetting = configuration["PasswordReset:Smtp:EnableSsl"] ?? "true";
            var enableSsl = bool.TryParse(sslSetting, out var parsedSsl) && parsedSsl;
            var port = int.TryParse(configuration["PasswordReset:Smtp:Port"], out var configuredPort)
                ? configuredPort
                : 587;

            if (string.IsNullOrWhiteSpace(host) ||
                port is < 1 or > 65535 ||
                Uri.CheckHostName(host) == UriHostNameType.Unknown ||
                !enableSsl ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            if (!MailAddress.TryCreate(fromAddress, out var parsedFromAddress) || parsedFromAddress is null)
                return null;

            if (!Uri.TryCreate(siteOrigin, UriKind.Absolute, out var origin) || origin is null)
                return null;

            if (origin.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(origin.UserInfo) ||
                origin.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(origin.Query) ||
                !string.IsNullOrEmpty(origin.Fragment))
            {
                return null;
            }

            return new SmtpSettings(
                host!,
                port,
                username!,
                password!,
                parsedFromAddress.Address,
                $"{origin.Scheme}://{origin.Authority}");
        }
    }
}
