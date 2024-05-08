using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Data_Layer.DataModels;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Repository.IRepository;
using Business_Layer.Utilities;
using Org.BouncyCastle.Asn1.Ocsp;
using iTextSharp.text.pdf;
using System;
using Microsoft.AspNetCore.Http;

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
            catch (Exception ex)
            {
                isSent = false;
            }

        }

        public void SendMailForCreateAccount(string email, string aspNetUserId, string link)
        {

            string createAccToken = Guid.NewGuid().ToString();

            string createAccLink = Constants.BASE_URL + "/Guest/CreateAccount?token=" + createAccToken;

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

            string subject = "Set up your Account";
            string body = "<h1>Create Account By clicking below</h1><a href=\"" + createAccLink + "\" >Create Account link</a>";

            SendMail(email, body, subject, out int sentTries, out bool isSent);

        }

        public void SendMailForPatientAgreement(int requestId, string patientEmail)
        {
            string encryptedId = EncryptionService.Encrypt(requestId.ToString());
            string sendAgreementLink = Constants.BASE_URL + "/Guest/ReviewAgreement?requestId=" + encryptedId;

            string subject = "Set up your Account";
            string body = "<h1>Hello , Patient!!</h1><p>You can review your agrrement and accept it to go ahead with the medical process," +
                " which  sent by the physician. </p><a href=\"" + sendAgreementLink + "\" >Click here to accept agreement</a>";

            SendMail(patientEmail, body, subject, out int sentTries, out bool isSent);

        }
        public void SendMailForSubmitRequest(string patientName, string toEmail, bool isAdmin, int senderUserId)
        {
            string submitRequestLink = Constants.BASE_URL + "/Guest/SubmitRequest";

            string subject = "Create Request Link";
            string body = $"<h1>Hola , {patientName} !!</h1><p>Clink the link below to create request.</p><a href=\" {submitRequestLink} \" >Submit Request Link</a>";

            SendMail(toEmail, body, subject, out int sentTries, out bool isSent);

            Emaillog emailLog = new Emaillog()
            {
                Recipientname = patientName,
                Emailtemplate = "1",
                Subjectname = subject,
                Emailid = toEmail,
                Roleid = (int)AccountType.Patient,
                Adminid = isAdmin ? senderUserId : null,
                Physicianid = isAdmin ? null : senderUserId,
                Createdate = DateTime.Now,
                Sentdate = DateTime.Now,
                Isemailsent = isSent,
                Senttries = sentTries,
            };

            _unitOfWork.EmailLogRepository.Add(emailLog);
            _unitOfWork.Save();
        }
        public void SendMailForRequestDTYSupport(string message, IEnumerable<string> offDutyPhyEmails, int adminId, int roleId)
        {

            string subject = "Request Support for HalloDoc";
            string body = message;

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

            //List<int> emailLogsId = new List<int>();
            foreach (string email in offDutyPhyEmails)
            {
                mailMessage.To.Add(email);
                Emaillog emaillog = new()
                {
                    Emailtemplate = "1",
                    Subjectname = subject,
                    Emailid = email,
                    Roleid = roleId,
                    Adminid = adminId,
                    Createdate = DateTime.Now,
                    Sentdate = DateTime.Now,
                    Isemailsent = true,
                    Senttries = 1,
                };

                _unitOfWork.EmailLogRepository.Add(emaillog);
                _unitOfWork.Save();

                //emailLogsId.Add(emaillog.Emaillogid);
            }

            client.Send(mailMessage);

        }

        public void SendMailToAdminForEditProfile(string message, int phyId, string phyName)
        {

            IEnumerable<Admin> admins = _unitOfWork.AdminRepository.GetAll();

            string subject = "Need To Edit My Profile";
            string? senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
            string? senderPassword = _config.GetSection("OutlookSMTP")["Password"];

            string editPhyProfileLink = Constants.BASE_URL + "/Admin/EditPhysicianAccount?physicianId="+phyId;

            string body = "<p>" +
                "Provider " + phyName + " sent message regarding changing his profile: " + message +
                "</p>" +
                "<a href=\"" + editPhyProfileLink + "\" >Click here to edit physician profile</a>";

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

            foreach (Admin admin in admins)
            {
                string email = admin.Email;

                if (!string.IsNullOrEmpty(email))
                {
                    mailMessage.To.Add(email);

                    Emaillog emaillog = new()
                    {
                        Emailtemplate = "r",
                        Subjectname = subject,
                        Emailid = email,
                        Roleid = admin.Roleid,
                        Adminid = admin.Adminid,
                        Createdate = DateTime.Now,
                        Sentdate = DateTime.Now,
                        Isemailsent = true,
                    };
                    _unitOfWork.EmailLogRepository.Add(emaillog);
                }

            }

            client.Send(mailMessage);
            _unitOfWork.Save();

        }

        public void SendMailWithAttachments(string toEmail, string body, string subject, List<Attachment> attachments)
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

            foreach (Attachment attachment in attachments)
            {
                mailMessage.Attachments.Add(attachment);
            }

            mailMessage.To.Add(toEmail);

            client.Send(mailMessage);
        }

        public void SendMailWithFilesAttached(string email, List<Attachment> attachments)
        {

            string subject = "Hallodoc documents attachments";
            string body = "<h3>Admin has sent you documents regarding your request.</h3>";

            SendMailWithAttachments(email, body, subject, attachments);
        }
    }
}
