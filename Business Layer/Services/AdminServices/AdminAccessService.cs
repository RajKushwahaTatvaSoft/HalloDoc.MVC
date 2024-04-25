using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace Business_Layer.Services.AdminServices
{
    public class AdminAccessService : IAdminAccessService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminAccessService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public ServiceResponse EditAccessSubmit(EditAccessViewModel model)
        {

            if (model.selectedMenus.Count() < 1)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "At least one screen need to be assigned with ",
                };
            }

            Role? role = _unitOfWork.RoleRepo.GetFirstOrDefault(role => role.Roleid == model.RoleId);

            if (role == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "Role not found",
                };
            }

            bool isRoleWithSameNameExists = _unitOfWork.RoleRepo.Where(x => (model.RoleName == null || x.Name.ToLower().Equals(model.RoleName.ToLower())) && x.Roleid != role.Roleid).Any();

            if (isRoleWithSameNameExists)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "Role With same already exists",
                };
            }

            role.Name = model.RoleName ?? "";
            _unitOfWork.RoleRepo.Update(role);
            _unitOfWork.Save();

            IEnumerable<int> existingMenus = _unitOfWork.RoleMenuRepository.Where(x => x.Roleid == model.RoleId).Select(x => x.Menuid ?? 0).ToList();
            IEnumerable<int> updatedMenus = model.selectedMenus;

            IEnumerable<int> menusToAdd = updatedMenus.Except(existingMenus);
            IEnumerable<int> menusToRemove = existingMenus.Except(updatedMenus);

            foreach (int menuId in menusToAdd)
            {
                Rolemenu rolemenu = new()
                {
                    Roleid = model.RoleId,
                    Menuid = menuId,
                };

                _unitOfWork.RoleMenuRepository.Add(rolemenu);
                _unitOfWork.Save();
            }

            foreach (int menuId in menusToRemove)
            {
                Rolemenu? rolemenu = _unitOfWork.RoleMenuRepository.GetFirstOrDefault(x => x.Roleid == model.RoleId && x.Menuid == menuId);
                if (rolemenu == null)
                {
                    continue;
                }
                _unitOfWork.RoleMenuRepository.Remove(rolemenu);
                _unitOfWork.Save();
            }
            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success,
            };
        }
        public ServiceResponse DeleteRole(int roleId)
        {

            Role? role = _unitOfWork.RoleRepo.GetFirstOrDefault(z => z.Roleid == roleId);

            if (role == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "Error occured while removing role. Please try again later.",
                };
            }

            bool isAnotherUserWithRoleExists = true;

            switch (role.Accounttype)
            {
                case (int)AccountType.Admin:
                    isAnotherUserWithRoleExists = _unitOfWork.AdminRepository.Where(admin => admin.Roleid == roleId).Any();
                    break;

                case (int)AccountType.Physician:
                    isAnotherUserWithRoleExists = _unitOfWork.PhysicianRepository.Where(phy => phy.Roleid == roleId).Any();
                    break;
            }
                
            if (isAnotherUserWithRoleExists)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "Please ensure this role isn't assigned to any user before deleting.",
                };
            }

            role.Isdeleted = true;
            _unitOfWork.RoleRepo.Update(role);
            _unitOfWork.Save();

            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success,
            };

        }

        public AdminProfileViewModel? GetEditAdminAccountModel(string adminAspId)
        {

            Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(ad => ad.Aspnetuserid == adminAspId);
            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == adminAspId);

            if(aspUser == null || admin == null)
            {
                return null;
            }

            int? cityId = admin?.City == null ? null : _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name == admin.City)?.Id;
            string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => admin == null || reg.Regionid == admin.Regionid)?.Name;

            AdminProfileViewModel model = new AdminProfileViewModel()
            {
                StatusId = admin?.Status,
                AdminId = admin?.Adminid,
                AspUserName = aspUser.Username,
                RoleId = admin?.Roleid,
                FirstName = admin?.Firstname,
                LastName = admin?.Lastname,
                Email = admin?.Email,
                PhoneNumber = admin?.Mobile,
                selectedRegions = _unitOfWork.AdminRegionRepo.Where(ar => admin == null || ar.Adminid == admin.Adminid).Select(_ => _.Regionid ?? 0),
                Address1 = admin?.Address1,
                Address2 = admin?.Address2,
                CityId = cityId ?? 0,
                State = state,
                Zip = admin?.Zip,
                AltPhoneNumber = admin?.Altphone,
                RegionId = admin?.Regionid ?? 0,
                roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Admin),
                regions = _unitOfWork.RegionRepository.GetAll(),
                adminMailCities = _unitOfWork.CityRepository.Where(city => admin == null || city.Regionid == admin.Regionid),
            };

            return model;
        }
    }
}
