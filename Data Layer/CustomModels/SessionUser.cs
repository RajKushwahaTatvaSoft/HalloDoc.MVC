using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels
{
    public class SessionUser
    {
        public int UserId { get; set; }
        public string UserAspId {  get; set; }
        public int AccountTypeId { get; set; }
        public int RoleId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}