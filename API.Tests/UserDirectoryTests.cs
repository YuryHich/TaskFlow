using System.Net;
using System.Net.Http.Json;
using API.Tests.Infrastructure;
using Application.DTOs;
using Domain.Models;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class UserDirectoryTests
{
    private readonly TaskFlowApiFixture _fixture;

    public UserDirectoryTests(TaskFlowApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Developer_CanGetMeAndDirectory_ButNotUsersList()
    {
        using var client = _fixture.CreateApiClient();
        var tokens = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        client.WithBearer(tokens.AccessToken);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var meDto = await me.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(meDto);
        Assert.Equal(tokens.UserId, meDto.Id);
        Assert.Equal(tokens.Email, meDto.Email);

        var directory = await client.GetAsync("/api/users/directory");
        Assert.Equal(HttpStatusCode.OK, directory.StatusCode);
        var directoryUsers = await directory.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(directoryUsers);
        Assert.Contains(directoryUsers, user => user.Id == tokens.UserId);

        var users = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task Manager_CanGetUsersList()
    {
        using var client = _fixture.CreateApiClient();
        var registered = await AuthHelper.RegisterAndLoginAsync(client, _fixture);
        await AuthHelper.SetRoleAsync(_fixture, registered.UserId, UserRole.Manager);
        var tokens = await AuthHelper.LoginAsync(client, _fixture, registered.Email);
        client.WithBearer(tokens.AccessToken);

        var users = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }
}
