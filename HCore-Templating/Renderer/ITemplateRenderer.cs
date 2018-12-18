using HCore.Templating.Templates.ViewModels.Shared;
using HCore.Tenants.Models;
using System.Threading.Tasks;

namespace HCore.Templating.Renderer
{
    public interface ITemplateRenderer
    {
        Task<string> RenderViewAsync<TModel>(string viewName, TModel model, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;
    }
}
