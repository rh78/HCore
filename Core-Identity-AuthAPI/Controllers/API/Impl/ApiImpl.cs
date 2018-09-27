using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ReinhardHolzner.Core.Identity.AuthAPI.Exceptions;
using ReinhardHolzner.Core.Web.Exceptions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReinhardHolzner.Core.Identity.AuthAPI.Controllers.API.Impl
{
    public abstract class ApiImpl
    {
        private ILogger _logger;

        public ApiImpl(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ApiImpl>();
        }

        protected void ValidateModel(object model)
        {
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(model, context, validationResults, true);

            if (!isValid)
            {
                var validationResult = validationResults[0];

                string reason = "The validation of the user data failed";
                if (validationResults.Count > 0 && !string.IsNullOrEmpty(validationResults[0].ErrorMessage))
                    reason = validationResults[0].ErrorMessage;

                throw new AuthAPIInvalidArgumentApiException(AuthAPIInvalidArgumentApiException.ValidationFailed, reason);
            }
        }

        protected void HandleIdentityError(IEnumerable<IdentityError> errors)
        {
            var enumerator = errors.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var error = enumerator.Current;

                if (Equals(error.Code, "DuplicateUserName"))
                    throw new AuthAPIInvalidArgumentApiException(AuthAPIInvalidArgumentApiException.DuplicateUserName, error.Description);
                else if (Equals(error.Code, "PasswordRequiresNonAlphanumeric"))
                    throw new AuthAPIInvalidArgumentApiException(AuthAPIInvalidArgumentApiException.PasswordRequiresNonAlphanumeric, error.Description);

                _logger.LogWarning($"Identity error was not covered: {error}");
            } else
            {
                _logger.LogWarning("Unknown identity error occured");
            }

            throw new InternalServerErrorApiException();            
        }
    }
}
