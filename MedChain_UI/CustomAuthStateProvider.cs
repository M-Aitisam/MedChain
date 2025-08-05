using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using MedChain_Models.Entities;

public sealed class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ILogger<CustomAuthStateProvider> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomAuthStateProvider(
        ProtectedSessionStorage sessionStorage,
        ILogger<CustomAuthStateProvider> logger,
        UserManager<ApplicationUser> userManager)
    {
        _sessionStorage = sessionStorage;
        _logger = logger;
        _userManager = userManager;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Return anonymous state during prerendering
            if (!OperatingSystem.IsBrowser())
            {
                return AnonymousState();
            }

            // Check session storage for token
            var tokenResult = await _sessionStorage.GetAsync<string>("authToken");
            if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
            {
                _logger.LogDebug("No authentication token found in session storage");
                return AnonymousState();
            }

            // Validate token
            var (isValid, claims) = await ValidateTokenAsync(tokenResult.Value);
            if (!isValid || claims == null)
            {
                _logger.LogWarning("Invalid token found - clearing storage");
                await ClearAuthDataAsync();
                return AnonymousState();
            }

            // Get email claim
            var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogWarning("Token doesn't contain email claim");
                return AnonymousState();
            }

            // Create principal directly from token claims
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            _logger.LogDebug("Authenticated user: {Email}", emailClaim.Value);
            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return AnonymousState();
        }
    }
    public async Task NotifyUserAuthenticationAsync(string token, string email)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");

        try
        {
            _logger.LogDebug("Starting authentication notification for {Email}", email);

            // 1. Store token in session storage
            await _sessionStorage.SetAsync("authToken", token);
            _logger.LogDebug("Token stored in session storage");

            // 2. Validate token and get claims
            var (isValid, claims) = await ValidateTokenAsync(token);
            if (!isValid || claims == null)
            {
                _logger.LogError("Invalid token provided");
                throw new InvalidOperationException("Invalid token provided");
            }

            // 3. Find email claim using multiple possible claim types
            var tokenEmail = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email ||
                c.Type == JwtRegisteredClaimNames.Email)?.Value;

            if (string.IsNullOrEmpty(tokenEmail))
            {
                _logger.LogError("Token missing email claim. Available claims: {@Claims}",
                    claims.Select(c => c.Type));
                throw new InvalidOperationException("Token doesn't contain email claim");
            }

            _logger.LogDebug("Token email: {TokenEmail}, Login email: {LoginEmail}",
                tokenEmail, email);

            // 4. Verify user exists in database
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogError("User not found in database: {Email}", email);
                throw new InvalidOperationException($"User with email {email} not found");
            }

            // 5. Create identity with all claims from token
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            // 6. Notify state change
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
            _logger.LogInformation("Authentication state changed notified for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication notification failed for {Email}", email);
            await ClearAuthDataAsync();

            // Wrap the exception to provide more context
            throw new ApplicationException($"Failed to authenticate user {email}", ex);
        }
    }
    public async Task NotifyUserLogoutAsync()
    {
        try
        {
            await ClearAuthDataAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState()));
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user logout");
            throw;
        }
    }

    private async Task<(bool isValid, IEnumerable<Claim>? claims)> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("Empty token provided for validation");
                return (false, default);
            }

            // Offload token validation to a background thread since it's CPU-bound work
            return await Task.Run(() =>
            {
                if (!_tokenHandler.CanReadToken(token))
                {
                    _logger.LogWarning("Token cannot be read by JWT handler");
                    return (false, default);
                }

                var jwtToken = _tokenHandler.ReadJwtToken(token);
                _logger.LogDebug("Token contains claims: {@Claims}", jwtToken.Claims.Select(c => new { c.Type, c.Value }));

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token has expired (ValidTo: {Expiration})", jwtToken.ValidTo);
                    return (false, default);
                }

                return (true, jwtToken.Claims);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return (false, default);
        }
    }

    private async Task ClearAuthDataAsync() =>
        await _sessionStorage.DeleteAsync("authToken");

    private static AuthenticationState AnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}