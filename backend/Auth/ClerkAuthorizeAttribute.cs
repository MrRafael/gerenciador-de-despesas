using Clerk.BackendAPI.Helpers.Jwks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MyFinBackend.Auth
{
    public class ClerkAuthorizeAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var options = new AuthenticateRequestOptions(
                    secretKey: "sk_test_qtYSjDJ75UC0DVCyqMwcvz8QznMwBWZMDdI0hn8lTM",
                    authorizedParties: new string[] { "http://localhost:5173" }
                );

                var authResult = await AuthenticateRequest.AuthenticateRequestAsync(context.HttpContext.Request, options);

                if (!authResult.IsAuthenticated)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Adicionar informações do usuário ao context
                var userId = authResult.Claims.FindFirst("sub")?.Value ??
                        authResult.Claims.FindFirst("user_id")?.Value ??
                        authResult.Claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = authResult.Claims.FindFirst("sid")?.Value ??
                           authResult.Claims.FindFirst("session_id")?.Value;
                var claims = new List<Claim>
                {
                    new Claim("sub", userId),
                    new Claim("sid", sessionId),
                };

                var identity = new ClaimsIdentity(claims, "Clerk");
                context.HttpContext.User = new ClaimsPrincipal(identity);
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
