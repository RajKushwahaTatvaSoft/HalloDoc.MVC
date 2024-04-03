using Business_Layer.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Helpers
{
    public static class RequestHelper
    {
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
