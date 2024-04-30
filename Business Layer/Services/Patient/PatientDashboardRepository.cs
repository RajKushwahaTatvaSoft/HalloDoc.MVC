using Business_Layer.Services.Patient.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.TableRow.Patient;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;

namespace Business_Layer.Services.Patient
{
    public class PatientDashboardRepository : IPatientDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        public PatientDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedList<PatientDashboardTRow>> GetPatientRequestsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            var query = (from r in _context.Requests
                         where r.Createduserid == userId
                         select new PatientDashboardTRow
                         {
                             RequestId = r.Requestid,
                             RequestStatus = RequestHelper.GetRequestStatusString(r.Status),
                             CreatedDate = r.Createddate,
                             FileCount = _context.Requestwisefiles.Count(file => file.Requestid == r.Requestid),
                         }).AsQueryable();

            return await PagedList<PatientDashboardTRow>.CreateAsync(
            query, pageNumber, pageSize);

        }
    }
}
