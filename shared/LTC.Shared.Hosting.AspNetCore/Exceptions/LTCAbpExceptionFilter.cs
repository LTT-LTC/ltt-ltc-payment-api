using LTC.Shared.CrossCuttingConcerns.Dtos.BaseModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Volo.Abp;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Filters;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.ExceptionHandling;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Http;
using Volo.Abp.Validation;

namespace LTC.Shared.Hosting.AspNetCore.Exceptions
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AbpExceptionFilter), typeof(IAsyncExceptionFilter), typeof(IFilterMetadata), typeof(IAbpFilter))]
    public class LTCAbpExceptionFilter(
        IWebHostEnvironment _hostEnvironment
        ) : AbpExceptionFilter
    {
        protected override bool ShouldHandleException(ExceptionContext context)
        {
            return true;
        }

        protected override async Task HandleAndWrapException(ExceptionContext context)
        {
            LogException(context, out var remoteServiceErrorInfo);

            await context.GetRequiredService<IExceptionNotifier>().NotifyAsync(new ExceptionNotificationContext(context.Exception));

            if (context.Exception is AbpAuthorizationException)
            {
                await context.HttpContext.RequestServices.GetRequiredService<IAbpAuthorizationExceptionHandler>()
                    .HandleAsync(context.Exception.As<AbpAuthorizationException>(), context.HttpContext);
            }
            else
            {
                if (!context.HttpContext.Response.HasStarted)
                {
                    context.HttpContext.Response.Headers.Append(AbpHttpConsts.AbpErrorFormat, "true");
                    context.HttpContext.Response.StatusCode = (int)context
                        .GetRequiredService<IHttpExceptionStatusCodeFinder>()
                        .GetStatusCode(context.HttpContext, context.Exception);
                }
                else
                {
                    var logger = context.GetService<ILogger<AbpExceptionFilter>>(NullLogger<AbpExceptionFilter>.Instance)!;
                    logger.LogWarning("HTTP response has already started, cannot set headers and status code!");
                }

                var statusCode = HttpStatusCode.BadRequest;
                switch (context.Exception)
                {
                    case UnauthorizedAccessException:
                        statusCode = HttpStatusCode.Unauthorized;
                        break;
                    case AbpValidationException:
                        statusCode = HttpStatusCode.BadRequest;
                        break;
                    case EntityNotFoundException:
                        statusCode = HttpStatusCode.NotFound;
                        break;
                    case UserFriendlyException:
                        statusCode = HttpStatusCode.BadRequest;
                        break;
                    case BusinessException:
                        statusCode = HttpStatusCode.Conflict;
                        break;
                    case NotImplementedException:
                        statusCode = HttpStatusCode.NotImplemented;
                        break;
                    default:
                        statusCode = HttpStatusCode.InternalServerError;
                        break;
                }

                context.Result = new JsonResult(ApiResult.ErrorResult(remoteServiceErrorInfo.Message, remoteServiceErrorInfo.Code, remoteServiceErrorInfo.Details, statusCode));

                if (_hostEnvironment.EnvironmentName.EndsWith("Development"))
                    context.Result = new JsonResult(ApiResult.ErrorResult(context.Exception.Message, null, HttpStatusCode.InternalServerError));
                else
                    context.Result = new JsonResult(ApiResult.ErrorResult("Unexpected error. Try again later!", "Unknown", HttpStatusCode.InternalServerError));

                context.HttpContext.Response.StatusCode = (int)statusCode;
            }

            context.ExceptionHandled = true; //Handled!
        }
    }
}
