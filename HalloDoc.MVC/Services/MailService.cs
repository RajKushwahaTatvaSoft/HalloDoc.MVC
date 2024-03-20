using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace HalloDoc.MVC.Services
{
    public class MailService
    {
        private readonly IConfiguration _config;
        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public static bool SendMail(string subject,bool isHtml, string body, string fromEmail, string toEmail, string senderEmail, string senderPassword)
        {
            try
            {

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
                    From = new MailAddress(fromEmail, "HalloDoc"),
                    Subject = subject,
                    IsBodyHtml = isHtml,
                    Body = body,
                };

                mailMessage.To.Add(toEmail);
                client.Send(mailMessage);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
