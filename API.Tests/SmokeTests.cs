using System.Net;
using API.Tests.Infrastructure;

namespace API.Tests;

[Collection(ApiTestCollection.Name)]
public class SmokeTests
{
    private readonly HttpClient _client;

    public SmokeTests(TaskFlowApiFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetProjects_WithoutToken_Returns401(){
        var response = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
