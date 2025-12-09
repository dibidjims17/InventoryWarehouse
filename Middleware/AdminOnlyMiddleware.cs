using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Web;

namespace MyRazorApp.Middleware
{
    public class AdminOnlyMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminOnlyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Only enforce on /Admin pages
            if (context.Request.Path.StartsWithSegments("/Admin"))
            {
                var role = context.Session.GetString("role");

                if (string.IsNullOrEmpty(role) || role != "admin")
                {
                    // Redirect to login with return URL
                    var returnUrl = HttpUtility.UrlEncode(context.Request.Path + context.Request.QueryString);
                    context.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
