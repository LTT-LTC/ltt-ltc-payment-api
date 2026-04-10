using LTC.Shared.CrossCuttingConcerns.Dtos.BaseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Volo.Abp.AspNetCore.Mvc;

namespace LTC.Shared.Hosting.Microservices.HttpApi
{
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ApiResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResult))]
    [ApiController]
    [Authorize]
    public abstract class AppControllerBase : AbpControllerBase
    {
        protected virtual ObjectResult Success(HttpStatusCode code = HttpStatusCode.OK, string message = "Success")
        {
            return StatusCode((int)code, ApiResult.OkResult(code, message));
        }

        protected virtual ObjectResult Success<TData>(TData data, HttpStatusCode code = HttpStatusCode.OK, string message = "Success")
        {
            return StatusCode((int)code, ApiResult<TData>.OkResult(data, code, message));
        }
    }
}
