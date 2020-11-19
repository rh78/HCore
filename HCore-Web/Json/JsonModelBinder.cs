using HCore.Web.Exceptions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HCore.Web.Json
{
    // from https://code.i-harness.com/en/q/2773832

    public class JsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Check the value sent in

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

                // Attempt to convert the input value
                var valueAsString = valueProviderResult.FirstValue;

                try
                {
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject(valueAsString, bindingContext.ModelType);

                    if (result != null)
                    {
                        bindingContext.Result = ModelBindingResult.Success(result);
                        return Task.CompletedTask;
                    }
                } 
                catch (JsonReaderException)
                {
                    throw new RequestFailedApiException(RequestFailedApiException.ArgumentInvalid, $"Parameter {bindingContext.ModelName} is not valid JSON value");
                }
            }

            return Task.CompletedTask;
        }
    }
}
