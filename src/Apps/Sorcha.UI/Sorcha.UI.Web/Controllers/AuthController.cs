using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Sorcha.UI.Web.Controllers;

/// <summary>
/// Authentication controller that handles login/logout.
/// Sets HttpOnly cookies for nav state AND returns Bearer tokens for WASM.
/// </summary>
[ApiController]
[Route("api/ui-auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");

            // Determine the API gateway URL:
            // 1. Use explicit URL if provided by the client (Development mode)
            // 2. Use configured ApiGateway:BaseUrl (Docker mode - internal network)
            // 3. Fall back to same origin (Request.Host)
            string tokenUrl;
            if (!string.IsNullOrEmpty(request.ApiGatewayUrl))
            {
                tokenUrl = $"{request.ApiGatewayUrl}/api/service-auth/token";
            }
            else
            {
                var configuredGatewayUrl = _configuration["ApiGateway:BaseUrl"];
                if (!string.IsNullOrEmpty(configuredGatewayUrl))
                {
                    tokenUrl = $"{configuredGatewayUrl}/api/service-auth/token";
                    _logger.LogDebug("Using configured API Gateway URL: {Url}", tokenUrl);
                }
                else
                {
                    tokenUrl = $"{Request.Scheme}://{Request.Host}/api/service-auth/token";
                }
            }

            var formData = new Dictionary<string, string>
            {
                ["username"] = request.Username,
                ["password"] = request.Password,
                ["grant_type"] = "password",
                ["client_id"] = "sorcha-ui-web"
            };

            var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed: {StatusCode}", response.StatusCode);
                return Unauthorized(new { error = "Invalid credentials" });
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return Unauthorized(new { error = "Invalid token response" });
            }

            // Parse JWT to get user info for the cookie
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenResponse.AccessToken);
            
            var claims = new List<Claim>();
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
            if (subClaim != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                claims.Add(new Claim(ClaimTypes.Name, subClaim.Value));
            }
            claims.Add(new Claim("profile", request.ProfileName ?? "Development"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            };

            // Store tokens for WASM handoff
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken ?? "" },
                new AuthenticationToken { Name = "expires_at", Value = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("O") }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            return Ok(new LoginResponse
            {
                Success = true,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { success = true });
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetAuthState()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthStateResponse { IsAuthenticated = false });
        }

        var authResult = await HttpContext.AuthenticateAsync();
        return Ok(new AuthStateResponse
        {
            IsAuthenticated = true,
            Username = User.Identity.Name,
            AccessToken = authResult.Properties?.GetTokenValue("access_token"),
            RefreshToken = authResult.Properties?.GetTokenValue("refresh_token"),
            ExpiresAt = authResult.Properties?.GetTokenValue("expires_at"),
            ProfileName = User.Claims.FirstOrDefault(c => c.Type == "profile")?.Value
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ProfileName { get; set; }
    public string? ApiGatewayUrl { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? TokenType { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}

public class AuthStateResponse
{
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? ExpiresAt { get; set; }
    public string? ProfileName { get; set; }
}
