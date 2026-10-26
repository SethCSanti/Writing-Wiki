using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;
using WorldbuildingWiki.Api.Security;
using WorldbuildingWiki.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WikiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Wiki")));
builder.Services.AddDataProtection().SetApplicationName("WorldbuildingWiki");
builder.Services.AddIdentity<WikiUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<WikiDbContext>()
    .AddTokenProvider<DataProtectorTokenProvider<WikiUser>>(TokenOptions.DefaultProvider)
    .AddTokenProvider<AuthenticatorTokenProvider<WikiUser>>(TokenOptions.DefaultAuthenticatorProvider);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-wiki-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Provisional idle lifetime; see decision register.
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.Properties.IssuedUtc is { } issued && DateTimeOffset.UtcNow - issued > TimeSpan.FromHours(8))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
{
    options.Cookie.Name = "__Host-wiki-mfa";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = false;
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(15));
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-wiki-csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<IEmailSender<WikiUser>, SmtpIdentityEmailSender>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(proxy, out var address)) options.KnownProxies.Add(address);
});

var app = builder.Build();

app.UseForwardedHeaders();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
app.MapAuthEndpoints();

app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var isMutation = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
    if (isMutation && !context.Request.Path.StartsWithSegments("/health"))
    {
        try
        {
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/private/{resource}", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

app.Run();

public partial class Program;

sealed class PostgresHealthCheck(WikiDbContext dbContext) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database unavailable.");
        }
        catch
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database unavailable.");
        }
    }
}
