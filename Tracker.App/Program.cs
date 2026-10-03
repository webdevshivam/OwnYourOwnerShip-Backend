using System.Text;
using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using Tracker.App.Common.Auth;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Hangfire;
using Tracker.App.Common.IoC;
using Tracker.App.Common.Models;
using Tracker.App.Common.Webhooks;
using Tracker.App.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Configure Autofac as the DI Container
// ==========================================
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// ==========================================
// 2. Framework & ASP.NET Core Services
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Omit null properties/attributes from JSON response payloads
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Enforce consistent ApiResponse envelope for automatic model validation errors (400 Bad Request)
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(err => new ValidationErrorDetail(
                    Field: kvp.Key,
                    Message: string.IsNullOrWhiteSpace(err.ErrorMessage) ? ApiConstants.InvalidValueMessage : err.ErrorMessage
                )))
                .ToList();

            var response = ApiResponse.Fail(ApiConstants.ValidationFailedMessage, StatusCodes.Status400BadRequest, errors);
            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Configure CORS for Frontend Client
const string FrontendCorsPolicy = "AllowFrontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "https://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// Configure JWT Bearer Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check Authorization header first, fallback to HttpOnly accessToken cookie
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(ApiConstants.AccessTokenCookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tracker API",
        Version = "v1",
        Description = "Production-grade backend API for Tracker.App"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token: Bearer {token}"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// Configure PostgreSQL Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Configure Redis ConnectionMultiplexer as a Singleton
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

// Configure Hangfire with PostgreSQL Storage & Background Workers
builder.Services.AddHangfireConfiguration(builder.Configuration);

// Configure Webhook Services & Resilient HttpClient
builder.Services.AddWebhookServices();

// ==========================================
// 3. Autofac Container Registrations
// ==========================================
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register custom modular dependencies via Autofac modules
    containerBuilder.RegisterModule(new ApplicationModule());
});

var app = builder.Build();

// ==========================================
// 4. HTTP Request Pipeline
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Ensure CORS middleware is placed after Routing and before Auth:
app.UseRouting();
app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (available at /hangfire)
app.UseHangfireDashboardCustom();

app.MapControllers();

// Seed default test user in development
await app.SeedDevelopmentDataAsync();

app.Run();
