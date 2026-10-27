using System;
using System.Threading.Tasks;
using HCore.Web.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HCore.Test
{
    /// <summary>
    /// The scoped IUrlHelper of Startup.ConfigureUrlHelper, built from IHttpContextAccessor the way IdentityServicesImpl uses it for mail links
    /// </summary>
    public class UrlHelperTests
    {
        private const string LoginPage = "/Account/Login";
        private const string LoginRoute = "Account/Login";
        private const string ConfirmEmailPage = "/Account/ConfirmEmail";
        private const string ConfirmEmailRoute = "Account/ConfirmEmail";
        private const string Scheme = "https";
        private const string Host = "portal.example.com";
        private const string UserUuid = "1001";
        private const string Code = "confirmation-code";

        [Fact]
        public void ConfigureUrlHelper_Page_ShouldReturn_TheConfirmEmailPageWithSchemeAndHost()
        {
            var loginEndpoint = CreatePageEndpoint(LoginRoute, LoginPage);
            var confirmEmailEndpoint = CreatePageEndpoint(ConfirmEmailRoute, ConfirmEmailPage);

            using var serviceProvider = CreateServiceProvider(loginEndpoint, confirmEmailEndpoint);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            httpContext.Request.Scheme = Scheme;
            httpContext.Request.Host = new HostString(Host);
            httpContext.Request.RouteValues["page"] = LoginPage;

            httpContext.SetEndpoint(loginEndpoint);

            serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

            using var serviceScope = serviceProvider.CreateScope();

            var urlHelper = serviceScope.ServiceProvider.GetRequiredService<IUrlHelper>();

            var values = new
            {
                userUuid = UserUuid,
                code = Code
            };

            var callbackUrl = urlHelper.Page(ConfirmEmailPage, pageHandler: null, values: values, protocol: Scheme);

            Assert.Equal($"{Scheme}://{Host}/{ConfirmEmailRoute}?userUuid={UserUuid}&code={Code}", callbackUrl);
        }

        private static RouteEndpoint CreatePageEndpoint(string route, string page)
        {
            var pageValues = new
            {
                page
            };

            var routePattern = RoutePatternFactory.Parse(route, defaults: pageValues, parameterPolicies: null, requiredValues: pageValues);

            return new RouteEndpoint(_ => Task.CompletedTask, routePattern, 0, EndpointMetadataCollection.Empty, page);
        }

        private static ServiceProvider CreateServiceProvider(params RouteEndpoint[] endpoints)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddRouting();
            services.AddMvcCore();
            services.AddHttpContextAccessor();

            var endpointDataSource = new DefaultEndpointDataSource(endpoints);

            services.AddSingleton<EndpointDataSource>(endpointDataSource);

            var configuration = new ConfigurationBuilder().Build();

            var testStartup = new TestStartup(configuration);

            testStartup.AddUrlHelper(services);

            return services.BuildServiceProvider();
        }

        [Fact]
        public void ConfigureUrlHelper_NoHttpContext_ShouldThrow_ArgumentNullException()
        {
            using var serviceProvider = CreateServiceProvider();

            serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = null;

            using var serviceScope = serviceProvider.CreateScope();

            Assert.ThrowsAny<ArgumentNullException>(() => serviceScope.ServiceProvider.GetRequiredService<IUrlHelper>());
        }

        private class TestStartup : Startup
        {
            public TestStartup(IConfiguration configuration)
                : base(configuration, hostingEnvironment: null)
            {
            }

            public void AddUrlHelper(IServiceCollection services)
            {
                ConfigureUrlHelper(services);
            }
        }
    }
}
