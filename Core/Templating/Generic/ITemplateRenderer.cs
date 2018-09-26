using System.Threading.Tasks;

namespace ReinhardHolzner.Core.Templating.Generic
{
    public interface ITemplateRenderer
    {
        Task<string> RenderViewAsync<TModel>(string viewName, TModel model);
    }
}
