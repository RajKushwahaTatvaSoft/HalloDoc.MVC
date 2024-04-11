using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public static class RequestHelper
    {
        public static string GetRequestIP()
        {
            string ip = "127.0.0.1";
            return ip;
        }

        public static int GetDashboardStatus(int requestStatus)
        {
            switch (requestStatus)
            {
                case (int)RequestStatus.Unassigned:
                    return (int)DashboardStatus.New;
                case (int)RequestStatus.Accepted:
                    return (int)DashboardStatus.Pending;
                case (int)RequestStatus.MDOnSite:
                case (int)RequestStatus.MDEnRoute:
                    return (int)DashboardStatus.Active;
                case (int)RequestStatus.Conclude:
                    return (int)DashboardStatus.Conclude;
                case (int)RequestStatus.Cancelled:
                case (int)RequestStatus.Closed:
                case (int)RequestStatus.CancelledByPatient:
                    return (int)DashboardStatus.ToClose;
                case (int)RequestStatus.Unpaid:
                    return (int)DashboardStatus.Unpaid;
                default: return -1;
            }

        }

        public static string GetRequestStatusString(int status)
        {
            switch (status)
            {
                case (int)RequestStatus.Unassigned:
                    return "Unassigned";
                case (int)RequestStatus.Accepted:
                    return "Accepted";
                case (int)RequestStatus.Cancelled:
                    return "Cancelled";
                case (int)RequestStatus.MDEnRoute:
                    return "MDEnRoute";
                case (int)RequestStatus.MDOnSite:
                    return "MDOnSite";
                case (int)RequestStatus.Conclude:
                    return "Conclude";
                case (int)RequestStatus.CancelledByPatient:
                    return "CancelledByPatient";
                case (int)RequestStatus.Closed:
                    return "Closed";
                case (int)RequestStatus.Unpaid:
                    return "Unpaid";
                case (int)RequestStatus.Clear:
                    return "Clear";
                case (int)RequestStatus.Block:
                    return "Block";
                default:
                    return "";
            }

        }

    }
}
