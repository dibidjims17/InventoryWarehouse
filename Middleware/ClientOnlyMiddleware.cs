public class ClientOnlyMiddleware
{
    private readonly RequestDelegate _next;

    public ClientOnlyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Only protect /Client/* pages
        if (context.Request.Path.StartsWithSegments("/Client"))
        {
            var role = context.Session.GetString("role");
            if (role != "client")
            {
                // Redirect to login with returnUrl
                var returnUrl = context.Request.Path + context.Request.QueryString;
                context.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
                return;
            }
        }

        await _next(context);
    }
}
