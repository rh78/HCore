using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HCore.Web.Exceptions
{
    public class InvalidArgumentApiException : ApiException
    {
        private readonly string _errorCode;
        private readonly ModelStateDictionary _modelState;

        public const string InvalidArgument = "invalid_argument";
        public const string MandateUuidInvalid = "mandate_uuid_invalid";
        public const string MandateUuidTooLong = "mandate_uuid_too_long";
        public const string MaxPagingOffsetExceeded = "max_paging_offset_exceeded";
        public const string MaxPagingLimitExceeded = "max_paging_limit_exceeded";
        public const string PagingOffsetInvalid = "paging_offset_invalid";
        public const string PagingLimitInvalid = "paging_limit_invalid";
        public const string ApiCredentialsMissing = "api_credentials_missing";
        public const string ClientIdMissing = "client_id_missing";
        public const string ClientIdInvalid = "client_id_invalid";
        public const string ClientIdTooLong = "client_id_too_long";
        public const string ClientSecretMissing = "client_secret_missing";
        public const string ClientSecretInvalid = "client_secret_invalid";
        public const string ClientSecretTooLong = "client_secret_too_long";
        public const string RedirectUrlNotRequired = "redirect_url_not_required";
        public const string RedirectUrlMissing = "redirect_url_missing";
        public const string RedirectUrlInvalid = "redirect_url_invalid";
        public const string RedirectUrlTooLong = "redirect_url_too_long";
        public const string RedirectUrlMustBeAbsolute = "redirect_url_must_be_absolute";
        public const string UuidMissing = "uuid_missing";
        public const string UuidInvalid = "uuid_invalid";
        public const string StateInvalid = "state_invalid";
        public const string CodeMissing = "code_missing";
        public const string CodeInvalid = "code_invalid";
        public const string CodeTooLong = "code_too_long";
        public const string IssuerMissing = "issuer_missing";
        public const string IssuerInvalid = "issuer_invalid";
        public const string IssuerTooLong = "issuer_too_long";
        public const string SubjectMissing = "subject_missing";
        public const string SubjectInvalid = "subject_invalid";
        public const string SubjectTooLong = "subject_too_long";
        public const string PrivateKeyMissing = "private_key_missing";
        public const string PrivateKeyNoDelimiters = "private_key_no_delimiters";
        public const string PrivateKeyNoPrivateKey = "private_key_no_private_key";
        public const string PrivateKeyInvalid = "private_key_invalid";
        public const string PrivateKeyTooLong = "private_key_too_long";
        public const string UserCredentialsMissing = "user_credentials_missing";
        public const string UserNameMissing = "user_name_missing";
        public const string UserNameInvalid = "user_name_invalid";
        public const string UserNameTooLong = "user_name_too_long";
        public const string PasswordMissing = "password_missing";
        public const string PasswordInvalid = "password_invalid";
        public const string PasswordTooShort = "password_too_short";
        public const string PasswordTooLong = "password_too_long";
        public const string PasswordConfirmationMissing = "password_confirmation_missing";
        public const string PasswordConfirmationNoMatch = "password_confirmation_no_match";
        public const string NameMissing= "name_missing";
        public const string NameInvalid = "name_invalid";
        public const string NameTooLong = "name_too_long";
        public const string TooManyUpdateRecords = "too_many_update_records";
        public const string NoRecordsSpecified = "no_records_specified";
        public const string UserGroupUuidMissing = "user_group_uuid_missing";
        public const string UserGroupUuidInvalid = "user_group_uuid_invalid";
        public const string UserUuidMissing = "user_uuid_missing";
        public const string UserUuidInvalid = "user_uuid_invalid";
        public const string EmailMissing = "email_missing";
        public const string EmailInvalid = "email_invalid";
        public const string PhoneNumberMissing = "phone_number_missing";
        public const string PhoneNumberInvalid = "phone_number_invalid";
        public const string ValidationFailed = "validation_failed";
        public const string DuplicateUserName = "duplicate_user_name";
        public const string PasswordRequiresNonAlphanumeric = "password_requires_non_alphanumeric";
        public const string SecurityTokenInvalid = "security_token_invalid";
        public const string ScrollUuidInvalid = "scroll_uuid_invalid";
        public const string ScrollUuidTooLong = "scroll_uuid_too_long";        

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
