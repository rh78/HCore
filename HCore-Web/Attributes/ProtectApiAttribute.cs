using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HCore.Web.Attributes
{
    public class ProtectApiAttribute : ActionFilterAttribute, IOrderedFilter
    {
        // Setting the order to int.MinValue, using IOrderedFilter, to attempt executing
        // this filter *before* the BaseController's OnActionExecuting.
        public new int Order => int.MinValue;

        /// <summary>
        /// Called before the action method is invoked
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            int? port = context.HttpContext.Connection.LocalPort;

            if (port == 443 || port == 80)
            {
                // only standard ports are open to the public
                // everything else is API

                context.Result = new NotFoundResult();
            }
        }
    }
}
