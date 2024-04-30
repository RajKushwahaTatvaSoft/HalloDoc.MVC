using Microsoft.EntityFrameworkCore;
using Data_Layer.DataContext;
using Business_Layer.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Business_Layer.Repository.IRepository;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Services.Helper;
using Business_Layer.Services.Patient.Interface;
using Business_Layer.Services.Patient;
using Rotativa.AspNetCore;
using Business_Layer.Services.Guest.Interface;
using Business_Layer.Services.Guest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Business_Layer.Services.AdminServices;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Services.PhysicianServices;
using Business_Layer.Services.PhysicianServices.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationDbContext")));

builder.Services.AddScoped<IPatientDashboardRepository, PatientDashboardRepository>();
builder.Services.AddScoped<IPhysicianDashboardService, PhysicianDashboardService>();
builder.Services.AddScoped<IPatientAuthRepository, PatientAuthRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPhysicianService, PhysicianService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUtilityService, UtilityService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".HalloDoc.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Adjust as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var jwtIssuer = builder.Configuration.GetSection("Jwt:Issuer").Get<string>();
var jwtKey = builder.Configuration.GetSection("Jwt:Key").Get<string>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 .AddJwtBearer(options =>
 {
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = jwtIssuer,
         ValidAudience = jwtIssuer,
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
     };
 });

builder.Services.AddNotyf(config => { config.DurationInSeconds = 3; config.IsDismissable = true; config.Position = NotyfPosition.TopRight; });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseNotyf();
app.UseAuthorization();
app.UseRotativa();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Guest}/{action=Index}");

// Add method for custom roles

app.Run();