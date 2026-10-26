using College_Library_Management_System.Data;
using College_Library_Management_System.Models;
using College_Library_Management_System.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

// Common & Core Services
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IEmailService, College_Library_Management_System.Services.EmailService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IAuditLogService, College_Library_Management_System.Services.AuditLogService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IDepartmentService, College_Library_Management_System.Services.DepartmentService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IStudentService, College_Library_Management_System.Services.StudentService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IStudentDashboardService, College_Library_Management_System.Services.StudentDashboardService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.ILibrarianService, College_Library_Management_System.Services.LibrarianService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IMasterDataService, College_Library_Management_System.Services.MasterDataService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IBookService, College_Library_Management_System.Services.BookService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IBookCopyService, College_Library_Management_System.Services.BookCopyService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.ILoanService, College_Library_Management_System.Services.LoanService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IFineService, College_Library_Management_System.Services.FineService>();
builder.Services.AddScoped<College_Library_Management_System.Services.Interfaces.IReportService, College_Library_Management_System.Services.ReportService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Database and apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
