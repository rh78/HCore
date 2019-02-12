using Microsoft.AspNetCore.Http;

namespace HCore.Web.Exceptions
{
    public class ExternalServiceApiException : ApiException
    {
        private readonly string _errorCode;

        public const string CloudStorageFileNotFound = "cloud_storage_file_not_found";
        public const string CloudStorageFileAccessDenied = "cloud_storage_file_access_denied";

        public ExternalServiceApiException(string errorCode, string message) :
            base(message)
        {
            _errorCode = errorCode;

        }

        public ExternalServiceApiException(string errorCode, string message, string uuid, string name) : 
            base(message, uuid, name)
        {
            _errorCode = errorCode;            
        }

        public ExternalServiceApiException(string errorCode, string message, long? uuid, string name) :
            base(message, uuid, name)
        {
            _errorCode = errorCode;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status424FailedDependency;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }
    }
}
