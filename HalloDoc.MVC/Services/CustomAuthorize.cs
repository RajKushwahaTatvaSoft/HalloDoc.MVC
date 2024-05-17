using Business_Layer.Services.Helper.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace HalloDoc.MVC.Services
{

    public class CustomAuthorize : Attribute, IAuthorizationFilter
    {
        private readonly int _accountTypeId;

        public CustomAuthorize(int accountType = 0)
        {
            _accountTypeId = accountType;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (_accountTypeId == 0)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                return;
            }

            var jwtService = context.HttpContext.RequestServices.GetService<IJwtService>();

            if (jwtService == null)
            {
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var token = context.HttpContext.Request.Cookies["hallodoc"];

            if (token == null || !jwtService.ValidateToken(token, out JwtSecurityToken jwtToken))
            {
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                return;
            }

            var roleClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "accountTypeId");
            var userIdClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "userId");
            var userNameClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "userName");
            var userAspIdClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "userAspId");

            context.HttpContext.Session.SetString("userName", userNameClaim?.Value ?? "");
            context.HttpContext.Session.SetString("userAspId", userAspIdClaim?.Value ?? "");
            context.HttpContext.Request.Headers.Add("userId", userIdClaim?.Value);
            context.HttpContext.Request.Headers.Add("userName", userNameClaim?.Value);
            context.HttpContext.Request.Headers.Add("userAspId", userAspIdClaim?.Value);

            if (roleClaim == null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Guest", action = "Index" }));
                context.HttpContext.Response.Cookies.Delete("hallodoc");
                return;
            }

            if (!(_accountTypeId == Convert.ToInt32(roleClaim.Value)))
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
