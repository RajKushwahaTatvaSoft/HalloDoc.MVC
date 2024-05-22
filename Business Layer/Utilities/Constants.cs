using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public class Constants
    {
        public const int SUPER_ADMIN_ID = 1;
        public const int DEFAULT_ADMIN_ROLE_ID = 3;
        public const int DEFAULT_PHYSICIAN_ROLE_ID = 5;
        public const string BASE_URL = "https://localhost:7161";
        public const string MASTER_ADMIN_ASP_USER_ID = "master12-fc91-4435-8ee5-9324978admin";

        public const string DEFAULT_ADMIN_IMAGE_PATH = "/images/default/admin_default_svg.svg";
        public const string DEFAULT_PROVIDER_IMAGE_PATH = "/images/default/physician_default_svg.svg";
        public const string DEFAULT_PATIENT_IMAGE_PATH = "/images/default/patient_default_svg.svg";

    }
}
