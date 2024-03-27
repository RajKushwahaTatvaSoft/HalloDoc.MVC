using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Business_Layer.Repository;

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

            var jwtService = context.HttpContext.RequestServices.GetService<IJwtService>();
            var dbService = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();

            if (jwtService == null || dbService == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var token = context.HttpContext.Request.Cookies["hallodoc"];

            if (token == null || !jwtService.ValidateToken(token, out JwtSecurityToken jwtToken))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var roleId = Convert.ToInt32(jwtToken.Claims.FirstOrDefault(c => c.Type == "roleId")?.Value);

            IEnumerable<Rolemenu> roleMenus = dbService.Rolemenus.Where(rm => rm.Roleid == roleId);

            if (!roleMenus.Any(rm => rm.Menuid == _menuId))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "AccessDenied" }));
                return;
            }

        }

    }
}
