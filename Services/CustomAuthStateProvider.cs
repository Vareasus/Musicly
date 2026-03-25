using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using AycaMusic.Models;

namespace AycaMusic.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _storage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private AppUser? _cachedUser;

    public CustomAuthStateProvider(ProtectedSessionStorage storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await _storage.GetAsync<int>("userId");
            if (!result.Success || result.Value == 0)
                return new AuthenticationState(_anonymous);

            var roleResult = await _storage.GetAsync<string>("userRole");
            var nameResult = await _storage.GetAsync<string>("userName");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.Value.ToString()),
                new(ClaimTypes.Name, nameResult.Value ?? ""),
                new(ClaimTypes.Role, roleResult.Value ?? "User")
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task LoginAsync(AppUser user)
    {
        await _storage.SetAsync("userId", user.Id);
        await _storage.SetAsync("userRole", user.Role);
        await _storage.SetAsync("userName", user.Username);
        _cachedUser = user;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task LogoutAsync()
    {
        await _storage.DeleteAsync("userId");
        await _storage.DeleteAsync("userRole");
        await _storage.DeleteAsync("userName");
        _cachedUser = null;

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_anonymous)));
    }

    public async Task<int> GetCurrentUserIdAsync()
    {
        try
        {
            var result = await _storage.GetAsync<int>("userId");
            return result.Success ? result.Value : 0;
        }
        catch { return 0; }
    }
}
