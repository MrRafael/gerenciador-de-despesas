using Clerk.BackendAPI.Helpers.Jwks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MyFinBackend.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ClerkAuthorizeAttribute : Attribute, IFilterFactory
    {
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            return new ClerkAuthorizeFilter(config);
        }
    }

    public class ClerkAuthorizeFilter(IConfiguration config) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var secretKey = config["clerkApiKey"]!;
                var frontendUrl = config["frontendUrl"]!;

                var options = new AuthenticateRequestOptions(
                    secretKey: secretKey,
                    authorizedParties: [frontendUrl]
                );

                var authResult = await AuthenticateRequest.AuthenticateRequestAsync(context.HttpContext.Request, options);

                if (!authResult.IsAuthenticated)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var userId = authResult.Claims.FindFirst("sub")?.Value
                    ?? authResult.Claims.FindFirst("user_id")?.Value
                    ?? authResult.Claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var sessionId = authResult.Claims.FindFirst("sid")?.Value
                    ?? authResult.Claims.FindFirst("session_id")?.Value;

                var claims = new List<Claim>
                {
                    new("sub", userId!),
                    new("sid", sessionId!),
                };

                context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Clerk"));
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}
