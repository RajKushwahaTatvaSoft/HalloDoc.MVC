using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface
{
    public interface IEmailService
    {
        public void SendMail(string toEmail, string body, string subject);

    }
}
