using Microsoft.AspNetCore.Authentication.Cookies;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();

            // HttpClient for API
            builder.Services.AddHttpClient("TutorConnectAPI", client =>
            {
                client.BaseAddress = new Uri("https://localhost:44374/");
            });

            builder.Services.AddScoped<ApiService>();
            builder.Services.AddHttpContextAccessor();
            // Add this if using View Component approach
            builder.Services.AddScoped<ThemeService>();

            // Add authentication using cookies
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";

                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.SlidingExpiration = true;

                    // Do NOT set ExpireTimeSpan globally
                    // Session cookies will die on browser close by default
                });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();  
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }



            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Authentication & authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
