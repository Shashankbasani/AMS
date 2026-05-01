using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Azure.Storage.Blobs;
using Serilog;
using AMS.Infrastructure.Data;
using AMS.Infrastructure.Repositories;
using AMS.Infrastructure.Services;
using AMS.Core.Interfaces;

/*
╔══════════════════════════════════════════════════════════════════════════════╗
║                              PROGRAM.CS EXPLAINED                             ║
║══════════════════════════════════════════════════════════════════════════════║
║                                                                               ║
║  This is the ENTRY POINT of your ASP.NET Core application.                   ║
║  Everything starts here!                                                      ║
║                                                                               ║
║  The file is divided into TWO main sections:                                  ║
║                                                                               ║
║  1. SERVICE CONFIGURATION (builder.Services.Add...)                           ║
║     - Register services for Dependency Injection                              ║
║     - Configure database, authentication, logging                             ║
║     - Everything your app NEEDS to run                                        ║
║                                                                               ║
║  2. MIDDLEWARE PIPELINE (app.Use...)                                          ║
║     - Define HOW requests flow through your app                               ║
║     - Order matters! Requests flow top to bottom                              ║
║     - Exception handling → Auth → Routing → Controller                        ║
║                                                                               ║
╚══════════════════════════════════════════════════════════════════════════════╝
*/

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 1: CREATE THE APPLICATION BUILDER
// This is like preparing the foundation before building a house
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 2: CONFIGURE SERILOG FOR LOGGING
// Serilog provides better logging than the default .NET logger
// ═══════════════════════════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Read settings from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console()                              // Log to console
    .WriteTo.File(                                  // Log to files
        path: "logs/ams-.log",
        rollingInterval: RollingInterval.Day,       // New file each day
        retainedFileCountLimit: 30                  // Keep 30 days of logs
    )
    .CreateLogger();

builder.Host.UseSerilog();  // Replace default logger with Serilog

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 3: CONFIGURE SERVICES (DEPENDENCY INJECTION CONTAINER)
// 
// What is Dependency Injection (DI)?
// - Instead of creating objects with "new", you ask the DI container
// - The container creates and manages object lifetimes
// - Makes code testable and loosely coupled
//
// Service Lifetimes:
// - Singleton: One instance for the entire application
// - Scoped: One instance per HTTP request
// - Transient: New instance every time it's requested
// ═══════════════════════════════════════════════════════════════════════════════

// --- DATABASE CONFIGURATION ---
// This registers the DbContext with the DI container
// EF Core will create the database connection when needed
builder.Services.AddDbContext<AMSDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Retry on transient failures (network issues, timeouts)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });
});

// --- REPOSITORY PATTERN REGISTRATION ---
// Register UnitOfWork and Repository for DI
// Scoped = One instance per HTTP request (important for DbContext!)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// --- AZURE STORAGE (Blob + Queue) ---
var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
if (!string.IsNullOrEmpty(storageConnectionString))
{
    // Blob Storage for documents
    builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));
    builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
    
    // Queue Storage for async messaging
    builder.Services.AddSingleton(new Azure.Storage.Queues.QueueServiceClient(storageConnectionString));
    builder.Services.AddScoped<IQueueService, AMS.WebAPI.Services.QueueService>();
    
    Log.Information("Azure Storage configured for Blob and Queue services");
}
else
{
    Log.Warning("Azure Storage connection string not configured. File uploads and queues will not work.");
}

// --- APPLICATION INSIGHTS ---
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
    Log.Information("Application Insights configured");
}

// --- JWT AUTHENTICATION ---
// Configure how the app validates JWT tokens
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "AMS",
        ValidAudience = jwtSettings["Audience"] ?? "AMS",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// --- AUTHORIZATION ---
builder.Services.AddAuthorization();

// --- CONTROLLERS ---
// Register MVC controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
        options.JsonSerializerOptions.WriteIndented = true;        // Pretty print in dev
    });

// --- CORS (Cross-Origin Resource Sharing) ---
// Allow frontend (React) to call our API from different domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- SWAGGER / OPENAPI ---
// Generates API documentation and provides test UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agency Management System API",
        Version = "v1",
        Description = "API for managing insurance clients, policies, and claims"
    });
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 4: BUILD THE APPLICATION
// At this point, all services are registered. Now we build the app.
// ═══════════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 5: DATABASE INITIALIZATION
// Automatically create database and apply migrations on startup
// ═══════════════════════════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AMSDbContext>();
    
    try
    {
        Log.Information("Ensuring database is created...");
        
        // EnsureCreated() creates the database if it doesn't exist
        // It also creates all tables based on your DbContext
        // NOTE: In production, use Migrations instead!
        context.Database.EnsureCreated();
        
        Log.Information("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error initializing database");
        throw;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 6: CONFIGURE THE HTTP REQUEST PIPELINE (MIDDLEWARE)
//
// Middleware is like a series of filters that requests pass through.
// Each middleware can:
// - Process the incoming request
// - Pass it to the next middleware
// - Process the outgoing response
//
// ORDER MATTERS! Common order:
// 1. Exception handling (catches errors from all below)
// 2. HTTPS redirection
// 3. Static files
// 4. Routing
// 5. CORS
// 6. Authentication (who are you?)
// 7. Authorization (what can you do?)
// 8. Custom middleware
// 9. Endpoints (controllers)
// ═══════════════════════════════════════════════════════════════════════════════

// Development-only middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AMS API V1");
        options.RoutePrefix = string.Empty; // Swagger at root URL
    });
}

// Use HTTPS (redirects HTTP to HTTPS)
// app.UseHttpsRedirection();  // Commented out for local development

// Serilog request logging
app.UseSerilogRequestLogging();

// Enable CORS
app.UseCors("AllowFrontend");

// Authentication & Authorization
// These MUST come before UseEndpoints/MapControllers
app.UseAuthentication();  // Validates JWT token, sets User.Identity
app.UseAuthorization();   // Checks if user has required roles/permissions

// Map controller endpoints
// This is where requests finally reach your controller actions
app.MapControllers();

// ═══════════════════════════════════════════════════════════════════════════════
// STEP 7: RUN THE APPLICATION
// This starts the web server and begins listening for HTTP requests
// ═══════════════════════════════════════════════════════════════════════════════

Log.Information("Starting AMS Web API...");
Log.Information("Swagger UI available at: http://localhost:5000/");

app.Run();
