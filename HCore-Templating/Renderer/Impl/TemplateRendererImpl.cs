using HCore.Templating.Templates.ViewModels.Shared;
using HCore.Tenants.Models;
using HCore.Tenants.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using jsreport.Shared;
using jsreport.Types;
using HCore.Translations.Resources;

// see https://scottsauber.com/2018/07/07/walkthrough-creating-an-html-email-template-with-razor-and-razor-class-libraries-and-rendering-it-from-a-net-standard-class-library/

namespace HCore.Templating.Renderer.Impl
{
    internal class TemplateRendererImpl : ITemplateRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpContext _context;
        private readonly IRenderService _renderService;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public TemplateRendererImpl(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider, 
            IHttpContextAccessor accessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _context = accessor.HttpContext;

            _renderService = _serviceProvider.GetService<IRenderService>();

            _tenantInfoAccessor = _serviceProvider.GetService<ITenantInfoAccessor>();            
        }

        public async Task<string> RenderViewAsync<TModel>(string viewName, TModel model, bool isPortals, ITenantInfo tenantInfo = null)
            where TModel : TemplateViewModel
        {
            EnrichTenantInfo(model, isPortals, tenantInfo);

            var actionContext = GetActionContext();
            var view = FindView(actionContext, viewName);

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    new ViewDataDictionary<TModel>(
                        metadataProvider: new EmptyModelMetadataProvider(),
                        modelState: new ModelStateDictionary())
                    {
                        Model = model
                    },
                    new TempDataDictionary(
                        actionContext.HttpContext,
                        _tempDataProvider),
                    output,
                    new HtmlHelperOptions());
                viewContext.RouteData = _context?.GetRouteData() ?? new RouteData();

                await view.RenderAsync(viewContext).ConfigureAwait(false);

                return output.ToString();
            }
        }

        public async Task<MemoryStream> RenderPdfAsync<TModel>(string viewName, TModel model, bool isPortals, ITenantInfo tenantInfo = null) 
            where TModel : TemplateViewModel
        {
            if (_renderService == null)
                throw new Exception("JSReport render service is not available");

            var htmlContent = await RenderViewAsync(viewName, model, isPortals, tenantInfo).ConfigureAwait(false);
            
            var pdf = await _renderService.RenderAsync(new RenderRequest()
            {
                Template = new Template()
                {
                    Content = htmlContent,
                    Engine = Engine.None,
                    Recipe = Recipe.ChromePdf,
                    Chrome = new Chrome()
                    {
                        DisplayHeaderFooter = true,
                        MarginTop = "2cm",
                        MarginLeft = "2cm",
                        MarginRight = "2cm",
                        MarginBottom = "2cm",
                        HeaderTemplate = "",
                        FooterTemplate = "<span style='color:black; font-size:8pt; font-family: sans-serif !important; width:100%;text-align:right;margin-right:2cm;'>"
                                         + Messages.page
                                         + " <span class=\"pageNumber\"></span> "
                                         + Messages.of
                                         + " <span class=\"totalPages\"></span></span>"
                    }
                }
            }).ConfigureAwait(false);
            
            var ms = new MemoryStream();

            await pdf.Content.CopyToAsync(ms);
            
            return ms;
        }

        private void EnrichTenantInfo<TModel>(TModel model, bool isPortals, ITenantInfo tenantInfo) 
            where TModel : TemplateViewModel
        {
            if (_tenantInfoAccessor != null && tenantInfo == null)
                tenantInfo = _tenantInfoAccessor.TenantInfo;
            
            if (tenantInfo == null)
                return;

            model.TenantName = tenantInfo.Name;
            model.TenantLogoSvgUrl = tenantInfo.LogoSvgUrl;
            model.TenantLogoPngUrl = tenantInfo.LogoPngUrl;
            model.TenantPrimaryColor = tenantInfo.PrimaryColorHex;
            model.TenantSecondaryColor = tenantInfo.SecondaryColorHex;
            model.TenantTextOnPrimaryColor = tenantInfo.TextOnPrimaryColorHex;
            model.TenantTextOnSecondaryColor = tenantInfo.TextOnSecondaryColorHex;
            model.TenantSupportEmail = tenantInfo.SupportEmail;
            model.TenantProductName = isPortals ? tenantInfo.PortalsProductName : tenantInfo.EcbProductName;
            model.TenantDefaultCulture = tenantInfo.DefaultCulture;
            model.TenantDefaultCurrency = tenantInfo.DefaultCurrency;

            model.EmailSettings = tenantInfo.EmailSettings;
        }

        private IView FindView(ActionContext actionContext, string viewName)
        {
            var getViewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);

            var errorMessage = string.Join(
                Environment.NewLine,
                new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations)); ;

            throw new InvalidOperationException(errorMessage);
        }

        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.RequestServices = _serviceProvider;

            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }

    }
}
