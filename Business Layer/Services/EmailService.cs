using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Business_Layer.Interface.Services;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Services
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
        public void SendMail(string toEmail, string body, string subject)
        {

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

            int count = 0;
            bool isSent = false;

            while (count < 5)
            {
                count++;
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

            Emaillog log = new Emaillog()
            {
                Emailtemplate = "1",
                Subjectname = subject,
                Emailid = toEmail,
                Confirmationnumber = "-",
                Roleid = 1,
                Senttries = count,
                Isemailsent = isSent,
                Sentdate = DateTime.Now,
            };

            _context.Add(log);
            _context.SaveChanges();


        }
    }
}
