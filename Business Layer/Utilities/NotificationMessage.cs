using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public static class NotificationMessage
    {
        public const string REQUEST_NOT_FOUND = "Request not found";
        public const string REQUEST_ALREADY_BLOCKED = "Request is already blocked";
        public const string REQUEST_CREATED_SUCCESSFULLY = "Request Created Successfully";
        public const string REQUEST_DELETED_SUCCESSFULLY = "Request Deleted Successfully";
        public const string REQUEST_UNBLOCKED_SUCCESSFULLY = "Request Unblocked Successfully";
        
        public const string PHYSICIAN_NOT_FOUND = "Physician not found";
        public const string FILE_NOT_FOUND = "File not found";
        public const string VENDOR_NOT_FOUND = "Vendor not found";
        public const string TIMESHEET_NOT_FOUND = "Time sheet not found";

        public const string PATIENT_CANNOT_CREATED_WITH_GIVEN_EMAIL = "Patient cannot be created with this email. Please try again with different email";
    }
}
