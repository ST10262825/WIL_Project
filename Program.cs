using Microsoft.AspNetCore.Authentication.Cookies;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register HttpClient for API
            builder.Services.AddHttpClient("TutorConnectAPI", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44374/");
            });

            builder.Services.AddScoped<ApiService>();

            // Add session and HTTP context accessor
            builder.Services.AddSession();
            builder.Services.AddHttpContextAccessor();

            // Add authentication using cookies
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Add authentication & authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Enable session middleware
            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
