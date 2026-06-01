using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Bzn.Cloudios.WebPlatform.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "authToken";
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = ParseJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private List<Claim> ParseJwt(string token)
    {
        var claims = new List<Claim>();

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return claims;

            var payload = parts[1];
            var base64Url = payload.Replace('-', '+').Replace('_', '/');
            var base64 = base64Url.PadRight(base64Url.Length + (4 - base64Url.Length % 4) % 4, '=');
            var jsonBytes = Convert.FromBase64String(base64);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            var payloadData = JsonSerializer.Deserialize<JsonElement>(json);

            if (payloadData.TryGetProperty("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? string.Empty));

            if (payloadData.TryGetProperty("email", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.GetString() ?? string.Empty));

            if (payloadData.TryGetProperty("role", out var role))
                claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));

            if (payloadData.TryGetProperty("realmId", out var realmId))
                claims.Add(new Claim("RealmId", realmId.GetString() ?? string.Empty));

            if (payloadData.TryGetProperty("userId", out var userId))
                claims.Add(new Claim("UserId", userId.GetString() ?? string.Empty));
        }
        catch
        {
            // Invalid token, return empty claims
        }

        return claims;
    }
}
