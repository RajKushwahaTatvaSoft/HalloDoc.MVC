using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public enum AllowMenu
    {
        AdminDashboard = 31,
        ProviderLocation = 32,
        MyProfile = 33,
        ProviderMenu = 34,
        Scheduling = 35,
        Invoicing = 36,
        Partners = 37,
        AccountAccess = 38, 
        UserAccess = 39,
        SearchRecords = 40,
        EmailLogs = 41,
        SMSLogs = 42,
        PatientRecords = 43,
        BlockedHistory = 44,
    }

    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3, // cancelled by admin
        MDEnRoute = 4,
        MDOnSite = 5,
        Conclude = 6,
        CancelledByPatient = 7, // cancelled by patient
        Closed = 8,
        Unpaid = 9,
        Clear = 10,
        Block = 11,
    }

    public enum DashboardStatus
    {
        New = 1,
        Pending = 2,
        Active = 3,
        Conclude = 4,
        ToClose = 5,
        Unpaid = 6,
    }

    public enum RequestType
    {
        Business = 1,
        Patient = 2,
        Family = 3,
        Concierge = 4
    }

    public enum AccountType
    {
        Admin = 1,
        Physician = 2,
        Patient = 3,
    }

}
