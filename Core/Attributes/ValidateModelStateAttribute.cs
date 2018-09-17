using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReinhardHolzner.Core.Exceptions;

namespace ReinhardHolzner.Core.Attributes
{
    /// <summary>
    /// Model state validation attribute
    /// </summary>
    public class ValidateModelStateAttribute : ActionFilterAttribute, IOrderedFilter
    {
        // Setting the order to int.MinValue, using IOrderedFilter, to attempt executing
        // this filter *before* the BaseController's OnActionExecuting.
        public new int Order => int.MinValue + 1;

        /// <summary>
        /// Called before the action method is invoked
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Per https://blog.markvincze.com/how-to-validate-action-parameters-with-dataannotation-attributes/

            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (descriptor != null)
            {
                foreach (var parameter in descriptor.MethodInfo.GetParameters())
                {
                    object args = null;
                    if (context.ActionArguments.ContainsKey(parameter.Name))
                    { 
                        args = context.ActionArguments[parameter.Name];
                    }

                    ValidateAttributes(parameter, args, context.ModelState);
                }
            }

            if (!context.ModelState.IsValid)
            {
                InvalidArgumentApiException invalidArgumentApiException = null;

                string errorMessage = null;

                foreach (var value in context.ModelState.Values)
                {
                    var errors = value.Errors;
                    if (errors == null || errors.Count == 0)
                        continue;

                    var error = errors.First();
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                    {
                        errorMessage = error.ErrorMessage;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(errorMessage))
                    invalidArgumentApiException = new InvalidArgumentApiException(InvalidArgumentApiException.InvalidArgument, errorMessage);
                else
                    invalidArgumentApiException = new InvalidArgumentApiException(InvalidArgumentApiException.InvalidArgument, "The parameter validation failed with unknown reason");

                context.Result = new BadRequestObjectResult(invalidArgumentApiException.SerializeException());
            }
        }

        private void ValidateAttributes(ParameterInfo parameter, object args, ModelStateDictionary modelState)
        {
            foreach (var attributeData in parameter.CustomAttributes)
            {
                var attributeInstance = parameter.GetCustomAttribute(attributeData.AttributeType);

                var validationAttribute = attributeInstance as ValidationAttribute;
                if (validationAttribute != null)
                {
                    var isValid = validationAttribute.IsValid(args);
                    if (!isValid)
                    {
                        modelState.AddModelError(parameter.Name, validationAttribute.FormatErrorMessage(parameter.Name));
                    }
                }
            }
        }
    }
}
