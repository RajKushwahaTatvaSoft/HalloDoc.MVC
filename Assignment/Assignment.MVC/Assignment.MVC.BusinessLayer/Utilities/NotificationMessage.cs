using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assignment.MVC.BusinessLayer.Utilities
{
    public static class NotificationMessage
    {
        public const string PATIENT_NOT_FOUND = "Patient Not Found";
        public const string PATIENT_ADDED_SUCCESSFULLY = "Patient Added Successfully";
        public const string PATIENT_UPDATED_SUCCESSFULLY = "Patient Updated Successfully";
        public const string PATIENT_REMOVED_SUCCESSFULLY = "Patient Deleted Successfully";
        public const string INVALID_FIELDS_ERROR = "Please ensure all fields are valid.";
    }
}
