using Anchor.Web.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Anchor.Web.Migrations.Tests;

public sealed class ApplicationDbContextMigrationTests
{
    [Fact]
    public async Task IdentitySchemaMigration_AppliesCleanlyToLocalDb()
    {
        var databaseName = $"AnchorWebMockup_{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 30
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var dbContext = new ApplicationDbContext(options);

        try
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

            Assert.Contains("00000000000000_CreateIdentitySchema", appliedMigrations);
            Assert.Empty(pendingMigrations);
            Assert.True(await dbContext.Database.CanConnectAsync());
        }
        finally
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }
}
