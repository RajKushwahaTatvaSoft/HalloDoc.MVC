using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Business_Layer.Services.Helper.Interface;
using AspNetCore;
using Business_Layer.Repository.IRepository;

namespace HalloDoc.MVC.Services
{
    public class RoleAuthorize : Attribute, IAuthorizationFilter
    {

        private readonly int _menuId;
        public RoleAuthorize(int menuId = 0)
        {
            _menuId = menuId;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (_menuId == 0)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            IJwtService? _jwtService = context.HttpContext.RequestServices.GetService<IJwtService>();
            IUnitOfWork? _unitOfWork = context.HttpContext.RequestServices.GetService<IUnitOfWork>();

            if (_jwtService == null || _unitOfWork == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                return;
            }

            var token = context.HttpContext.Request.Cookies["hallodoc"];

            if (token == null || !_jwtService.ValidateToken(token, out JwtSecurityToken jwtToken))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                return;
            }

            int roleId = Convert.ToInt32(jwtToken.Claims.FirstOrDefault(c => c.Type == "roleId")?.Value);

            Role? role = _unitOfWork.RoleRepo.GetFirstOrDefault(role => role.Roleid == roleId);

            if (role == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                return;
            }

            IEnumerable<Rolemenu> roleMenus = _unitOfWork.RoleMenuRepository.Where(rm => rm.Roleid == roleId);

            var sessionRef = context.HttpContext.Session;

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var rolemenu in roleMenus)
            {
                stringBuilder.Append(rolemenu.Menuid.ToString() + "-");
            }

            stringBuilder.Length--;

            sessionRef.SetString("roleMenu", stringBuilder.ToString());

            if (!roleMenus.Any(rm => rm.Menuid == _menuId))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "AccessDenied" }));
                return;
            }

        }

    }
}
