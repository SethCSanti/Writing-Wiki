using Microsoft.EntityFrameworkCore;
using WorldbuildingWiki.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WikiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Wiki")));
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapGet("/", () => Results.Ok(new { service = "Worldbuilding Wiki API", status = "running" }));

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
