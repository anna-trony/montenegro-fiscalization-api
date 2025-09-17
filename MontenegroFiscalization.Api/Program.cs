using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MontenegroFiscalization.Api.Services;
using MontenegroFiscalization.Core.Interfaces;
using MontenegroFiscalization.Core.Services;
using MontenegroFiscalization.Infrastructure.Data;
using MontenegroFiscalization.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuring Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Montenegro Fiscalization API",
        Version = "v1",
        Description = "REST API for Montenegro invoice fiscalization system"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuring Database - Using SQLite for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<FiscalizationDbContext>(options =>
        options.UseSqlite("Data Source=fiscalization.db"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost;Database=MontenegroFiscalization;Trusted_Connection=true;TrustServerCertificate=true";
    
    builder.Services.AddDbContext<FiscalizationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Configuring JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "your-256-bit-secret-key-for-jwt-token-generation-replace-this";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "montenegro-fiscalization-api";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "montenegro-fiscalization-clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();

// Adding Distributed Cache (Redis if configured, else in-memory)
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "MontenegroFiscalization";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// Registering Application Services
builder.Services.AddScoped<IFiscalizationService, FiscalizationService>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICertificateService, VaultCertificateService>();

// Configuring CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://yourdomain.com",
            "https://app.yourdomain.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
    
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<FiscalizationDbContext>("database");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddEventLog();
}

var app = builder.Build();

// Applying migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<FiscalizationDbContext>();
        
        context.Database.EnsureCreated();
        
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
        
        if (!context.ApiKeys.Any())
        {
            logger.LogInformation("Seeding demo data...");
            
            var tenant = new MontenegroFiscalization.Core.Models.TenantConfiguration
            {
                TenantId = "demo-tenant",
                TenantName = "Demo Company",
                TIN = "02004003",
                BusinessUnitCode = "bb123bb123",
                TCRCode = "cc123cc123",
                SoftwareCode = "ss123ss123",
                MaintainerCode = "mm123mm123",
                IsInVAT = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.TenantConfigurations.Add(tenant);
            
            var apiKey = new MontenegroFiscalization.Core.Models.ApiKey
            {
                TenantId = "demo-tenant",
                Key = "demo-api-key-12345",
                Name = "Demo API Key",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };
            context.ApiKeys.Add(apiKey);
            
            context.SaveChanges();
            logger.LogInformation("Demo data seeded successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Montenegro Fiscalization API v1");
        c.RoutePrefix = string.Empty;
    });
    app.UseCors("Development");
}
else
{
    app.UseHsts();
    app.UseCors("Production");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Logging startup
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Montenegro Fiscalization API started");
startupLogger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("Database: SQLite (fiscalization.db)");
startupLogger.LogInformation("Swagger UI: http://localhost:5189");

app.Run();