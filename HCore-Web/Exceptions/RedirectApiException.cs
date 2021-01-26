using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class RedirectApiException : ApiException
    {
        public string Location { get; }

        public RedirectApiException(string location) :
            base(ForbiddenApiException.RedirectNecessary, "A redirect is necessary")

        {
            Location = location;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status307TemporaryRedirect;
        }

        public override string GetErrorCode()
        {
            return ForbiddenApiException.RedirectNecessary;
        }
    }
}
