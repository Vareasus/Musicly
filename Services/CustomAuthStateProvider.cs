using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using Musicly.Models;

namespace Musicly.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly ProtectedLocalStorage _localStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private AppUser? _cachedUser;

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage, ProtectedLocalStorage localStorage)
    {
        _sessionStorage = sessionStorage;
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // First check session storage
            var result = await _sessionStorage.GetAsync<int>("userId");
            if (result.Success && result.Value != 0)
            {
                return await BuildAuthState(result.Value);
            }

            // Then check local storage ("Remember Me")
            var localResult = await _localStorage.GetAsync<int>("userId");
            if (localResult.Success && localResult.Value != 0)
            {
                // Restore session from local storage
                var roleResult = await _localStorage.GetAsync<string>("userRole");
                var nameResult = await _localStorage.GetAsync<string>("userName");

                await _sessionStorage.SetAsync("userId", localResult.Value);
                await _sessionStorage.SetAsync("userRole", roleResult.Value ?? "User");
                await _sessionStorage.SetAsync("userName", nameResult.Value ?? "");

                return await BuildAuthState(localResult.Value);
            }

            return new AuthenticationState(_anonymous);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    private async Task<AuthenticationState> BuildAuthState(int userId)
    {
        var roleResult = await _sessionStorage.GetAsync<string>("userRole");
        var nameResult = await _sessionStorage.GetAsync<string>("userName");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, nameResult.Value ?? ""),
            new(ClaimTypes.Role, roleResult.Value ?? "User")
        };

        var identity = new ClaimsIdentity(claims, "CustomAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task LoginAsync(AppUser user, bool rememberMe = false)
    {
        // Always save to session
        await _sessionStorage.SetAsync("userId", user.Id);
        await _sessionStorage.SetAsync("userRole", user.Role);
        await _sessionStorage.SetAsync("userName", user.Username);
        _cachedUser = user;

        // If "Remember Me", also save to local storage (persistent)
        if (rememberMe)
        {
            await _localStorage.SetAsync("userId", user.Id);
            await _localStorage.SetAsync("userRole", user.Role);
            await _localStorage.SetAsync("userName", user.Username);
        }

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
        // Clear both storages
        await _sessionStorage.DeleteAsync("userId");
        await _sessionStorage.DeleteAsync("userRole");
        await _sessionStorage.DeleteAsync("userName");
        await _localStorage.DeleteAsync("userId");
        await _localStorage.DeleteAsync("userRole");
        await _localStorage.DeleteAsync("userName");
        _cachedUser = null;

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_anonymous)));
    }

    public async Task<int> GetCurrentUserIdAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<int>("userId");
            return result.Success ? result.Value : 0;
        }
        catch { return 0; }
    }
}
