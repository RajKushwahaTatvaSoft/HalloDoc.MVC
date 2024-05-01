using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Helper.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Data_Layer.DataModels;
using Business_Layer.Utilities;

namespace Business_Layer.Services.Helper
{
    public class SMSService : ISMSService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SMSService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void SendSMS(string phone, string recipientName,int roleId, int adminId, int phyId, string confirmationNumber, int action,int requestId)
        {
            int sentTries = 0;
            bool isSent = false;

            Smslog smsLog = new Smslog()
            {
                Recipientname = recipientName,
                Smstemplate = "1",
                Mobilenumber = phone,
                Roleid = roleId,
                Adminid = adminId,
                Physicianid = phyId,
                Confirmationnumber = confirmationNumber,
                Action = action,
                Requestid = requestId,
                Createdate = DateTime.Now,
                Sentdate = DateTime.Now,
                Issmssent = true,
                Senttries = 1,
            };

            _unitOfWork.SMSLogRepository.Add(smsLog);
            _unitOfWork.Save();

        }
    }
}
