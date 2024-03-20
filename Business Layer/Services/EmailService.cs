using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Business_Layer.Interface;

namespace Business_Layer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public void SendMail(string toEmail, string body, string subject)
        {

            string senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
            string senderPassword = _config.GetSection("OutlookSMTP")["Password"];

            SmtpClient client = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "HalloDoc"),
                Subject = subject,
                IsBodyHtml = true,
                Body = body,
            };

            mailMessage.To.Add(toEmail);

            client.Send(mailMessage);


        }
    }
}
