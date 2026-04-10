using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LTC.Shared.CrossCuttingConcerns.Dtos.BaseModel
{
    public class ErrorResultDto
    {
        public ErrorResultDto(string message, string? code = null)
        {
            Message = message;
            Code = code;
        }

        public ErrorResultDto(string message, string? code = null, dynamic? details = null)
        {
            Message = message;
            Code = code;
            Details = details;
        }

        public string? Code { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }
    }

    public class BaseResultDto
    {
        public virtual string Status { get; set; }
        public virtual ErrorResultDto? Error { get; set; }
        public virtual HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public virtual string Message { get; set; }
        public virtual string SystemName { get; set; }
    }

    public class ApiResult : BaseResultDto
    {
        public static ApiResult OkResult(HttpStatusCode httpStatusCode = HttpStatusCode.OK, string message = "Success")
        {
            return new ApiResult
            {
                StatusCode = httpStatusCode,
                Message = message
            };
        }

        public static ApiResult ErrorResult(string message, string messageCode = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            return new ApiResult
            {
                Message = "Not Success",
                Status = "NOTOK",
                StatusCode = httpStatusCode,
                Error = new ErrorResultDto(message, messageCode)
            };
        }

        public static ApiResult ErrorResult(string message, string messageCode = null, string details = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            return new ApiResult
            {
                Message = "Not Success",
                Status = "NOTOK",
                StatusCode = httpStatusCode,
                Error = new ErrorResultDto(message, messageCode, details)
            };
        }

        public static ApiResult ValidateModelErrorResult(dynamic validationErrors)
        {
            return new ApiResult
            {
                Message = "Error",
                Status = "NOTOK",
                StatusCode = HttpStatusCode.BadRequest,
                Error = new ErrorResultDto("Data is not valid!", null, validationErrors)
            };
        }
    }

    public class ApiResult<TData> : BaseResultDto
    {
        /// <summary>
        /// Return success with data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ApiResult<TData> OkResult(TData data, HttpStatusCode httpStatusCode = HttpStatusCode.OK, string message = "Success")
        {
            return new ApiResult<TData>
            {
                Data = data,
                StatusCode = httpStatusCode,
                Message = message
            };
        }

        /// <summary>
        /// Return error with data
        /// </summary>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static ApiResult ErrorResult(string message, string messageCode = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            return new ApiResult
            {
                Message = message,
                Status = "NOTOK",
                StatusCode = httpStatusCode,
                Error = new ErrorResultDto(message, messageCode)
            };
        }

        public TData Data { get; set; }
    }
}
