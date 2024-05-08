using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public class AuthHelper
    {

        public static string GenerateSHA256(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            using (SHA256 hashEngine = SHA256.Create())
            {
                byte[] hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte stringByte in hashedBytes)
                {
                    string hex = stringByte.ToString("x2");
                    stringBuilder.Append(hex);
                }
                return stringBuilder.ToString();
            }
        }

        public static string GetControllerFromMenuId(int menuId)
        {
            if(menuId <= 14)
            {
                return "Admin";
            }
            else if(menuId <= 18)
            {
                return "Physician";
            }

            return string.Empty;
        }

        public static string GetActionFromMenuId(int menuId)
        {
            AllowMenu menu = (AllowMenu)menuId;
            switch (menu)
            {
                case AllowMenu.AdminDashboard:
                    return "Dashboard";
                case AllowMenu.ProviderLocation:
                    return "ProviderLocation";
                case AllowMenu.AdminProfile:
                    return "Profile";
                case AllowMenu.ProviderMenu:
                    return "ProviderMenu";
                case AllowMenu.Scheduling:
                    return "Scheduling";
                case AllowMenu.Invoicing:
                    return "Invoicing";
                case AllowMenu.Partners:
                    return "Vendors";
                case AllowMenu.AccountAccess:
                    return "AccountAccess";
                case AllowMenu.UserAccess:
                    return "UserAccess";
                case AllowMenu.SearchRecords:
                    return "SearchRecords";
                case AllowMenu.EmailLogs:
                    return "EmailLogs";
                case AllowMenu.SMSLogs:
                    return "SMSLogs";
                case AllowMenu.PatientRecords:
                    return "PatientRecords";
                case AllowMenu.BlockedHistory:
                    return "BlockedHistory";
                case AllowMenu.ProviderDashboard:
                    return "Dashboard";
                case AllowMenu.ProviderProfile:
                    return "Profile";
                case AllowMenu.ProviderSchedule:
                    return "Schedule";
                case AllowMenu.ProviderInvoicing:
                    return "Invoicing";
            }
            return "";
        }

    }
}
