using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WebApi.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredRole;

        public AuthorizeRoleAttribute(string role)
        {
            _requiredRole = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(roleClaim) || !roleClaim.Equals(_requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ForbidResult(); // 403 Forbidden
            }
        }
    }
}
