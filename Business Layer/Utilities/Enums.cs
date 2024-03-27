using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{

    public enum AllowMenu
    {
        Regions = 1,
        Schedulign = 2,
        History = 3,
        Accounts = 4,
        MyProfile = 5,
        AdminDashboard = 6,
        Role = 7,
        Provider = 8,
    }

    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3,
        MDEnRoute = 4,
        MDOnSite = 5,
        Conclude = 6,
        CancelledByPatient = 7,
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

    public enum AllowRole
    {
        Admin = 1,
        Patient = 2,
        Physician = 3
    }

}
