using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DomoNow.Parking.Api.Endpoints;
using DomoNow.Parking.Api.ErrorHandling;
using DomoNow.Parking.Api.Security;
using DomoNow.Parking.Application;
using DomoNow.Parking.Infrastructure;
using DomoNow.Parking.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PUT")));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Límite por IP: un cliente abusivo no puede bloquear el login de los demás operadores.
    options.AddPolicy(AuthEndpoints.LoginRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateAndSeedOnStartup)
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapParkingSpotEndpoints();
app.MapParkingAssignmentEndpoints();

await app.RunAsync();

public partial class Program;
