using System.Security.Claims;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace WorldbuildingWiki.Api.Tests;

public sealed class AuthorizationTests : IClassFixture<PrivateApiFactory>
{
    private readonly HttpClient _client;
    private readonly PrivateApiFactory _factory;
    public AuthorizationTests(PrivateApiFactory factory) { _factory = factory; _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }); }

    [Theory]
    [InlineData("entries")]
    [InlineData("search")]
    [InlineData("graph")]
    [InlineData("timeline")]
    [InlineData("maps")]
    [InlineData("images")]
    [InlineData("downloads")]
    [InlineData("archives")]
    public async Task Protected_resources_deny_anonymous_requests(string resource)
    {
        var response = await _client.GetAsync($"/private/{resource}");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("entries")]
    [InlineData("search")]
    [InlineData("graph")]
    [InlineData("timeline")]
    [InlineData("maps")]
    [InlineData("images")]
    [InlineData("downloads")]
    [InlineData("archives")]
    public async Task Authenticated_owner_reaches_protected_resource_boundary(string resource)
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        owner.DefaultRequestHeaders.Add("X-Test-Owner", "true");
        var response = await owner.GetAsync($"/private/{resource}");
        Assert.Equal(System.Net.HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_token_endpoint_is_public_and_returns_secure_cookie()
    {
        var response = await _client.GetAsync("/auth/csrf");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Host-wiki-csrf=", cookie);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task State_change_without_csrf_token_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email = "owner@example.invalid", password = "not-a-real-password" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Session_cookie_is_secure_http_only_host_cookie_and_strict_same_site()
    {
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.Equal("__Host-wiki-session", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.Equal("/", options.Cookie.Path);
        Assert.Null(options.Cookie.Domain);
    }

    [Fact]
    public void Pending_mfa_cookie_is_secure_host_only_and_short_lived()
    {
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.TwoFactorUserIdScheme);
        Assert.Equal("__Host-wiki-mfa", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.Null(options.Cookie.Domain);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ExpireTimeSpan);
    }

    [Fact]
    public async Task Session_validation_endpoint_requires_authentication()
    {
        var response = await _client.GetAsync("/auth/session");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_owner_can_validate_session()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        owner.DefaultRequestHeaders.Add("X-Test-Owner", "true");
        var response = await owner.GetAsync("/auth/session");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Sensitive_password_change_requires_an_authenticated_owner()
    {
        var csrfResponse = await _client.GetAsync("/auth/csrf");
        using var payload = JsonDocument.Parse(await csrfResponse.Content.ReadAsStringAsync());
        var token = payload.RootElement.GetProperty("token").GetString()!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/password/change")
        {
            Content = JsonContent.Create(new { currentPassword = "old", newPassword = "new" })
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class PrivateApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services => services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "TestOwner";
            options.DefaultChallengeScheme = "TestOwner";
        }).AddScheme<AuthenticationSchemeOptions, OwnerAuthHandler>("TestOwner", _ => { }));
    }
}

public sealed class OwnerAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Owner")) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "owner"), new Claim(ClaimTypes.Name, "owner")], "TestOwner");
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "TestOwner")));
    }
}
