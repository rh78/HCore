namespace HCore.Web.Middleware
{
    public class AuthorizationMiddlewareOptions
    {
        public string RoutePrefix { get; set; } = null;

        public string PolicyName { get; set; } = "default";

        public string AuthenticationScheme { get; set; } = null;
    }
}
