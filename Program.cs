using MyRazorApp.Services; // <-- needed for MongoService
using MyRazorApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Add session
builder.Services.AddSession();
builder.Services.AddSingleton<MongoService>();
builder.Services.AddHttpContextAccessor();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must come before your middlewares
app.UseSession();

// Custom middlewares
app.UseMiddleware<RememberMeMiddleware>();  // restores session from remember-me cookie
app.UseMiddleware<CheckUserStatusMiddleware>();
app.UseMiddleware<RoleMiddleware>();

// No built-in authorization scheme required
// app.UseAuthorization();  <-- not needed for session-based approach

app.MapRazorPages();

app.Run();
