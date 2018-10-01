using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ReinhardHolzner.Core.Web.Exceptions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReinhardHolzner.Core.Identity.AuthAPI.Controllers.API.Impl
{
    public abstract class ApiImpl : Web.API.Impl.ApiImpl
    {
        public const int MaxUserUuidLength = 50;
        public const int MaxUserNameLength = 50;
        public const int MinPasswordLength = 6;
        public const int MaxPasswordLength = 50;
        public const int MaxCodeLength = 50;

        private ILogger _logger;

        public ApiImpl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ApiImpl>();
        }

        public static string ProcessUserUuid(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidMissing, "The user UUID is missing");

            if (!Uuid.IsMatch(userUuid))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            if (userUuid.Length > MaxUserUuidLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.UserUuidInvalid, "The user UUID is invalid");

            return userUuid;
        }

        public static string ProcessEmail(string email, bool required)
        {
            if (string.IsNullOrEmpty(email))
            {
                if (required)
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailMissing, "The email address is missing");

                return null;
            }

            if (!new EmailAddressAttribute().IsValid(email))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.EmailInvalid, "The email address is invalid");

            return email;
        }

        public static string ProcessPhoneNumber(string phoneNumber, bool required)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                if (required)
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberMissing, "The phone number is missing");

                return null;
            }

            if (!new PhoneAttribute().IsValid(phoneNumber))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PhoneNumberInvalid, "The phone number is invalid");

            return phoneNumber;
        }

        public static string ProcessPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordMissing, "The password is missing");

            if (!SafeString.IsMatch(password))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordInvalid, "The password contains invalid characters");

            if (password.Length < MinPasswordLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordTooShort, "The password is too short");

            if (password.Length > MaxPasswordLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordTooLong, "The password is too long");

            return password;
        }

        public static string ProcessPasswordConfirmation(string password, string passwordConfirmation)
        {
            if (string.IsNullOrEmpty(passwordConfirmation))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordConfirmationMissing, "The password confirmation is missing");

            if (!Equals(password, passwordConfirmation))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordConfirmationNoMatch, "The password confirmation is not matching the password");

            return password;
        }

        public static string ProcessCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeMissing, "The code is missing");

            if (!SafeString.IsMatch(code))
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeInvalid, "The code contains invalid characters");

            if (code.Length > MaxCodeLength)
                throw new InvalidArgumentApiException(InvalidArgumentApiException.CodeTooLong, "The code is too long");

            return code;
        }

        protected void HandleIdentityError(IEnumerable<IdentityError> errors)
        {
            var enumerator = errors.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var error = enumerator.Current;

                if (Equals(error.Code, "DuplicateUserName"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.DuplicateUserName, "This user name already exists");
                else if (Equals(error.Code, "PasswordRequiresNonAlphanumeric"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.PasswordRequiresNonAlphanumeric, "The password requires non alphanumeric characters");
                else if (Equals(error.Code, "InvalidToken"))
                    throw new InvalidArgumentApiException(InvalidArgumentApiException.SecurityTokenInvalid, "The security token is invalid or expired");
                else if (Equals(error.Code, "PasswordMismatch"))
                    throw new UnauthorizedApiException(UnauthorizedApiException.PasswordDoesNotMatch, "The password does not match our records");

                _logger.LogWarning($"Identity error was not covered: {error.Code}");
            } else
            {
                _logger.LogWarning("Unknown identity error occured");
            }

            throw new InternalServerErrorApiException();            
        }        
    }
}
