using LTC.Shared.CrossCuttingConcerns.Dtos.BaseModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace LTC.Shared.Hosting.Microservices
{
    public class ApplicationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger<ApplicationExceptionFilterAttribute> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ApplicationExceptionFilterAttribute(ILogger<ApplicationExceptionFilterAttribute> logger, IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, context.Exception.Message);
            if (context.Exception != null)
            {
                var statusCode = HttpStatusCode.BadRequest;
                context.HttpContext.Response.ContentType = "application/json";
                if (context.Exception is EntityNotFoundException)
                    statusCode = HttpStatusCode.NotFound;
                else if (context.Exception is UserFriendlyException || context.Exception is BusinessException)
                {
                    //statusCode = HttpStatusCode.Conflict;
                    var exception = context.Exception as BusinessException;
                    context.Result = new JsonResult(ApiResult.ErrorResult(exception.Message, exception.Code, statusCode));
                }
                else if (context.Exception is AbpAuthorizationException)
                {
                    statusCode = HttpStatusCode.Unauthorized;
                    context.Result = new JsonResult(ApiResult.ErrorResult("Unauthorized", "Unauthorized", statusCode));
                }
                else if (context.Exception is AbpValidationException)
                {
                    var exception = context.Exception as AbpValidationException;
                    string message = string.Join("\r\n", exception.ValidationErrors.Select(x => x.ErrorMessage));
                    context.Result = new JsonResult(ApiResult.ErrorResult(message, "VALIDATE_ERROR", statusCode));
                }
                else
                {
                    if (_hostEnvironment.EnvironmentName.EndsWith("Development"))
                        context.Result = new JsonResult(ApiResult.ErrorResult(context.Exception.Message, null, HttpStatusCode.InternalServerError));
                    else
                        context.Result = new JsonResult(ApiResult.ErrorResult("Unexpected error. Try again later", "Unknown", HttpStatusCode.InternalServerError));

                    context.HttpContext.Response.ContentType = "application/json";
                    statusCode = HttpStatusCode.InternalServerError;
                }

                context.HttpContext.Response.StatusCode = (int)statusCode;
                context.ExceptionHandled = true;
            }
        }
    }
}
