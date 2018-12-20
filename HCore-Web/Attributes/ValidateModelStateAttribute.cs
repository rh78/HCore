using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HCore.Web.Exceptions;

namespace HCore.Web.Attributes
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

            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
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
                    throw new RequestFailedApiException(RequestFailedApiException.ArgumentInvalid, errorMessage);
                else
                    throw new RequestFailedApiException(RequestFailedApiException.ArgumentInvalid, "The parameter validation failed with unknown reason");                
            }
        }

        private void ValidateAttributes(ParameterInfo parameter, object args, ModelStateDictionary modelState)
        {
            foreach (var attributeData in parameter.CustomAttributes)
            {
                var attributeInstance = parameter.GetCustomAttribute(attributeData.AttributeType);

                if (attributeInstance is ValidationAttribute validationAttribute)
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
