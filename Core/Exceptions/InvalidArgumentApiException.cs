using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReinhardHolzner.Core.Exceptions
{
    public class InvalidArgumentApiException : ApiException
    {
        private string _errorCode;
        private ModelStateDictionary _modelState;

        public const string InvalidArgument = "invalid_argument";
        public const string MandateUuidInvalid = "mandate_uuid_invalid";
        public const string MaxPagingOffsetExceeded = "max_paging_offset_exceeded";
        public const string MaxPagingLimitExceeded = "max_paging_limit_exceeded";
        public const string PagingOffsetInvalid = "paging_offset_invalid";
        public const string PagingLimitInvalid = "paging_limit_invalid";
        public const string ApiCredentialsMissing = "api_credentials_missing";
        public const string ClientIdMissing = "client_id_missing";
        public const string ClientIdInvalid = "client_id_invalid";
        public const string ClientSecretMissing = "client_secret_missing";
        public const string ClientSecretInvalid = "client_secret_invalid";
        public const string RedirectUrlNotRequired = "redirect_url_not_required";
        public const string RedirectUrlMissing = "redirect_url_missing";
        public const string RedirectUrlInvalid = "redirect_url_invalid";
        public const string UuidInvalid = "uuid_invalid";
        public const string StateInvalid = "state_invalid";
        public const string CodeInvalid = "code_invalid";
        public const string IssuerMissing = "issuer_missing";
        public const string IssuerInvalid = "issuer_invalid";
        public const string SubjectMissing = "subject_missing";
        public const string SubjectInvalid = "subject_invalid";
        public const string PrivateKeyMissing = "private_key_missing";
        public const string PrivateKeyInvalid = "private_key_invalid";
        public const string UserCredentialsMissing = "user_credentials_missing";
        public const string UserNameMissing = "user_name_missing";
        public const string UserNameInvalid = "user_name_invalid";
        public const string PasswordMissing = "password_missing";
        public const string PasswordInvalid = "password_invalid";
        public const string NameInvalid = "name_invalid";

        public InvalidArgumentApiException(string errorCode, string message) : 
            base(message)
        {
            _errorCode = errorCode;            
        }

        public InvalidArgumentApiException(string errorCode, string message, ModelStateDictionary modelState) :
           this(errorCode, message)
        {
            _modelState = modelState;
        }

        public override int GetStatusCode()
        {
            return StatusCodes.Status400BadRequest;
        }

        public override string GetErrorCode()
        {
            return _errorCode;
        }

        public override object GetObject()
        {
            return _modelState;
        }
    }
}
