using Data_Layer.CustomModels.Filter;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Data_Layer.DataModels;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminRecordService
    {
        public Task<PagedList<SearchRecordTRow>> GetSearchRecordsDataAsync(SearchRecordFilter searchRecordFilter);
        public IEnumerable<SearchRecordTRow> GetSearchRecordsDataUnPaginated(SearchRecordFilter searchRecordFilter);
        public DataTable GetDataTableForSearchRecord(IEnumerable<SearchRecordTRow> requestList);
        public Task<PagedList<LogTableRow>> GetEmailLogsPaginatedAsync(LogFilter filter);
        public Task<PagedList<LogTableRow>> GetSMSLogsPaginatedAsync(LogFilter filter);
        public Task<PagedList<User>> GetPatientRecordsPaginatedAsync(PatientRecordFilter filter);
        public ServiceResponse UnBlockRequest(int requestId, string adminName, int adminId);
    }
}
