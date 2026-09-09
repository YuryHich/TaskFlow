using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace API.Tests
{
    public class TaskFlowApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string AdminConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=password";
        public const string TestConnectionString = "Host=localhost;Port=5432;Database=TaskFlowDb_Tests;Username=postgres;Password=password";
        public const string JwtKey = "MySuperSecretTestKeyThatIsAtLeast32CharactersLong";
        public const string TestPassword = "TestPass1";

        public async Task InitializeAsync()
        {
            await EnsureDatabaseExistsAsync();
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.UseSetting("Jwt:Key", JwtKey);
            builder.UseSetting("Jwt:Issuer", "TaskFlow");
            builder.UseSetting("Jwt:Audience", "TaskFlow");
            builder.UseSetting("Jwt:AccessTokenExpirationMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenExpirationDays", "7");
        }

        private static async Task EnsureDatabaseExistsAsync()
        {
            await using var connection = new NpgsqlConnection(AdminConnectionString);
            await connection.OpenAsync();

            await using var existsCmd = connection.CreateCommand();
            existsCmd.CommandText = """SELECT 1 FROM pg_database WHERE datname = 'TaskFlowDb_Tests'""";
            var exists = await existsCmd.ExecuteScalarAsync();
            if (exists is not null) return;

            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = "CREATE DATABASE \"TaskFlowDb_Tests\"";
            await createCmd.ExecuteNonQueryAsync();
        }

        public HttpClient CreateApiClient()
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
        }
    }
}