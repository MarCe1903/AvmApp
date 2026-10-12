namespace Project4.WebAPI.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail,string subject,string htmlBody);
}