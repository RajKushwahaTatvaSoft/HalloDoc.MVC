using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.CustomModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data_Layer.CustomModels.Filter;

namespace Business_Layer.Services.AdminServices
{
    public class AdminRecordService : IAdminRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AdminRecordService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<SearchRecordTRow> GetSearchRecordsDataUnPaginated(SearchRecordFilter searchRecordFilter)
        {
            var query = (from r in _unitOfWork.RequestRepository.GetAll()
                         join rc in _unitOfWork.RequestClientRepository.GetAll() on r.Requestid equals rc.Requestid
                         join rnote in _unitOfWork.RequestNoteRepository.GetAll() on r.Requestid equals rnote.Requestid into noteGroup
                         from noteItem in noteGroup.DefaultIfEmpty()
                         join rs in _unitOfWork.RequestStatusRepository.GetAll() on r.Status equals rs.Statusid
                         join phy in _unitOfWork.PhysicianRepository.GetAll() on r.Physicianid equals phy.Physicianid into phyGroup
                         from phyItem in phyGroup.DefaultIfEmpty()
                         where (string.IsNullOrEmpty(searchRecordFilter.PatientName) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(searchRecordFilter.PatientName.ToLower()))
                         && (searchRecordFilter.RequestStatus == 0 || r.Status == searchRecordFilter.RequestStatus)
                         && (searchRecordFilter.RequestType == 0 || r.Requesttypeid == searchRecordFilter.RequestType)
                         && (string.IsNullOrEmpty(searchRecordFilter.PhoneNumber) || rc.Phonenumber.ToLower().Contains(searchRecordFilter.PhoneNumber.ToLower()))
                         && (string.IsNullOrEmpty(searchRecordFilter.ProviderName) || (phyItem.Firstname + " " + phyItem.Lastname).ToLower().Contains(searchRecordFilter.ProviderName.ToLower()))
                         && (string.IsNullOrEmpty(searchRecordFilter.PatientEmail) || rc.Email.ToLower().Contains(searchRecordFilter.PatientEmail.ToLower()))
                         && ((searchRecordFilter.FromDateOfService == null) || r.Accepteddate >= searchRecordFilter.FromDateOfService.Value.Date)
                         && ((searchRecordFilter.ToDateOfService == null) || r.Accepteddate <= searchRecordFilter.ToDateOfService.Value.Date)
                         select new SearchRecordTRow
                         {
                             RequestId = r.Requestid,
                             PatientName = rc.Firstname + " " + rc.Lastname,
                             Requestor = RequestHelper.GetRequestType(r.Requesttypeid),
                             DateOfService = r.Accepteddate,
                             CloseCaseDate = _unitOfWork.RequestStatusLogRepository.GetAll().AsEnumerable().FirstOrDefault(rstaus => rstaus.Requestid == r.Requestid && rstaus.Status == (int)RequestStatus.Closed).Createddate,
                             Email = rc.Email ?? "",
                             PhoneNumber = rc.Phonenumber ?? "",
                             Address = rc.Address ?? "",
                             Zip = rc.Zipcode ?? "",
                             RequestStatus = rs.Statusname,
                             Physician = phyItem.Firstname + " " + phyItem.Lastname,
                             PhysicianNote = noteItem.Physiciannotes,
                             AdminNote = noteItem.Adminnotes,
                             CancelledByPhysicianNote = "",
                             PatientNote = "",
                         });

            return query;
        }


        public async Task<PagedList<SearchRecordTRow>> GetSearchRecordsDataAsync(SearchRecordFilter searchRecordFilter)
        {
            int pageSize = searchRecordFilter.PageSize < 1 ? 1 : searchRecordFilter.PageSize;
            int pageNumber = searchRecordFilter.PageNumber < 1 ? 1 : searchRecordFilter.PageNumber;

            var query = (from r in _unitOfWork.RequestRepository.GetAll()
                         join rc in _unitOfWork.RequestClientRepository.GetAll() on r.Requestid equals rc.Requestid
                         join rnote in _unitOfWork.RequestNoteRepository.GetAll() on r.Requestid equals rnote.Requestid into noteGroup
                         from noteItem in noteGroup.DefaultIfEmpty()
                         join rs in _unitOfWork.RequestStatusRepository.GetAll() on r.Status equals rs.Statusid
                         join phy in _unitOfWork.PhysicianRepository.GetAll() on r.Physicianid equals phy.Physicianid into phyGroup
                         from phyItem in phyGroup.DefaultIfEmpty()
                         where (string.IsNullOrEmpty(searchRecordFilter.PatientName) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(searchRecordFilter.PatientName.ToLower()))
                         && (searchRecordFilter.RequestStatus == 0 || r.Status == searchRecordFilter.RequestStatus)
                         && (searchRecordFilter.RequestType == 0 || r.Requesttypeid == searchRecordFilter.RequestType)
                         && (string.IsNullOrEmpty(searchRecordFilter.PhoneNumber) || rc.Phonenumber.ToLower().Contains(searchRecordFilter.PhoneNumber.ToLower()))
                         && (string.IsNullOrEmpty(searchRecordFilter.ProviderName) || (phyItem.Firstname + " " + phyItem.Lastname).ToLower().Contains(searchRecordFilter.ProviderName.ToLower()))
                         && (string.IsNullOrEmpty(searchRecordFilter.PatientEmail) || rc.Email.ToLower().Contains(searchRecordFilter.PatientEmail.ToLower()))
                         && ((searchRecordFilter.FromDateOfService == null) || r.Accepteddate >= searchRecordFilter.FromDateOfService.Value.Date)
                         && ((searchRecordFilter.ToDateOfService == null) || r.Accepteddate <= searchRecordFilter.ToDateOfService.Value.Date)
                         select new SearchRecordTRow
                         {
                             RequestId = r.Requestid,
                             PatientName = rc.Firstname + " " + rc.Lastname,
                             Requestor = RequestHelper.GetRequestType(r.Requesttypeid),
                             DateOfService = r.Accepteddate,
                             CloseCaseDate = _unitOfWork.RequestStatusLogRepository.GetAll().AsEnumerable().FirstOrDefault(rstaus => rstaus.Requestid == r.Requestid && rstaus.Status == (int)RequestStatus.Closed).Createddate,
                             Email = rc.Email ?? "",
                             PhoneNumber = rc.Phonenumber ?? "",
                             Address = rc.Address ?? "",
                             Zip = rc.Zipcode ?? "",
                             RequestStatus = rs.Statusname,
                             Physician = phyItem.Firstname + " " + phyItem.Lastname,
                             PhysicianNote = noteItem.Physiciannotes,
                             AdminNote = noteItem.Adminnotes,
                             CancelledByPhysicianNote = "",
                             PatientNote = "",
                         }).AsQueryable();

            PagedList<SearchRecordTRow> pagedList = await PagedList<SearchRecordTRow>.CreateAsync(
            query, pageNumber, pageSize);

            return pagedList;
        }
    }
}
