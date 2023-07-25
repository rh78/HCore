using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Web.Middleware
{
    internal class OpenRequestsMiddleware
    {
        private readonly RequestDelegate _next;

        internal static int OpenRequests = 0;

        public OpenRequestsMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Interlocked.Increment(ref OpenRequests);

            try
            {
                await _next.Invoke(context).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref OpenRequests);
            }
        }
    }
}
