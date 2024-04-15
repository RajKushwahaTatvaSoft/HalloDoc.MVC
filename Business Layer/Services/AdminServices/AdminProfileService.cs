using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Business_Layer.Services.AdminServices
{
    public class AdminProfileService : IAdminProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AdminProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public AdminProfileViewModel? GetAdminProfileModel(int adminId)
        {

            Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (admin == null)
            {
                //_notyf.Error("Admin Not Found");
                //return RedirectToAction("Index");
                return null;
            }

            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == admin.Aspnetuserid);
            if (aspUser == null)
            {
                //_notyf.Error("AspUser Not Found");
                //return RedirectToAction("Index");
                return null;
            }

            string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(r => r.Regionid == admin.Regionid)?.Name;

            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            IEnumerable<int> adminRegions = _unitOfWork.AdminRegionRepo.Where(region => region.Adminid == adminId).ToList().Select(x => (int)x.Regionid);
            IEnumerable<City> adminMailCityList = _unitOfWork.CityRepository.Where(city => city.Regionid == admin.Regionid);
            int cityId = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name == admin.City)?.Id ?? 0;

            AdminProfileViewModel model = new()
            {
                AspUserName = aspUser.Username,
                StatusId = admin.Status,
                RoleId = admin.Roleid,
                FirstName = admin.Firstname,
                Email = admin.Email,
                ConfirmEmail = admin.Email,
                PhoneNumber = admin.Mobile,
                AltPhoneNumber = admin.Altphone,
                LastName = admin.Lastname,
                regions = regions,
                adminMailCities = adminMailCityList,
                Address1 = admin.Address1,
                Address2 = admin.Address2,
                City = admin.City,
                State = state,
                Zip = admin.Zip,
                RegionId = admin.Regionid ?? 0,
                selectedRegions = adminRegions,
                CityId = cityId,
                roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Admin),
            };

            return model;
        }

        public ServiceResponse UpdateAdminPassword(int adminId, string password)
        {
            Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);
            ServiceResponse response = new ServiceResponse();

            if (admin == null)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = "Admin not found";
                return response;
            }

            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(asp => asp.Id == admin.Aspnetuserid);

            if (aspUser == null)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = "Aspnetuser not found";
                return response;
            }

            string passHash = AuthHelper.GenerateSHA256(password);

            aspUser.Passwordhash = passHash;
            _unitOfWork.AspNetUserRepository.Update(aspUser);
            _unitOfWork.Save();

            response.StatusCode = ResponseCode.Success;
            return response;

        }

        public ServiceResponse UpdateAdminRole(int roleId, int adminId, string adminAspId)
        {

            ServiceResponse response = new ServiceResponse();
            if (adminId == 0 || adminAspId == null)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = "Cannot find admin Id.";
                return response;
            }

            Admin? adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(admin => admin.Adminid == adminId);
            if (adminUser == null)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = "Cannot find admin.";
                return response;
            }

            adminUser.Roleid = roleId;
            _unitOfWork.AdminRepository.Update(adminUser);
            _unitOfWork.Save();

            response.StatusCode = ResponseCode.Success;

            return response;
        }

        public ServiceResponse UpdateAdminPersonalDetails(int adminId, List<int> regions, string firstName, string lastName, string email, string phone)
        {
            ServiceResponse response = new ServiceResponse();
            Admin? adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (adminUser == null)
            {
                response.StatusCode = ResponseCode.Error;
                response.Message = "Admin not found";
                return response;
            }


            adminUser.Firstname = firstName;
            adminUser.Lastname = lastName;
            adminUser.Mobile = phone;

            _unitOfWork.AdminRepository.Update(adminUser);

            _unitOfWork.Save();

            List<int> adminRegions = _unitOfWork.AdminRegionRepo.Where(region => region.Adminid == adminId).ToList().Select(x => (int)x.Regionid).ToList();

            List<int> commonRegions = new List<int>();

            // Finding common regions in both new and old lists
            foreach (int region in adminRegions)
            {
                if (regions.Contains(region))
                {
                    commonRegions.Add(region);
                }
            }

            // Removing them from both lists
            foreach (int region in commonRegions)
            {
                adminRegions.Remove(region);
                regions.Remove(region);
            }

            // From difference we will remove regions that were in old list but not in new list
            foreach (int region in adminRegions)
            {
                Adminregion? ar = _unitOfWork.AdminRegionRepo.GetFirstOrDefault(ar => ar.Regionid == region);
                if (ar != null)
                {
                    _unitOfWork.AdminRegionRepo.Remove(ar);
                }
            }

            // And Add the regions that were in new list but not in old list
            foreach (int region in regions)
            {
                Adminregion adminregion = new Adminregion()
                {
                    Adminid = adminId,
                    Regionid = region,
                };

                _unitOfWork.AdminRegionRepo.Add(adminregion);
            }

            _unitOfWork.Save();

            response.StatusCode = ResponseCode.Success;
            return response;
        }
    }
}
