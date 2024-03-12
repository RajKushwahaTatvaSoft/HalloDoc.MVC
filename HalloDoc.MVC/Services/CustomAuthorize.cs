using Business_Layer.Interface;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace HalloDoc.MVC.Services
{
    public enum AllowRole
    {
        Admin = 1,
        Patient = 2,
        Physician = 3
    }

    public class CustomAuthorize : Attribute, IAuthorizationFilter
    {
        private readonly int _roleId;

        public CustomAuthorize(int roleId = 0)
        {
            _roleId = roleId;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if(_roleId == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var jwtService = context.HttpContext.RequestServices.GetService<IJwtService>();

            if (jwtService == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var request = context.HttpContext.Request;
            var token = request.Cookies["hallodoc"];

            if (token == null || !jwtService.ValidateToken(token, out JwtSecurityToken jwtToken))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var roleClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "roleId");
            var userIdClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "userId");
            var userNameClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "userName");
            context.HttpContext.Request.Headers.Add("userId",userIdClaim.Value);
            context.HttpContext.Request.Headers.Add("userName",userNameClaim.Value);

            if (roleClaim == null)
            {
                
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            if (!(_roleId == Convert.ToInt32(roleClaim.Value)))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "AccessDenied" }));
                return;
            }

        }
    }
}
