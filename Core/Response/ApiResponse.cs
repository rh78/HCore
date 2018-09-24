using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;

namespace ReinhardHolzner.Core.Result
{
    public partial class ApiResult
    {
        public int StatusCode { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }

        public ApiResult(int statusCode)
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, string>();
        }

        public ApiResult(Dictionary<string, string> headers)
        {
            StatusCode = StatusCodes.Status200OK;
            Headers = headers;
        }

        public ApiResult(int statusCode, Dictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Headers = headers;
        }

        public ApiResult(string locationHeader)
        {
            StatusCode = (int) HttpStatusCode.Created;

            Headers = new Dictionary<string, string>();
            Headers.Add(HeaderNames.Location, locationHeader);
        }
    }

    public partial class ApiResult<TResult> : ApiResult
    {
        public TResult Result { get; private set; }

        public ApiResult(TResult result)
            : base(StatusCodes.Status200OK)
        {
            Result = result;
        }

        public ApiResult(Dictionary<string, string> headers, TResult result)
            : base(headers)
        {
            Result = result;
        }

        public ApiResult(int statusCode, Dictionary<string, string> headers, TResult result)
            : base(statusCode, headers)
        {
            Result = result;
        }

        public ApiResult(string locationHeader, TResult result)
            : base(locationHeader)
        {
            Result = result;
        }        
    }
}