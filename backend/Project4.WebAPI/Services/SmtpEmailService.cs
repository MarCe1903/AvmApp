using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
namespace Project4.WebAPI.Services;
public class SmtpEmailService : IEmailService
{
    private readonly MailSettings _mail;
    public SmtpEmailService(IOptions<MailSettings> options)
    {
        _mail=options.Value;
    }

    public async Task SendAsync(string toEmail,string subject, string htmlBody)
    {
        using var client= new SmtpClient(_mail.Host, _mail.Port)
        {
            Credentials= new NetworkCredential(_mail.UserName, _mail.Password),
            EnableSsl=true

        };
        using var message =new MailMessage
        {
            From=new MailAddress(_mail.FromEmail,_mail.FromName),
            Subject=subject,
            Body=htmlBody,
            IsBodyHtml=true
        };
        message.To.Add(toEmail);
        await client.SendMailAsync(message);
    }


}