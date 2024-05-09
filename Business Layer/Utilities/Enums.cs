﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public enum AccountStatus
    {
        Active = 1,
        Disabled = 2,
    }

    public enum PayrateCategoryEnum
    {
        NightShiftWeekend = 1,
        Shift = 2,
        HouseCallNightWeekend =3,
        PhoneConsult = 4,
        PhoneConsultNightWeekend =5,
        BatchTesting = 6,
        HouseCall = 7,
    }

    public enum RequestCallType
    {
        HouseCall = 1,
        Consult = 2,
    }

    public enum AllowMenu
    {
        AdminDashboard = 1,
        ProviderLocation = 2,
        AdminProfile = 3,
        ProviderMenu = 4,
        Scheduling = 5,
        Invoicing = 6,
        Partners = 7,
        AccountAccess = 8,
        UserAccess = 9,
        SearchRecords = 10,
        EmailLogs = 11,
        SMSLogs = 12,
        PatientRecords = 13,
        BlockedHistory = 14,
        ProviderDashboard = 15,
        ProviderInvoicing = 16,
        ProviderSchedule = 17,
        ProviderProfile = 18,
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

    public enum ShiftStatus
    {
        Approved = 1,
        Pending = 2,
    }

    public enum ShiftWeekDays
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7
    }

}
