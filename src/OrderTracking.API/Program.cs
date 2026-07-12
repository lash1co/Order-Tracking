using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using OrderTracking.API.Errors;
using OrderTracking.API.DevTokens;
using OrderTracking.API.Realtime;
using OrderTracking.API.Simulation;
using OrderTracking.Application.Abstractions.Realtime;
using OrderTracking.Application.Drivers.GetDriverPerformance;
using OrderTracking.Application.Drivers.GetNearestDrivers;
using OrderTracking.Application.Drivers.CreateDriver;
using OrderTracking.Application.Drivers.UpdateDriverLocation;
using OrderTracking.Application.Orders.AssignDriver;
using OrderTracking.Application.Orders.CreateOrder;
using OrderTracking.Application.Orders.GetActiveOrders;
using OrderTracking.Application.Orders.GetOrder;
using OrderTracking.Application.Orders.UpdateOrderStatus;
using OrderTracking.Infrastructure;
using OrderTracking.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(45);
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "order-tracking-api",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<AssignDriverHandler>();
builder.Services.AddScoped<UpdateOrderStatusHandler>();
builder.Services.AddScoped<GetActiveOrdersHandler>();
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddScoped<GetNearestDriversHandler>();
builder.Services.AddScoped<GetDriverPerformanceHandler>();
builder.Services.AddScoped<CreateDriverHandler>();
builder.Services.AddScoped<UpdateDriverLocationHandler>();
builder.Services.AddSingleton<ITrackingNotifier, SignalRTrackingNotifier>();
builder.Services.Configure<DriverMovementOptions>(builder.Configuration.GetSection("DriverMovementSimulator"));
builder.Services.AddHostedService<DriverMovementSimulator>();

var signingKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/tracking"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Tracking API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseResponseCompression();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");
app.MapHub<TrackingHub>("/hubs/tracking").RequireAuthorization();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("DemoTokens:Enabled"))
{
    app.MapPost("/api/v1/dev/tokens", (DemoTokenRequest request, IConfiguration configuration) =>
    {
        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "Dispatcher",
            "Driver"
        };
        var requestedRoles = request.Roles ?? [];
        var roles = requestedRoles
            .Where(role => allowedRoles.Contains(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => allowedRoles.Single(allowed => allowed.Equals(role, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (roles.Length == 0)
            return Results.BadRequest(new { error = "At least one valid role is required: Admin, Dispatcher or Driver." });

        var expiresAt = DateTimeOffset.UtcNow.AddHours(Math.Clamp(request.ExpiresInHours ?? 8, 1, 24));
        var token = DemoTokenFactory.Create(configuration, roles, request.Subject ?? "demo-user", expiresAt);
        return Results.Ok(new DemoTokenResponse(token, roles, expiresAt));
    })
    .AllowAnonymous()
    .RequireRateLimiting("api")
    .WithName("CreateDemoToken")
    .WithTags("Development");
}

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (bool.TryParse(app.Configuration["Database:ApplyMigrationsOnStartup"], out var applyMigrations) && applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderTrackingDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

public partial class Program;
