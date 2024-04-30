using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Data_Layer.DataModels;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Repository.IRepository;
using Business_Layer.Utilities;

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

            SendMail(email,body,subject,out int sentTries, out bool isSent);           

        }

        public void SendMailForPatientAgreement(int requestId,string patientEmail)
        {
            string encryptedId = EncryptionService.Encrypt(requestId.ToString());
            string sendAgreementLink = Constants.BASE_URL + "/Guest/ReviewAgreement?requestId=" + encryptedId;

            string subject = "Set up your Account";
            string body = "<h1>Hello , Patient!!</h1><p>You can review your agrrement and accept it to go ahead with the medical process," +
                " which  sent by the physician. </p><a href=\"" + sendAgreementLink + "\" >Click here to accept agreement</a>";

            SendMail(patientEmail,body,subject,out int sentTries,out bool isSent);

        }
        public void SendMailForSubmitRequest()
        {

        }
    }
}
