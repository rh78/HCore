using System.IO;
using HCore.Tenants.Models;
﻿using HCore.Templating.Templates.ViewModels.Shared;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HCore.Templating.Renderer
{
    public interface ITemplateRenderer
    {
        Task<string> RenderViewAsync<TModel>(string viewName, TModel model, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;

        Task<MemoryStream> RenderPdfAsync<TModel>(string viewName, TModel model, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel;
    }
}
