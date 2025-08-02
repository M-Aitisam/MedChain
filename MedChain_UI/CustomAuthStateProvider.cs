using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

public sealed class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ILogger<CustomAuthStateProvider> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public CustomAuthStateProvider(
        ProtectedSessionStorage sessionStorage,
        ILogger<CustomAuthStateProvider> logger)
    {
        _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Return anonymous state during prerendering
            if (OperatingSystem.IsBrowser() == false)
            {
                return CreateAnonymousState();
            }

            var tokenResult = await _sessionStorage.GetAsync<string>("authToken");
            if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
            {
                _logger.LogDebug("No authentication token found in storage");
                return CreateAnonymousState();
            }

            var validationResult = await ValidateAndParseToken(tokenResult.Value);
            if (!validationResult.isValid || validationResult.claims is null)
            {
                _logger.LogWarning("Invalid token found - clearing storage");
                await _sessionStorage.DeleteAsync("authToken");
                return CreateAnonymousState();
            }

            var principal = CreatePrincipal(validationResult.claims);
            _logger.LogInformation("Authenticated user: {UserName}", principal.Identity?.Name);
            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication state");
            return CreateAnonymousState();
        }
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Attempted to notify authentication with empty token");
            return;
        }

        try
        {
            var validationResult = await ValidateAndParseToken(token);
            if (!validationResult.isValid || validationResult.claims is null)
            {
                _logger.LogWarning("Invalid token provided for authentication");
                return;
            }

            await _sessionStorage.SetAsync("authToken", token);
            var principal = CreatePrincipal(validationResult.claims);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
            _logger.LogInformation("Successfully notified user authentication");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify user authentication");
        }
    }

    public async Task NotifyUserLogoutAsync()
    {
        try
        {
            await _sessionStorage.DeleteAsync("authToken");
            NotifyAuthenticationStateChanged(Task.FromResult(CreateAnonymousState()));
            _logger.LogInformation("Successfully notified user logout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify user logout");
        }
    }

    private async Task<(bool isValid, IEnumerable<Claim>? claims)> ValidateAndParseToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("Empty token provided for validation");
                return (false, null);
            }

            if (!_tokenHandler.CanReadToken(token))
            {
                _logger.LogWarning("Token cannot be read by JWT handler");
                return (false, null);
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token has expired (ValidTo: {Expiration})", jwtToken.ValidTo);
                return (false, null);
            }

            return (true, jwtToken.Claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return (false, null);
        }
    }

    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
        => new(new ClaimsIdentity(claims, "jwt"));

    private static AuthenticationState CreateAnonymousState()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));
}