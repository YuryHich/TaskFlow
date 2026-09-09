using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Domain.Models;

namespace API.Tests.Infrastructure;

public record AuthTokens(string AccessToken, string RefreshToken, Guid UserId, string Email);

public static class AuthHelper
{
    public static async Task<AuthTokens> RegisterAndLoginAsync(
        HttpClient client,
        TaskFlowApiFixture fixture,
        string? email = null,
        string? username = null,
        string? password = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        email ??= $"user-{suffix}@test.local";
        username ??= $"user{suffix[..12]}";
        password ??= TaskFlowApiFixture.TestPassword;

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest{
            Email = email,
            Username = username,
            Password = password
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest{
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();

        var tokens = (await login.Content.ReadFromJsonAsync<TokenResponse>())
        ?? throw new InvalidOperationException("Login returned empty body.");

        Guid userId;
        using (var scope = fixture.Services.CreateScope()){
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        }

        return new AuthTokens(tokens.AccessToken, tokens.RefreshToken, userId, email);
    }

    public static HttpClient WithBearer(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static async Task SetRoleAsync(TaskFlowApiFixture fixture, Guid userId, UserRole role)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(userId)
        ?? throw new InvalidOperationException($"User with ID {userId} not found.");
        user.Role = role;
        await db.SaveChangesAsync();
    }
}
