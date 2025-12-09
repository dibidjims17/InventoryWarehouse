USERS

if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
{
    return RedirectToPage("/Auth/Login");
}

ADMINS

if (HttpContext.Session.GetString("role") != "admin")
{
    return RedirectToPage("/Auth/Login");
}
