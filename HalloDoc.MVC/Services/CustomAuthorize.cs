using Business_Layer.Interface;
using Business_Layer.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NuGet.Protocol;
using System.IdentityModel.Tokens.Jwt;

namespace HalloDoc.MVC.Services
{

    public class CustomAuthorize : Attribute, IAuthorizationFilter
    {
        private readonly int _roleId;

        public CustomAuthorize(int roleId = 0)
        {
            _roleId = roleId;
        }


        
        public void OnAuthorization(AuthorizationFilterContext context)
        {


            if(_roleId == 0)
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

            var token = context.HttpContext.Request.Cookies["hallodoc"];
                        
            if (token == null || !jwtService.ValidateToken(token, out JwtSecurityToken jwtToken))
            {

                //if (IsAjaxRequest(context.HttpContext.Request))
                //{
                //    context.Result = new JsonResult(new { error = "Access denied", redirectToLogin = true })
                //    {
                //        StatusCode = StatusCodes.Status403Forbidden
                //    };
                //}
                //else
                //{
                //    context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Login", action = "Index" }));
                //}

                //return;

                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var roleClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "accountTypeId");
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

        //public static bool IsAjaxRequest(this HttpRequest request)
        //{
        //    if (request == null)
        //        throw new ArgumentNullException("request");
        //    if (request["X-Requested-With"] == "XMLHttpRequest")
        //        return true;
        //    if (request.Headers != null)
        //        return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        //    return false;
        //}

    }
}
