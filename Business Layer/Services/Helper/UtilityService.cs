using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc;

namespace Business_Layer.Services.Helper
{
    public class UtilityService : IUtilityService
    {

        private readonly IUnitOfWork _unitOfWork;
        public UtilityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            IEnumerable<City> cities = _unitOfWork.CityRepository.Where(city => city.Regionid == regionId).OrderBy(_ => _.Name);
            return cities;
        }

        public string GenerateUserName(AccountType accountType, string firstName, string lastName)
        {
            string prefix = "";

            switch (accountType)
            {
                case AccountType.Admin:
                    prefix = "AD";
                    break;

                case AccountType.Physician:
                    prefix = "MD";
                    break;

                case AccountType.Patient:
                    prefix = "PT";
                    break;
            }

            string userName = prefix + "." + lastName + "." + firstName.ElementAt(0);

            int count = 0;
            while (_unitOfWork.AspNetUserRepository.GetAll().Any(aspUser => aspUser.Username == userName))
            {
                count++;
                userName = userName + count.ToString();
            }

            return userName;
        }


        public string GenerateConfirmationNumber(User user)
        {
            string? regionAbbr = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == user.Regionid)?.Abbreviation;

            DateTime todayStart = DateTime.Now.Date;
            int count = _unitOfWork.RequestRepository.Where(req => req.Createddate > todayStart).Count();

            string confirmationNumber = regionAbbr + user.Createddate.Day.ToString("D2") + user.Createddate.Month.ToString("D2") + (user.Lastname?.Substring(0, 2).ToUpper() ?? "NA") + user.Firstname.Substring(0, 2).ToUpper() + (count + 1).ToString("D4");
            return confirmationNumber;
        }

        public SessionUser? GetSessionUserFromAdminId(int adminId)
        {
            Admin? adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(admin => admin.Adminid == adminId);

            if(adminUser == null)
            {
                return null;
            }

            SessionUser sessionUser = new SessionUser()
            {
                UserId = adminUser.Adminid,
                UserAspId = adminUser.Aspnetuserid ?? "",
                Email = adminUser.Email,
                AccountTypeId = (int)AccountType.Admin,
                RoleId = adminUser.Roleid ?? 0,
                UserName = adminUser.Firstname + (String.IsNullOrEmpty(adminUser.Lastname) ? "" : " " + adminUser.Lastname),
            };

            return sessionUser;
        }

    }
}
