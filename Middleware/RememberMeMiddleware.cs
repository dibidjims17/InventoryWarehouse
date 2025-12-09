using Microsoft.AspNetCore.Http;
using MyRazorApp.Services;
using System.Threading.Tasks;

public class RememberMeMiddleware
{
    private readonly RequestDelegate _next;

    public RememberMeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MongoService mongoService)
    {
        // If session is missing but rememberme cookie exists
        if (string.IsNullOrEmpty(context.Session.GetString("user_id")) &&
            context.Request.Cookies.TryGetValue("rememberme", out var token))
        {
            // Validate token in DB
            var user = await mongoService.GetUserBySessionTokenAsync(token);
            if (user != null)
            {
                // Restore session
                context.Session.SetString("session_token", token);
                context.Session.SetString("user_id", user.UserId ?? string.Empty);
                context.Session.SetString("username", user.Username ?? "Unknown");
                context.Session.SetString("role", user.Role ?? "client");
            }
        }

        // Call the next middleware
        await _next(context);
    }
}
