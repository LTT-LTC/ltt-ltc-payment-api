using LTC.Shared.CrossCuttingConcerns.Dtos.BaseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Volo.Abp.Authorization.Permissions;

namespace LTC.Shared.Hosting.Microservices.Authentication
{
    public class PermissionsAuthorizeAttribute : TypeFilterAttribute
    {
        public PermissionsAuthorizeAttribute(params string[] permissions) : base(typeof(PermissionsAuthorizeFilter))
        {
            Arguments = new object[] { permissions };
        }
    }

    /// <summary>
    /// Execute system permission authorization
    /// </summary>
    /// 
    public class PermissionsAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _permissionNames;
        private readonly IPermissionChecker _permissionChecker;

        public PermissionsAuthorizeFilter(string[] permissionNames, IPermissionChecker permissionChecker)
        {
            _permissionNames = permissionNames;
            _permissionChecker = permissionChecker;
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            if (hasAllowAnonymous)
                return;

            if (!context.HttpContext.User.Claims.Any())
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new JsonResult(ApiResult.ErrorResult("Unauthorized", "Unauthorized", HttpStatusCode.Unauthorized));
                return;
            }

            if (_permissionNames == null || !_permissionNames.Any())
                return;

            var multiplePermissionGrantResult = await _permissionChecker.IsGrantedAsync(_permissionNames.First());
            //if (!multiplePermissionGrantResult.AllGranted)
            if (!multiplePermissionGrantResult)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Result = new JsonResult(ApiResult.ErrorResult("Unauthorized", "Unauthorized", HttpStatusCode.Forbidden));
                return;
            }
        }
    }
}
