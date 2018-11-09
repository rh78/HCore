using HCore.Templating.Templates.ViewModels.Shared;
using System.Threading.Tasks;

namespace HCore.Templating.Generic
{
    public interface ITemplateRenderer
    {
        Task<string> RenderViewAsync<TModel>(string viewName, TModel model)
            where TModel : TemplateViewModel;
    }
}
