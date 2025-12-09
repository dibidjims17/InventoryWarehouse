// SessionHelper.cs
using Microsoft.AspNetCore.Http;

namespace MyRazorApp.Helpers
{
    public static class SessionHelper
    {
        public static bool IsLoggedIn(HttpContext http) =>
            !string.IsNullOrEmpty(http.Session.GetString("user_id"));

        public static bool IsAdmin(HttpContext http) =>
            http.Session.GetString("role") == "admin";
    }
}
