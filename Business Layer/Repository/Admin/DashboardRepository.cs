using Business_Layer.Interface.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.Admin
{
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

    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AdminRequest> GetAdminRequest(int status)
        {
            List<AdminRequest> adminRequests = new List<AdminRequest>();

            if (status == (int)DashboardStatus.New)
            {

                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Unassigned)
                                 select new AdminRequest
                                 {
                                     RequestId = r.Requestid,
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Pending)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Accepted)
                                 select new AdminRequest
                                 {
                                     RequestId = r.Requestid,
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Active)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Conclude)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Conclude)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.ToClose)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Unpaid)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Unpaid)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }

            return adminRequests;
        }


        public static string GetPatientDOB(Requestclient u)
        {
            string udb = u.Intyear + "-" + u.Strmonth + "-" + u.Intdate;
            if (u.Intyear == null || u.Strmonth == null || u.Intdate == null)
            {
                return "";
            }

            DateTime dobDate = DateTime.Parse(udb);
            string dob = dobDate.ToString("MMM dd, yyyy");
            var today = DateTime.Today;
            var age = today.Year - dobDate.Year;
            if (dobDate.Date > today.AddYears(-age)) age--;

            string dobString = dob + " (" + age + ")";

            return dobString;
        }

        public static string GetRequestType(Request request)
        {
            switch (request.Requesttypeid)
            {
                case (int)RequestType.Business: return "Business";
                case (int)RequestType.Patient: return "Patient";
                case (int)RequestType.Concierge: return "Concierge";
                case (int)RequestType.Family: return "Relative/Family";
            }

            return null;
        }

    }
}
