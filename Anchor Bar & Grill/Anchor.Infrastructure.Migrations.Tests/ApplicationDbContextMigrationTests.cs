using Anchor.Infrastructure.Data;
using Anchor.Domain.Menu;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Anchor.Infrastructure.Migrations.Tests;

public sealed class ApplicationDbContextMigrationTests
{
    [Fact]
    public async Task ApplicationSchemaMigration_AppliesIdentityAndMenuCatalogCleanlyToLocalDb()
    {
        var databaseName = $"AnchorWebIdentity_{Guid.NewGuid():N}";
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

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureDeletedAsync();

        try
        {
            await context.Database.MigrateAsync();

            var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToArray();

            Assert.Contains("00000000000000_CreateIdentitySchema", appliedMigrations);
            Assert.Contains("20260513172154_AddBootstrapIdentityFields", appliedMigrations);
            Assert.Contains("20260514200752_AddManagedUserProfileFields", appliedMigrations);
            Assert.Contains("20260515170550_AddAccountConfirmedFlag", appliedMigrations);
            Assert.Contains("20260517015850_AddMenuCatalog", appliedMigrations);
            Assert.Contains("20260518213938_LinkSeededRecurringSpecialsToMenuItems", appliedMigrations);
            Assert.Empty(pendingMigrations);
            Assert.True(await context.Database.CanConnectAsync());

            var columnNames = await GetAspNetUsersColumnNamesAsync(connectionString);
            Assert.Contains("AccountConfirmed", columnNames);
            Assert.Contains("FirstName", columnNames);
            Assert.Contains("MustChangePassword", columnNames);
            Assert.Contains("IsBootstrapAccount", columnNames);
            Assert.Contains("LastName", columnNames);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = "bootstrap@anchor.test",
                NormalizedUserName = "BOOTSTRAP@ANCHOR.TEST",
                Email = "bootstrap@anchor.test",
                NormalizedEmail = "BOOTSTRAP@ANCHOR.TEST",
                FirstName = "Bootstrap",
                LastName = "Captain",
                AccountConfirmed = true,
                MustChangePassword = true,
                IsBootstrapAccount = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var persistedUser = await context.Users.SingleAsync(savedUser => savedUser.Id == user.Id);
            Assert.Equal("Bootstrap", persistedUser.FirstName);
            Assert.Equal("Captain", persistedUser.LastName);
            Assert.True(persistedUser.AccountConfirmed);
            Assert.True(persistedUser.MustChangePassword);
            Assert.True(persistedUser.IsBootstrapAccount);

            var tableNames = await GetTableNamesAsync(connectionString);
            Assert.Contains("MenuSections", tableNames);
            Assert.Contains("MenuItems", tableNames);
            Assert.Contains("MenuItemPriceVariants", tableNames);
            Assert.Contains("MenuItemTabs", tableNames);
            Assert.Contains("RecurringSpecials", tableNames);
            Assert.Contains("MenuServiceWindows", tableNames);

            Assert.True(await context.MenuSections.AnyAsync(section => section.Name == "Appetizers"));
            Assert.True(await context.MenuItems.AnyAsync(item => item.Name == "Cheese Curds"));
            Assert.True(await context.MenuItems.AnyAsync(item => item.MenuItemId == Guid.Parse("9E7F7A6B-C8DB-4E8D-B2EF-A60A40E91F70") && !item.IsVisibleToGuests));
            Assert.True(await context.MenuItemPriceVariants.AnyAsync(variant => variant.Label == "Bowl" && variant.Amount == 6m));
            Assert.True(await context.MenuItemTabs.AnyAsync(link => link.MenuItemId == Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A") && link.Tab == MenuTab.Dinner));
            Assert.True(await context.RecurringSpecials.AnyAsync(special => special.Title == "Monday Night Burgers"));
            Assert.True(await context.RecurringSpecials.AnyAsync(special => special.RecurringSpecialId == Guid.Parse("6BAA63B3-55C9-4E47-8555-803573B9B38D") && special.LinkedMenuItemId == Guid.Parse("9E7F7A6B-C8DB-4E8D-B2EF-A60A40E91F70")));
            Assert.True(await context.MenuServiceWindows.AnyAsync(window => window.Tab == MenuTab.Drinks && window.DayOfWeek == DayOfWeek.Friday && window.ClosesNextDay));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task AddAccountConfirmedFlag_copies_existing_email_confirmation_state()
    {
        var databaseName = $"AnchorWebIdentity_{Guid.NewGuid():N}";
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

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureDeletedAsync();

        const string migrationBeforeAccountConfirmed = "20260514200752_AddManagedUserProfileFields";
        var confirmedUserId = Guid.NewGuid().ToString("N");
        var pendingUserId = Guid.NewGuid().ToString("N");

        try
        {
            await context.Database.MigrateAsync(migrationBeforeAccountConfirmed);

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO AspNetUsers
                (
                    Id,
                    AccessFailedCount,
                    Email,
                    EmailConfirmed,
                    IsBootstrapAccount,
                    LockoutEnabled,
                    MustChangePassword,
                    PhoneNumberConfirmed,
                    TwoFactorEnabled,
                    UserName
                )
                VALUES
                ({0}, 0, {1}, 1, 0, 0, 0, 0, 0, {1}),
                ({2}, 0, {3}, 0, 0, 0, 0, 0, 0, {3});
                """,
                confirmedUserId,
                "confirmed@anchor.test",
                pendingUserId,
                "pending@anchor.test");

            await context.Database.MigrateAsync();

            var states = await GetAccountConfirmationStatesAsync(connectionString);

            Assert.True(states[confirmedUserId].EmailConfirmed);
            Assert.True(states[confirmedUserId].AccountConfirmed);
            Assert.False(states[pendingUserId].EmailConfirmed);
            Assert.False(states[pendingUserId].AccountConfirmed);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<IReadOnlyList<string>> GetAspNetUsersColumnNamesAsync(string connectionString)
    {
        var columnNames = new List<string>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'AspNetUsers'
            ORDER BY COLUMN_NAME;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnNames.Add(reader.GetString(0));
        }

        return columnNames;
    }

    private static async Task<IReadOnlyList<string>> GetTableNamesAsync(string connectionString)
    {
        var tableNames = new List<string>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private static async Task<IReadOnlyDictionary<string, (bool AccountConfirmed, bool EmailConfirmed)>> GetAccountConfirmationStatesAsync(string connectionString)
    {
        var states = new Dictionary<string, (bool AccountConfirmed, bool EmailConfirmed)>(StringComparer.Ordinal);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, AccountConfirmed, EmailConfirmed
            FROM AspNetUsers;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            states.Add(
                reader.GetString(0),
                (reader.GetBoolean(1), reader.GetBoolean(2)));
        }

        return states;
    }
}

