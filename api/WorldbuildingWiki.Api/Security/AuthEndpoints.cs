using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorldbuildingWiki.Api.Data;

namespace WorldbuildingWiki.Api.Security;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth").AllowAnonymous().RequireRateLimiting("auth");

        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        auth.MapGet("/setup-status", async (WikiDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(config["OwnerSetup:Key"])) return Results.NotFound();
            var owner = await db.Users.SingleOrDefaultAsync();
            return Results.Ok(new { setupAvailable = owner is null || !owner.EmailConfirmed || !owner.TwoFactorEnabled });
        });

        auth.MapPost("/setup", async (SetupRequest request, IConfiguration config, UserManager<WikiUser> users, IEmailSender<WikiUser> emailSender) =>
        {
            var expectedKey = config["OwnerSetup:Key"];
            if (string.IsNullOrWhiteSpace(expectedKey) || !FixedTimeEquals(request.SetupKey, expectedKey)) return Results.NotFound();
            var publicOrigin = config["Email:PublicOrigin"];
            if (string.IsNullOrWhiteSpace(publicOrigin) || string.IsNullOrWhiteSpace(config["Email:Smtp:Host"]) || string.IsNullOrWhiteSpace(config["Email:From"]))
                return Results.Problem("Owner email verification is not configured.", statusCode: 503);
            if (!IsAllowedPublicOrigin(publicOrigin, config["ASPNETCORE_ENVIRONMENT"]))
                return Results.Problem("Owner email verification origin must use HTTPS.", statusCode: 503);
            if (await users.Users.AnyAsync()) return Results.Conflict(new { error = "Setup is unavailable." });
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });
            var email = request.Email.Trim();
            var user = new WikiUser { UserName = email, Email = email, EmailConfirmed = false };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return Results.BadRequest(new { error = "The account could not be created." });

            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var confirmationUrl = $"{publicOrigin.TrimEnd('/')}/setup#email={Uri.EscapeDataString(user.Email!)}&code={Uri.EscapeDataString(token)}";
            try { await emailSender.SendConfirmationLinkAsync(user, user.Email!, confirmationUrl); }
            catch (Exception) { return Results.Problem("Owner email verification is unavailable.", statusCode: 503); }
            return Results.Ok(new { emailVerificationRequired = true });
        });

        auth.MapPost("/setup/confirm-email", async (ConfirmEmailRequest request, UserManager<WikiUser> users) =>
        {
            var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email);
            if (user is null || user.EmailConfirmed || string.IsNullOrWhiteSpace(request.Code)) return Results.BadRequest(new { error = "Email confirmation failed." });
            var confirmed = await users.ConfirmEmailAsync(user, request.Code);
            if (!confirmed.Succeeded) return Results.BadRequest(new { error = "Email confirmation failed." });
            await users.ResetAuthenticatorKeyAsync(user);
            var key = await users.GetAuthenticatorKeyAsync(user);
            var issuer = Uri.EscapeDataString("Worldbuilding Wiki");
            var label = Uri.EscapeDataString(user.Email!);
            return Results.Ok(new { authenticatorKey = key, otpauthUri = $"otpauth://totp/{issuer}:{label}?secret={key}&issuer={issuer}&digits=6" });
        });

        auth.MapPost("/setup/confirm-totp", async (ConfirmTotpRequest request, UserManager<WikiUser> users, WikiDbContext db) =>
        {
            var user = await db.Users.SingleOrDefaultAsync();
            if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(request.Code)) return Results.NotFound();
            if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code)))
                return Results.BadRequest(new { error = "The code is invalid." });
            var enabled = await users.SetTwoFactorEnabledAsync(user, true);
            if (!enabled.Succeeded) return Results.BadRequest(new { error = "MFA could not be enabled." });
            var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            await users.UpdateSecurityStampAsync(user);
            return Results.Ok(new { recoveryCodes });
        });

        auth.MapPost("/login", async (LoginRequest request, SignInManager<WikiUser> signIn) =>
        {
            var result = await signIn.PasswordSignInAsync(request.Email ?? string.Empty, request.Password ?? string.Empty,
                isPersistent: false, lockoutOnFailure: true);
            if (result.RequiresTwoFactor) return Results.Ok(new { requiresMfa = true });
            if (!result.Succeeded) return Results.Unauthorized();
            await signIn.SignOutAsync();
            return Results.Unauthorized();
        });

        auth.MapPost("/login/totp", async (TotpRequest request, SignInManager<WikiUser> signIn) =>
        {
            var result = await signIn.TwoFactorAuthenticatorSignInAsync(NormalizeCode(request.Code), false, true);
            return result.Succeeded ? Results.Ok(new { authenticated = true }) : Results.Unauthorized();
        });

        auth.MapPost("/login/recovery", async (RecoveryRequest request, SignInManager<WikiUser> signIn) =>
        {
            var result = await signIn.TwoFactorRecoveryCodeSignInAsync(request.Code ?? string.Empty);
            return result.Succeeded ? Results.Ok(new { authenticated = true }) : Results.Unauthorized();
        });

        auth.MapPost("/logout", async (SignInManager<WikiUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        auth.MapPost("/password-reset/request", async (ResetRequest request, UserManager<WikiUser> users, IEmailSender<WikiUser> sender) =>
        {
            var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email.Trim());
            if (user is not null && await users.IsEmailConfirmedAsync(user))
            {
                var code = await users.GeneratePasswordResetTokenAsync(user);
                try { await sender.SendPasswordResetCodeAsync(user, user.Email!, code); }
                catch (InvalidOperationException) { /* Keep reset responses identical when delivery is not configured. */ }
                catch (Exception) { /* Do not disclose delivery state or account existence. */ }
            }
            return Results.Ok(new { message = "If the address is verified, reset instructions will be sent." });
        });

        auth.MapPost("/password-reset/complete", async (ResetPasswordRequest request, UserManager<WikiUser> users) =>
        {
            var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email.Trim());
            if (user is null || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "The reset could not be completed." });
            var result = await users.ResetPasswordAsync(user, request.Code, request.NewPassword);
            if (!result.Succeeded) return Results.BadRequest(new { error = "The reset could not be completed." });
            await users.UpdateSecurityStampAsync(user);
            return Results.Ok(new { passwordReset = true });
        });

        endpoints.MapPost("/auth/password/change", async (ChangePasswordRequest request, UserManager<WikiUser> users, SignInManager<WikiUser> signIn, HttpContext context) =>
        {
            var user = await users.GetUserAsync(context.User);
            if (user is null) return Results.Unauthorized();
            var result = await users.ChangePasswordAsync(user, request.CurrentPassword ?? string.Empty, request.NewPassword ?? string.Empty);
            if (!result.Succeeded) return Results.BadRequest(new { error = "Password change failed." });
            await users.UpdateSecurityStampAsync(user);
            await signIn.SignOutAsync();
            return Results.Ok(new { passwordChanged = true, requiresReauthentication = true });
        }).RequireAuthorization().RequireRateLimiting("auth");

        endpoints.MapGet("/auth/session", (HttpContext context) => Results.Ok(new { authenticated = true, email = context.User.Identity?.Name }))
            .RequireAuthorization();

        return endpoints;
    }

    private static bool FixedTimeEquals(string? supplied, string expected)
    {
        if (supplied is null) return false;
        var left = Encoding.UTF8.GetBytes(supplied);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Replace(" ", "").Replace("-", "");

    private static bool IsAllowedPublicOrigin(string value, string? environment)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        return uri.Scheme == Uri.UriSchemeHttps || environment == "Development" && uri.Scheme == Uri.UriSchemeHttp &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             IPAddress.TryParse(uri.Host, out var address) && IPAddress.IsLoopback(address));
    }

    public sealed record SetupRequest(string SetupKey, string Email, string Password);
    public sealed record ConfirmEmailRequest(string? Email, string? Code);
    public sealed record ConfirmTotpRequest(string Code);
    public sealed record LoginRequest(string? Email, string? Password);
    public sealed record TotpRequest(string? Code);
    public sealed record RecoveryRequest(string? Code);
    public sealed record ResetRequest(string? Email);
    public sealed record ResetPasswordRequest(string? Email, string? Code, string? NewPassword);
    public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
}
