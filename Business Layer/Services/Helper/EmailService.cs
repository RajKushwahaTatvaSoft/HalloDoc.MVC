using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Server.IIS.Core;
using Org.BouncyCastle.Asn1.X509;
using Business_Layer.Services.Helper.Interface;
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Business_Layer.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Business_Layer.Repository;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Business_Layer.Services.Helper
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;


        public EmailService(IConfiguration config, IUnitOfWork unitOfWork)
        {
            _config = config;
            _unitOfWork = unitOfWork;
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

            try
            {
                client.Send(mailMessage);
                isSent = true;
            }
            catch (Exception e)
            {
                isSent = false;
            }

        }


        public void SendMailForCreateAccount(string email, string aspNetUserId, string link)
        {

            string createAccToken = Guid.NewGuid().ToString();

            Passtoken passtoken = new Passtoken()
            {
                Aspnetuserid = aspNetUserId,
                Createddate = DateTime.Now,
                Email = email,
                Isdeleted = false,
                Isresettoken = false,
                Uniquetoken = createAccToken,
            };

            _unitOfWork.PassTokenRepository.Add(passtoken);
            _unitOfWork.Save();

            string createLink = link + "?token=" + createAccToken;

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
                Subject = "Set up your Account",
                IsBodyHtml = true,
                Body = "<h1>Create Account By clicking below</h1><a href=\"" + createLink + "\" >Create Account link</a>",
            };

            mailMessage.To.Add(email);

            client.Send(mailMessage);


        }

    }
}
