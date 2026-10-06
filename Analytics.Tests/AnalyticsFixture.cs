using Analytics.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Analytics.Tests;

public sealed class AnalyticsFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string AdminConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=password";
    public const string TestConnectionString = "Host=localhost;Port=5432;Database=TaskFlowAnalytics_Tests;Username=postgres;Password=password";
    public const string JwtKey = "MySuperSecretTestKeyThatIsAtLeast32CharactersLong";

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var existsCmd = connection.CreateCommand();
        existsCmd.CommandText = """SELECT 1 FROM pg_database WHERE datname = 'TaskFlowAnalytics_Tests'""";
        if (await existsCmd.ExecuteScalarAsync() is null)
        {
            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = "CREATE DATABASE \"TaskFlowAnalytics_Tests\"";
            await createCmd.ExecuteNonQueryAsync();
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Analytics", TestConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", "TaskFlow");
        builder.UseSetting("Jwt:Audience", "TaskFlow");
    }

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE "ProcessedEvents", "TaskFacts", "ProjectFacts" """);
    }
}
