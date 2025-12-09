using Microsoft.AspNetCore.Http;
using MyRazorApp.Services;
using System.Threading.Tasks;

namespace MyRazorApp.Middleware
{
    public class CheckUserStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public CheckUserStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, MongoService mongo)
        {
            // Only check if user is logged in
            var userId = context.Session.GetString("user_id");
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await mongo.GetUserByIdAsync(userId);

                if (user == null || user.Status?.ToLower() == "deactivated")
                {
                    // Clear session and cookies
                    context.Session.Clear();
                    if (context.Request.Cookies.ContainsKey("rememberme"))
                        context.Response.Cookies.Delete("rememberme");

                    // Optionally redirect to login page with message
                    context.Response.Redirect("/Auth/Login?error=deactivated");
                    return; // stop further processing
                }
            }

            await _next(context); // proceed to next middleware
        }
    }
}
