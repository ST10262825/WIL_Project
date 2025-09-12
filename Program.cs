using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TutorConnectAPI.Data;
using TutorConnectAPI.Services;
using TutorConnectAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ---------------------
// Add services
// ---------------------

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddControllers();

// ---------------------
// CORS - allow WebApp project to access API & SignalR
// ---------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:44347") // WebApp URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// ---------------------
// SignalR for real-time messaging
// ---------------------
builder.Services.AddSignalR();

// ---------------------
// Swagger with JWT support
// ---------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TutorConnectAPI",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ---------------------
// JWT Authentication
// ---------------------
var key = Encoding.ASCII.GetBytes("SuperSecretJwtKeyReplaceThisWithMoreChars!");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // Enable SignalR to receive token via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ---------------------
// Authorization
// ---------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ---------------------
// Build app
// ---------------------
var app = builder.Build();

// ---------------------
// Seed database
// ---------------------
using (var scope = app.Services.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var dbContext = scopedServices.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(dbContext);
}

// ---------------------
// Middleware
// ---------------------
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();



app.UseHttpsRedirection();

app.UseCors(); // <-- Enable CORS here
app.UseAuthentication();
app.UseAuthorization();

// ---------------------
// Map Controllers & SignalR hubs
// ---------------------
app.MapControllers();
app.MapHub<ChatHub>("/chatHub"); // SignalR hub endpoint

app.Run();
