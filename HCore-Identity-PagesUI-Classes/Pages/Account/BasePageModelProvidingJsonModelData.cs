using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Newtonsoft.Json;
using HCore.Translations.Resources;

namespace HCore.Identity.PagesUI.Classes.Pages.Account
{
    public abstract class BasePageModelProvidingJsonModelData : PageModel
    {
        public abstract string ModelAsJson { get; }

        public virtual string ValidationErrors =>
            JsonConvert.SerializeObject(
                GetValidationErrors(), 
                new JsonSerializerSettings()
                {
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                }
            )
        ;

        private List<string> GetValidationErrors()
        {
            var result = new List<string>();

            foreach (var value in ModelState.Values)
            {
                if (value.Errors != null)
                {
                    foreach (var error in value.Errors)
                    {
                        if (!string.IsNullOrEmpty(error.ErrorMessage))
                            result.Add(error.ErrorMessage);
                        else if (error.Exception != null)
                            result.Add(Messages.internal_server_error);
                    }
                }
            }

            return result;
        }
    }
}