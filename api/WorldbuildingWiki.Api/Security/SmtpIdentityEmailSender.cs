using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace WorldbuildingWiki.Api.Security;

public sealed class SmtpIdentityEmailSender(IConfiguration configuration) : IEmailSender<WikiUser>
{
    public Task SendConfirmationLinkAsync(WikiUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Worldbuilding Wiki email", $"Use this link to confirm your email: {confirmationLink}");

    public Task SendPasswordResetLinkAsync(WikiUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Worldbuilding Wiki password", $"Use this link to reset your password: {resetLink}");

    public Task SendPasswordResetCodeAsync(WikiUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your Worldbuilding Wiki password", $"Use this code to reset your password: {resetCode}");

    private async Task SendAsync(string recipient, string subject, string body)
    {
        var host = configuration["Email:Smtp:Host"];
        var sender = configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("Password reset email delivery is not configured.");

        using var message = new MailMessage(sender, recipient, subject, body);
        using var client = new SmtpClient(host, configuration.GetValue<int?>("Email:Smtp:Port") ?? 587)
        {
            EnableSsl = true
        };
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
        await client.SendMailAsync(message);
    }
}
