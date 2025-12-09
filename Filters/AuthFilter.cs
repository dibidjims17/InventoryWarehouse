using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyRazorApp.Filters
{
    public class AuthFilter : IActionFilter
    {
        private readonly bool _requireAdmin;

        public AuthFilter(bool requireAdmin = false)
        {
            _requireAdmin = requireAdmin;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            var userId = httpContext.Session.GetString("user_id");
            var role = httpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(userId))
            {
                // Not logged in
                context.Result = new RedirectToPageResult("/Auth/Login");
                return;
            }

            if (_requireAdmin && role != "admin")
            {
                // Not an admin
                context.Result = new ContentResult
                {
                    Content = "Access denied. Admins only.",
                    StatusCode = 403
                };
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing after action
        }
    }
}
