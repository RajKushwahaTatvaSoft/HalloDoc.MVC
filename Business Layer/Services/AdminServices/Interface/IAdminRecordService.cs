using Data_Layer.CustomModels.Filter;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminRecordService
    {
        public Task<PagedList<SearchRecordTRow>> GetSearchRecordsDataAsync(SearchRecordFilter searchRecordFilter);
        public IEnumerable<SearchRecordTRow> GetSearchRecordsDataUnPaginated(SearchRecordFilter searchRecordFilter);
    }
}
