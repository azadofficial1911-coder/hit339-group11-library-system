using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
namespace LibraryManagementSystem.Security;

// Dedicated credentials: a staff login cookie never grants API access.
public class CatalogueApiAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CatalogueApiKey";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps)
            return Task.FromResult(AuthenticateResult.Fail("HTTPS is required."));
        var expected = configuration["CatalogueApi:Key"];
        if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32)
            return Task.FromResult(AuthenticateResult.Fail("API access is not configured."));
        if (!Request.Headers.TryGetValue("X-Api-Key", out var values) || values.Count != 1)
            return Task.FromResult(AuthenticateResult.Fail("An API key is required."));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(values[0] ?? ""));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        if (!CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "catalogue-reader") }, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "ApiKey";
        return Task.CompletedTask;
    }
}
