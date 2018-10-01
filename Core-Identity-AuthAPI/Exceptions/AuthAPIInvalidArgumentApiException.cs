using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReinhardHolzner.Core.Web.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReinhardHolzner.Core.Identity.AuthAPI.Exceptions
{
    public class AuthAPIInvalidArgumentApiException : InvalidArgumentApiException
    {
        public const string ValidationFailed = "validation_failed";
        public const string DuplicateUserName = "duplicate_user_name";
        public const string PasswordRequiresNonAlphanumeric = "password_requires_non_alphanumeric";
        public const string SecurityTokenInvalid = "security_token_invalid";

        public AuthAPIInvalidArgumentApiException(string errorCode, string message) 
            : base(errorCode, message)
        {
        }

        public AuthAPIInvalidArgumentApiException(string errorCode, string message, ModelStateDictionary modelState) 
            : base(errorCode, message, modelState)
        {
        }        
    }
}
