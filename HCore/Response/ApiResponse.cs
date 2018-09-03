using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ReinhardHolzner.HCore.Result
{
    public partial class ApiResult
    {
        public int StatusCode { get; private set; }

        public Dictionary<string, IEnumerable<string>> Headers { get; private set; }

        public ApiResult(int statusCode)
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, IEnumerable<string>>();
        }

        public ApiResult(Dictionary<string, IEnumerable<string>> headers)
        {
            StatusCode = StatusCodes.Status200OK;
            Headers = headers;
        }

        public ApiResult(int statusCode, Dictionary<string, IEnumerable<string>> headers)
        {
            StatusCode = statusCode;
            Headers = headers;
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

        public ApiResult(Dictionary<string, IEnumerable<string>> headers, TResult result)
            : base(headers)
        {
            Result = result;
        }

        public ApiResult(int statusCode, Dictionary<string, IEnumerable<string>> headers, TResult result)
            : base(statusCode, headers)
        {
            Result = result;
        }
    }
}