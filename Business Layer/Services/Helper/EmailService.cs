using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Server.IIS.Core;
using Org.BouncyCastle.Asn1.X509;
using Business_Layer.Services.Helper.Interface;

namespace Business_Layer.Services.Helper
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public EmailService(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        public void SendMail(string toEmail, string body, string subject, out int sentTries, out bool isSent)
        {
            sentTries = 0;
            isSent = false;

            string? senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
            string? senderPassword = _config.GetSection("OutlookSMTP")["Password"];

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

            while (sentTries < 3)
            {
                sentTries++;
                try
                {
                    client.Send(mailMessage);
                    isSent = true;
                    break;
                }
                catch (Exception e)
                {
                    isSent = false;
                    continue;
                }

            }

        }

    }
}
