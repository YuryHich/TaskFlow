using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.Tests.Infrastructure;
using Application.DTOs;


namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class AuthTests 
{
    private readonly TaskFlowApiFixture _fixture;

    public AuthTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_ShouldReturn201AndTokens()
    {
        using var client = _fixture.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        string email = $"user-{suffix}@test.local";
        string username = $"user{suffix[..12]}";
        string password = TaskFlowApiFixture.TestPassword;

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = password
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var tokens = await register.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_ShouldReturn200AndTokens()
    {
        using var client = _fixture.CreateClient();

        var suffix = Guid.NewGuid().ToString("N");
        var email = $"user-{suffix}@test.local";
        var username = $"user{suffix[..12]}";
        var password = TaskFlowApiFixture.TestPassword;

        // 1) Сначала регистрируем пользователя
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = password
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        // 2) Потом логинимся теми же данными
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task ReRegister_ShouldReturn409()
    {
        using var client = _fixture.CreateClient();
        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        var reRegister = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = tokens.Email,
            Username =  Guid.NewGuid().ToString("N"),
            Password = TaskFlowApiFixture.TestPassword
        });
        Assert.Equal(HttpStatusCode.Conflict, reRegister.StatusCode);
    }


    [Fact]
    public async Task WrongPassword_ShouldReturn401()
    {
        using var client = _fixture.CreateClient();
        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = tokens.Email,
            Password = "WrongPass1"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Rotation_ShouldReturn200AndTokens()
    {
        using var client = _fixture.CreateClient();

        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);

        // R0 
        var firstRefreshToken = tokens.RefreshToken;
        var firstRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        
        var firstRotation = await firstRefresh.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(firstRotation);

        // R1
        var secondRefreshToken = firstRotation.RefreshToken;

        var secondRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = secondRefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, secondRefresh.StatusCode);

        var secondRotation = await secondRefresh.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(secondRotation);

        // Проверка: refresh rotate работает
        Assert.NotEqual(firstRefreshToken, secondRefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(secondRotation.RefreshToken));
    }

    [Fact]
    public async Task ReuseRefreshToken_ShouldReturn401()
    {
         using var client = _fixture.CreateClient();

        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);

        // R0 
        var firstRefreshToken = tokens.RefreshToken;
        var firstRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        
        var firstRotation = await firstRefresh.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(firstRotation);

        // R1
        var secondRefreshToken = firstRotation.RefreshToken;

        var secondWrongRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, secondWrongRefresh.StatusCode);

        var RevokedRefreshToken = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = secondRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, RevokedRefreshToken.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldReturn204AndRefresh_ShouldReturn401()
    {
        using var client = _fixture.CreateClient();
        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = tokens.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
