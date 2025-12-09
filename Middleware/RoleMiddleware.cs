using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Web;

namespace MyRazorApp.Middleware
{
    public class RoleMiddleware
    {
        private readonly RequestDelegate _next;

        // Dictionary of path prefixes and allowed roles
        private readonly Dictionary<string, string[]> _pathRoles = new()
        {
            { "/Admin", new[] { "admin" } },
            { "/Client", new[] { "client", "admin" } } // profile page is here
        };

        public RoleMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.ToString();
            var role = context.Session.GetString("role");

            foreach (var entry in _pathRoles)
            {
                if (path.StartsWith(entry.Key))
                {
                    if (string.IsNullOrEmpty(role) || !entry.Value.Contains(role))
                    {
                        var returnUrl = HttpUtility.UrlEncode(context.Request.Path + context.Request.QueryString);
                        context.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
                        return;
                    }
                    break; // Role allowed, no need to check others
                }
            }

            await _next(context);
        }
    }
}
