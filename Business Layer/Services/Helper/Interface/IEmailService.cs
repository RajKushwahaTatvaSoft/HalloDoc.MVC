using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.Helper.Interface
{
    public interface IEmailService
    {
        public void SendMail(string toEmail, string body, string subject, out int sentTries, out bool isSent);
        public void SendMailForCreateAccount(string email, string aspNetUserId,string link);
        public void SendMailForPatientAgreement(int requestId, string patientEmail);
        public void SendMailForSubmitRequest(string patientName, string toEmail, bool isAdmin, int senderUserId);
        public void SendMailWithAttachments(string toEmail, string body, string subject, List<Attachment> attachments);
        public void SendMailToAdminForEditProfile(string message, int phyId, string phyName);
        public void SendMailWithFilesAttached(string email, List<Attachment> attachments);
        public void SendMailForRequestDTYSupport(string message, IEnumerable<string> offDutyPhyEmails, int adminId, int roleId);
    }
}
