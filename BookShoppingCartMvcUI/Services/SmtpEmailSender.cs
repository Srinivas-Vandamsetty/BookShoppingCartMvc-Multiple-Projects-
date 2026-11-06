using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
namespace BookShoppingCartMvcUI.Services;
// Development pickup files stay outside wwwroot; production always requires authenticated TLS SMTP.
public sealed class SmtpEmailSender(IConfiguration config, IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var client = new SmtpClient();
        var from = config["Email:From"] ?? "noreply@localhost";
        if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(config["Email:SmtpHost"]))
        {
            var directory = Path.Combine(environment.ContentRootPath, ".email-outbox");
            Directory.CreateDirectory(directory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = directory;
        }
        else
        {
            client.Host = config["Email:SmtpHost"] ?? throw new InvalidOperationException("Email delivery is not configured.");
            client.Port = config.GetValue("Email:SmtpPort", 587);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(config["Email:Username"], config["Email:Password"]);
        }
        using var message = new MailMessage(from, email, subject, htmlMessage) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
}
