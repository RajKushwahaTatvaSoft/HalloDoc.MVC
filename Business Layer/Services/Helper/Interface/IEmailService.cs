﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.Helper.Interface
{
    public interface IEmailService
    {
        public void SendMail(string toEmail, string body, string subject, out int sentTries, out bool isSent);

    }
}