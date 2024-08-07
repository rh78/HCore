﻿using System.IO;
using HCore.Tenants.Models;
﻿using HCore.Templating.Templates.ViewModels.Shared;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HCore.Templating.Renderer
{
    public interface ITemplateRenderer
    {
        Task<string> RenderViewAsync<TModel>(string viewName, TModel model, bool? isPortals, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;

        Task<MemoryStream> RenderPdfAsync<TModel>(string viewName, TModel model, bool? isPortals, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;

        Task<MemoryStream> RenderPngAsync<TModel>(string viewName, TModel model, int width, int height, bool? isPortals, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;
    }
}
