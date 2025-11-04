using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebFindLove.Helper.Email;
using WebFindLove.Helper.Seeder;
using WebFindLove.HelperServices;
using WebFindLove.Hubs;
using WebFindLove.Models;
using WebFindLove.Models.Repositories;
using WebFindLove.Models.Options;
using WebFindLove.Models.Services;
using WebFindLove.Models.Services.RoleService;
using WebFindLove.Models.Services.UserService;
using WebFindLove.Models.UnitOfWork;
using DotNetEnv;

// 🔹 Load environment variables from .env file
Env.Load();

// 🔹 Cấu hình Serilog trước khi build
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WebFindLove")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        "Logs/app-log-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting WebFindLove application");

    var builder = WebApplication.CreateBuilder(args);

    // Override configuration from environment variables (loaded from .env file)
    // ASP.NET Core automatically prioritizes environment variables, but we'll ensure they're set
    
    // Priority 1: Azure SQL Connection String (for Azure deployment)
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING")))
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING");
        Log.Information("Using Azure SQL connection string from environment variable");
    }
    // Priority 2: SQL Authentication with individual components (for local/other deployments)
    else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_SERVER")))
    {
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var connectionString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};MultipleActiveResultSets=True;TrustServerCertificate=True;";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        Log.Information("Using SQL authentication connection string from environment variables");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDIS_CONNECTION")))
    {
        builder.Configuration["ConnectionStrings:Redis"] = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
    {
        builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
    {
        builder.Configuration["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMTP_EMAIL")))
    {
        builder.Configuration["EmailSettings:SenderEmail"] = Environment.GetEnvironmentVariable("SMTP_EMAIL");
        builder.Configuration["EmailSettings:SmtpServer"] = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? builder.Configuration["EmailSettings:SmtpServer"];
        builder.Configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")))
    {
        builder.Configuration["GoogleAuth:ClientId"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        builder.Configuration["GoogleAuth:ClientSecret"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
    }

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ADMIN_USERNAME")))
    {
        builder.Configuration["DefaultAdmin:UserName"] = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        builder.Configuration["DefaultAdmin:Email"] = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        builder.Configuration["DefaultAdmin:Password"] = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    }

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddHttpContextAccessor();

    // Thêm EF vào DI container
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    Log.Information("Database configured with connection string");


    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.Secure = CookieSecurePolicy.SameAsRequest;
        options.CheckConsentNeeded = context => false;
        options.MinimumSameSitePolicy = SameSiteMode.None;
    });

    // Configure Authentication
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Home/Index";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            // Use SameSite None for OAuth to work properly
            options.Cookie.SameSite = SameSiteMode.None;
            // Must be Secure when SameSite is None
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        })
        .AddGoogle(GoogleDefaults.AuthenticationScheme, googleOptions =>
        {
            var googleConfig = builder.Configuration.GetSection("GoogleAuth");
            var clientId = googleConfig["ClientId"];
            var clientSecret = googleConfig["ClientSecret"];
            
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && 
                clientId != "YOUR_CLIENT_ID" && clientSecret != "YOUR_CLIENT_SECRET")
            {
                googleOptions.ClientId = clientId;
                googleOptions.ClientSecret = clientSecret;
                googleOptions.CallbackPath = "/Auth/GoogleCallback";
                
                // Save tokens for future requests
                googleOptions.SaveTokens = true;
                
                var clientIdPreview = clientId?.Length > 20 ? clientId?.Substring(0, 20) + "..." : clientId;
                Log.Information("Google Authentication configured with ClientId: {ClientId}", clientIdPreview);
            }
            else
            {
                Log.Warning("Google Authentication not configured - ClientId or ClientSecret is missing");
            }
        });
    Log.Information("Authentication configured");

    builder.Services.AddAuthorization();

    // Register SignalR (built-in .NET 8.0)
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
    Log.Information("SignalR configured");

    // Add Session support for storing user data
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Email
    builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSettings"));

    // OpenAI
    builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

    // Google Authentication
    builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("GoogleAuth"));

    // Register UnitOfWork and services
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

    // Đăng ký các dịch vụ ứng dụng và kho lưu trữ
    // Pattern: Controller → Service → Repository → UnitOfWork → DbContext
    builder.Services.AddApplicationServices();      // Đăng ký tất cả Services (User, Role, ...)
    builder.Services.AddInfrastructureRepositories(); // Đăng ký tất cả Repositories (User, Role, ...)

    builder.Services.AddScoped<IDataSeedService, DataSeedService>();

    Log.Information("Services registered successfully");

    var app = builder.Build();

    Log.Information("Application built successfully");

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
        Log.Information("Production environment configured");
    }
    else
    {
        Log.Information("Development environment configured");
    }
    // Add permissuion seeding
    //using (var scope = app.Services.CreateScope())
    //{
    //    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //    PermissionSeeder.SyncPermissions(db);
    //}

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    // Cookie policy must be before authentication
    app.UseCookiePolicy();
    
    // Session middleware (must be before authentication)
    app.UseSession();

    // Authentication & Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Seed default admin user
    using (var scope = app.Services.CreateScope())
    {
        var dataSeedService = scope.ServiceProvider.GetRequiredService<IDataSeedService>();
        await dataSeedService.SeedDefaultAdminUserAsync();
    }

    // Map API routes
    app.MapControllers();

    // Map SignalR Hub
    app.MapHub<ChatHub>("/chatHub");
    Log.Information("SignalR ChatHub mapped to /chatHub");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    Log.Information("Application starting...");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Listening on: {Urls}", string.Join(", ", app.Urls));

    app.Run();

    Log.Information("Application stopped gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
