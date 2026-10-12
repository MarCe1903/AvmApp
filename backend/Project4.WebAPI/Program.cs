using Project4.Business;
using Project4.Business.Abstract;
using Project4.Business.Concrete;
using Project4.DataAccess;
using Project4.DataAccess.Abstract;
using Project4.WebAPI.Services;
using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured via user-secrets, environment variables, or deployment settings.");
}

// DbContext: SQL Server (DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(defaultConnection);
});

// DataProtection: OAuth state cookie'leri restart'ta bozulmasın
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")))
    .SetApplicationName("AvmApp");

// JWT settings must come from configuration, user-secrets, or environment variables.
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    jwtSettings.Issuer = "Project4";
}
if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    jwtSettings.Audience = "Project4Client";
}
if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters long.");
}

builder.Services.Configure<JwtSettings>(options =>
{
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.Secret = jwtSettings.Secret;
    options.AccessTokenMinutes = jwtSettings.AccessTokenMinutes;
});
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// CORS (Angular dev origin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// DI kayıtları
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IProductDal, EfProductDal>();

builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<IOrderDal, EfOrderDal>();

builder.Services.AddScoped<IOrderStatusService, OrderStatusManager>();
builder.Services.AddScoped<IOrderStatusDal, EfOrderStatusDal>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();


// JWT Authentication
var issuer = jwtSettings.Issuer;
var audience = jwtSettings.Audience;
var secret = jwtSettings.Secret;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.SaveToken = true;
        // Use legacy validator to avoid JsonWebTokenHandler header decode issues.
        o.UseSecurityTokenValidators = true;
        o.SecurityTokenValidators.Clear();
        o.SecurityTokenValidators.Add(new JwtSecurityTokenHandler());
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var authHeader = ctx.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    var parts = authHeader
                        .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim('"'));

                    var token = parts.FirstOrDefault(p => p.Count(c => c == '.') >= 2)
                        ?? parts.LastOrDefault();

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        ctx.Token = token;
                    }
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"JWT auth failed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? string.Empty)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "avmapp.external";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle(options =>
    {
        options.SignInScheme = "External";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-google";
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SaveTokens = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always
});

// DB şemasını (Users tablosu) otomatik oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Users tablosu yoksa oluştur (bazı DB'lerde EnsureCreated kaçırabiliyor)
    var creator = db.GetService<IRelationalDatabaseCreator>();
    if (!creator.Exists())
    {
        creator.Create();
    }

    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Email] NVARCHAR(256) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [FullName] NVARCHAR(256) NOT NULL,
        [Role] NVARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
END
");

    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Favorites','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Favorites](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
END
");

    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.ChatSessions','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatSessions](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [LastMessageAt] DATETIME2 NULL
    );
END
");

    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.ChatMessages','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionId] INT NOT NULL,
        [SenderId] INT NOT NULL,
        [SenderRole] NVARCHAR(50) NOT NULL,
        [Text] NVARCHAR(MAX) NOT NULL,
        [SentAt] DATETIME2 NOT NULL
    );
END
");

    db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.UserOrders','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserOrders](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [OrderStatusId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
END
");
    db.Database.ExecuteSqlRaw(@"
    IF COL_LENGTH('dbo.Users', 'MfaEnabled') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Users] ADD [MfaEnabled] BIT NOT NULL CONSTRAINT DF_Users_MfaEnabled DEFAULT 0;
    END
    ");

    db.Database.ExecuteSqlRaw(@"
    IF OBJECT_ID('dbo.MfaChallenges','U') IS NULL
BEGIN
        CREATE TABLE [dbo].[MfaChallenges](
            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [ChallengeId] NVARCHAR(64) NOT NULL,
            [UserId] INT NOT NULL,
            [CodeHash] NVARCHAR(256) NOT NULL,
            [ExpiresAt] DATETIME2 NOT NULL,
            [IsUsed] BIT NOT NULL,
            [AttemptCount] INT NOT NULL,
            [CreatedAt] DATETIME2 NOT NULL
        );
END
");



    // Ensure required order statuses exist in Northwind DB.
    var statusSeed = new[]
    {
        new { Code = "preparing", Description = "Hazirlaniyor" },
        new { Code = "on_road", Description = "Yolda" },
        new { Code = "delivered", Description = "Teslim edildi" }
    };

    foreach (var status in statusSeed)
    {
        var existing = db.OrderStatuses.FirstOrDefault(s => s.Code.ToLower() == status.Code);
        if (existing is null)
        {
            db.OrderStatuses.Add(new Project4.Entities.OrderStatus
            {
                Code = status.Code,
                Description = status.Description
            });
        }
        else if (existing.Description != status.Description)
        {
            existing.Description = status.Description;
        }
    }
    db.SaveChanges();

    var seedAdminEmail = builder.Configuration["SeedAdmin:Email"];
    var seedAdminPassword = builder.Configuration["SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(seedAdminEmail) && !string.IsNullOrWhiteSpace(seedAdminPassword))
    {
        var adminUser = db.Users.FirstOrDefault(u => u.Email == seedAdminEmail);
        var hasher = new PasswordHasher<User>();

        if (adminUser is null)
        {
            adminUser = new User
            {
                Email = seedAdminEmail,
                FullName = builder.Configuration["SeedAdmin:FullName"] ?? "Admin User",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(adminUser);
        }

        adminUser.PasswordHash = hasher.HashPassword(adminUser, seedAdminPassword);
        adminUser.Role = "Admin";
        db.SaveChanges();
    }
}

app.UseCors("spa");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
