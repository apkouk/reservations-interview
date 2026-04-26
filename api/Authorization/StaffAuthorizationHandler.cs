using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Authorization
{
    public class StaffRequirement : IAuthorizationRequirement { }

    public class StaffAuthorizationHandler : AuthorizationHandler<StaffRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            StaffRequirement requirement)
        {
            var httpContext = (context.Resource as AuthorizationFilterContext)?.HttpContext;
            if (httpContext != null)
            {
                httpContext.Request.Cookies.TryGetValue("access", out string? accessValue);
                if (accessValue == "1")
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
