using System.Collections.Generic;
using HCore.Translations.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace HCore.PagesUI.Classes.Pages
{
    public abstract class BasePageModelProvidingJsonModelData : PageModel
    {
        public abstract string ModelAsJson { get; }

        private string _scriptNonce = null;

        public string GetScriptNonce()
        {
            if (_scriptNonce == null)
            {
                _scriptNonce = HttpContext.GetScriptNonce();
            }

            return _scriptNonce;
        }

        public virtual string ValidationErrorsAsJson =>
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