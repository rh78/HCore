using HCore.Templating.Templates.ViewModels.Shared;
using HCore.Tenants;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// see https://scottsauber.com/2018/07/07/walkthrough-creating-an-html-email-template-with-razor-and-razor-class-libraries-and-rendering-it-from-a-net-standard-class-library/

namespace HCore.Templating.Generic.Impl
{
    internal class TemplateRendererImpl : ITemplateRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        private readonly ITenantInfoAccessor _tenantInfoAccessor;

        public TemplateRendererImpl(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;

            _tenantInfoAccessor = _serviceProvider.GetService<ITenantInfoAccessor>();
        }

        public async Task<string> RenderViewAsync<TModel>(string viewName, TModel model)
            where TModel : TemplateViewModel
        {
            EnrichTenantInfo(model);

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

                await view.RenderAsync(viewContext).ConfigureAwait(false);

                return output.ToString();
            }
        }

        private void EnrichTenantInfo<TModel>(TModel model) 
            where TModel : TemplateViewModel
        {
            if (_tenantInfoAccessor == null)
                return;

            var tenantInfo = _tenantInfoAccessor.TenantInfo;

            if (tenantInfo == null)
                return;

            model.TenantName = tenantInfo.Name;
            model.TenantLogoUrl = tenantInfo.LogoUrl;
            model.TenantPrimaryColor = ConvertToHexColor(tenantInfo.PrimaryColor);
            model.TenantSecondaryColor = ConvertToHexColor(tenantInfo.SecondaryColor);
            model.TenantTextOnPrimaryColor = ConvertToHexColor(tenantInfo.TextOnPrimaryColor);
            model.TenantTextOnSecondaryColor = ConvertToHexColor(tenantInfo.TextOnSecondaryColor);
            model.TenantSupportEmail = tenantInfo.SupportEmail;
            model.TenantProductName = tenantInfo.ProductName;
        }

        private string ConvertToHexColor(int? color)
        {
            return "#" + (color != null ? ((int)color).ToString("X6") : "000000");
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
