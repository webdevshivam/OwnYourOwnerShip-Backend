using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tracker.App.Common.IoC;
using Tracker.App.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Configure Autofac as the DI Container
// ==========================================
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// ==========================================
// 2. Framework & ASP.NET Core Services
// ==========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthorization();

app.MapControllers();

app.Run();