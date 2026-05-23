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
            Assert.Contains("20260519134817_RefactorRecurringSpecialsToMenuItemSpecials", appliedMigrations);
            Assert.Contains("20260522221240_AddMenuSectionCalloutsAndUniqueMenuNames", appliedMigrations);
            Assert.Contains("20260522233151_AddMenuSectionVisibilityAndMultiSectionAssignments", appliedMigrations);
            Assert.Contains("20260523013508_AddMenuHierarchyAndSeasonalAvailability", appliedMigrations);
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
            Assert.Contains("MenuItemSectionAssignments", tableNames);
            Assert.Contains("MenuItemSpecialDays", tableNames);
            Assert.Contains("MenuItemTabs", tableNames);
            Assert.Contains("MenuItemSpecials", tableNames);
            Assert.Contains("MenuSectionTabs", tableNames);
            Assert.Contains("MenuServiceWindows", tableNames);
            Assert.DoesNotContain("RecurringSpecials", tableNames);

            Assert.True(await context.MenuSections.AnyAsync(section => section.Name == "Appetizers"));
            Assert.True(await context.MenuItems.AnyAsync(item => item.Name == "Cheese Curds"));
            Assert.False(await context.MenuItems.AnyAsync(item => item.MenuItemId == Guid.Parse("9E7F7A6B-C8DB-4E8D-B2EF-A60A40E91F70")));
            Assert.True(await context.MenuItems.AnyAsync(item => item.MenuItemId == Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E") && item.Name == "Monday Night Burgers"));
            Assert.True(await context.MenuItems.AnyAsync(item => item.MenuItemId == Guid.Parse("6BAA63B3-55C9-4E47-8555-803573B9B38D") && item.Name == "Sunday Pork Chop Dinner"));
            Assert.True(await context.MenuItemPriceVariants.AnyAsync(variant => variant.Label == "Bowl" && variant.Amount == 6m));
            Assert.True(await context.MenuItemTabs.AnyAsync(link => link.MenuItemId == Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E") && link.Tab == MenuTab.Dinner));
            Assert.True(await context.MenuItemSpecialDays.AnyAsync(day => day.MenuItemId == Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E") && day.DayOfWeek == DayOfWeek.Monday));
            Assert.True(await context.MenuItemSpecials.AnyAsync(special => special.MenuItemId == Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E") && special.StartsAt == new TimeOnly(17, 0) && special.StartDate == null));
            Assert.True(await context.MenuItemSpecials.AnyAsync(special => special.MenuItemId == Guid.Parse("6BAA63B3-55C9-4E47-8555-803573B9B38D") && special.Callout == "$17 dinner plate"));
            Assert.True(await context.MenuServiceWindows.AnyAsync(window => window.Tab == MenuTab.Drinks && window.DayOfWeek == DayOfWeek.Friday && window.ClosesNextDay));
            var menuSectionColumns = await GetColumnNamesAsync(connectionString, "MenuSections");
            var menuItemColumns = await GetColumnNamesAsync(connectionString, "MenuItems");
            Assert.Contains("Callout", menuSectionColumns);
            Assert.Contains("ParentSectionId", menuSectionColumns);
            Assert.Contains("NormalizedName", menuSectionColumns);
            Assert.Contains("NormalizedName", menuItemColumns);
            Assert.Contains("SeasonStartMonth", menuItemColumns);
            Assert.Contains("SeasonStartDay", menuItemColumns);
            Assert.Contains("SeasonEndMonth", menuItemColumns);
            Assert.Contains("SeasonEndDay", menuItemColumns);
            Assert.Contains("UsesSectionVisibility", menuItemColumns);
            Assert.DoesNotContain("MenuSectionId", menuItemColumns);
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

    [Fact]
    public async Task RefactorRecurringSpecialsToMenuItemSpecials_promotes_existing_legacy_special_rows()
    {
        var databaseName = $"AnchorWebMenu_{Guid.NewGuid():N}";
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

        const string migrationBeforeSpecialRefactor = "20260518213938_LinkSeededRecurringSpecialsToMenuItems";
        var customSpecialId = Guid.Parse("71E1F47F-2CF1-4FC6-9E59-F6E54E0FF4A2");

        try
        {
            await context.Database.MigrateAsync(migrationBeforeSpecialRefactor);

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO RecurringSpecials
                (
                    RecurringSpecialId,
                    Tab,
                    MenuSectionId,
                    DayOfWeek,
                    Title,
                    Description,
                    TimeNote,
                    PriceNote,
                    LinkedMenuItemId,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived
                )
                VALUES
                (
                    {0},
                    {1},
                    {2},
                    {3},
                    {4},
                    {5},
                    {6},
                    {7},
                    {8},
                    {9},
                    1,
                    0
                );
                """,
                customSpecialId,
                (int)MenuTab.Dinner,
                Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B"),
                (int)DayOfWeek.Thursday,
                "Thursday Burger Blitz",
                "Custom recurring special saved before the refactor migration.",
                "5:30 PM - 10:00 PM",
                "$13 basket special",
                Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A"),
                9);

            await context.Database.MigrateAsync();

            var promotedItem = await context.MenuItems.SingleAsync(item => item.MenuItemId == customSpecialId);
            var promotedPriceVariants = await context.MenuItemPriceVariants
                .Where(variant => variant.MenuItemId == customSpecialId)
                .OrderBy(variant => variant.SortOrder)
                .ToListAsync();
            var promotedTabs = await context.MenuItemTabs
                .Where(link => link.MenuItemId == customSpecialId)
                .Select(link => link.Tab)
                .ToListAsync();
            var promotedAssignments = await context.MenuItemSectionAssignments
                .Where(assignment => assignment.MenuItemId == customSpecialId)
                .Select(assignment => assignment.MenuSectionId)
                .ToListAsync();
            var promotedSpecial = await context.MenuItemSpecials.SingleAsync(special => special.MenuItemId == customSpecialId);
            var promotedSpecialDays = await context.MenuItemSpecialDays
                .Where(day => day.MenuItemId == customSpecialId)
                .Select(day => day.DayOfWeek)
                .ToListAsync();

            Assert.Equal("Thursday Burger Blitz", promotedItem.Name);
            Assert.Equal("Custom recurring special saved before the refactor migration.", promotedItem.Description);
            Assert.Equal([Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B")], promotedAssignments);
            Assert.Equal("images/menu/burgers.svg", promotedItem.ImagePath);
            Assert.False(promotedItem.IsArchived);
            Assert.True(promotedItem.IsVisibleToGuests);
            Assert.Single(promotedPriceVariants);
            Assert.Equal("Regular", promotedPriceVariants[0].Label);
            Assert.Equal(11m, promotedPriceVariants[0].Amount);
            Assert.Equal([MenuTab.Dinner], promotedTabs);
            Assert.Equal(MenuItemSpecialScheduleKind.WeeklyRecurring, promotedSpecial.ScheduleKind);
            Assert.Equal([DayOfWeek.Thursday], promotedSpecialDays);
            Assert.Null(promotedSpecial.StartDate);
            Assert.Equal(new TimeOnly(17, 30), promotedSpecial.StartsAt);
            Assert.Equal(new TimeOnly(22, 0), promotedSpecial.EndsAt);
            Assert.False(promotedSpecial.ClosesNextDay);
            Assert.Equal("$13 basket special", promotedSpecial.Callout);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task AddMenuSectionCalloutsAndUniqueMenuNames_renames_duplicate_sections_and_items_before_applying_unique_indexes()
    {
        var databaseName = $"AnchorWebMenu_{Guid.NewGuid():N}";
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

        const string migrationBeforeUniqueMenuNames = "20260519134817_RefactorRecurringSpecialsToMenuItemSpecials";
        var firstDuplicateSectionId = Guid.Parse("3A16D079-3A42-4F02-A8D4-5A110665D4D8");
        var secondDuplicateSectionId = Guid.Parse("97F83F31-98A1-42E0-BEDB-3EC2735CF447");
        var firstDuplicateItemId = Guid.Parse("CC2A834A-ED95-4F63-9BE3-A43C878C23A2");
        var secondDuplicateItemId = Guid.Parse("A63AF6CD-EB1D-43E7-8C7B-760B43A818D8");

        try
        {
            await context.Database.MigrateAsync(migrationBeforeUniqueMenuNames);

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO MenuSections
                (
                    MenuSectionId,
                    Name,
                    Family,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived
                )
                VALUES
                ({0}, {1}, {2}, 90, 1, 0),
                ({3}, {4}, {5}, 91, 1, 0);

                INSERT INTO MenuItems
                (
                    MenuItemId,
                    MenuSectionId,
                    Name,
                    Description,
                    ImagePath,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived,
                    OfferStartsOn,
                    OfferEndsOn,
                    IsSeasonal
                )
                VALUES
                ({6}, {0}, {7}, {8}, NULL, 1, 1, 0, NULL, NULL, 0),
                ({9}, {3}, {10}, {11}, NULL, 1, 1, 0, NULL, NULL, 0);
                """,
                firstDuplicateSectionId,
                "Late Plates",
                (int)MenuFamily.Food,
                secondDuplicateSectionId,
                " late plates ",
                (int)MenuFamily.Food,
                firstDuplicateItemId,
                "Night Soda",
                "First duplicate item before the normalized-name migration.",
                secondDuplicateItemId,
                " night soda ",
                "Second duplicate item before the normalized-name migration.");

            await context.Database.MigrateAsync();

            var duplicatedSections = await context.MenuSections
                .Where(section => section.MenuSectionId == firstDuplicateSectionId || section.MenuSectionId == secondDuplicateSectionId)
                .OrderBy(section => section.SortOrder)
                .ToListAsync();
            var duplicatedItems = await context.MenuItems
                .Where(item => item.MenuItemId == firstDuplicateItemId || item.MenuItemId == secondDuplicateItemId)
                .OrderBy(item => item.MenuItemId)
                .ToListAsync();

            Assert.Contains(duplicatedSections, section => string.Equals(section.Name, "Late Plates", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(duplicatedSections, section => section.Name.Contains("(Duplicate)", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(2, duplicatedSections.Select(section => section.NormalizedName).Distinct(StringComparer.Ordinal).Count());
            Assert.Contains(duplicatedItems, item => string.Equals(item.Name, "Night Soda", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(duplicatedItems, item => item.Name.Contains("(Duplicate)", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(2, duplicatedItems.Select(item => item.NormalizedName).Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task AddMenuSectionVisibilityAndMultiSectionAssignments_backfills_section_assignments_and_section_tabs()
    {
        var databaseName = $"AnchorWebMenu_{Guid.NewGuid():N}";
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

        const string migrationBeforeMultiSectionSupport = "20260522221240_AddMenuSectionCalloutsAndUniqueMenuNames";
        var breakfastSectionId = Guid.Parse("12D8D1E5-D7E1-49F8-9F4A-D34764E46B91");
        var emptyDrinkSectionId = Guid.Parse("1A2E2A5C-3C0A-4C11-B542-B95D8AF48A4D");
        var itemId = Guid.Parse("3D1C68B8-C0E7-4D51-A5BE-C0B9D8C7F8F6");

        try
        {
            await context.Database.MigrateAsync(migrationBeforeMultiSectionSupport);

            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO MenuSections
                (
                    MenuSectionId,
                    Name,
                    NormalizedName,
                    Callout,
                    Family,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived
                )
                VALUES
                ({0}, {1}, {2}, NULL, {3}, 90, 1, 0),
                ({4}, {5}, {6}, NULL, {7}, 91, 1, 0);

                INSERT INTO MenuItems
                (
                    MenuItemId,
                    MenuSectionId,
                    Name,
                    NormalizedName,
                    Description,
                    ImagePath,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived,
                    OfferStartsOn,
                    OfferEndsOn,
                    IsSeasonal
                )
                VALUES
                ({8}, {0}, {9}, {10}, {11}, NULL, 9, 1, 0, NULL, NULL, 0);

                INSERT INTO MenuItemTabs
                (
                    MenuItemId,
                    Tab
                )
                VALUES
                ({8}, {12}),
                ({8}, {13});
                """,
                breakfastSectionId,
                "Breakfast Specials",
                "BREAKFAST SPECIALS",
                (int)MenuFamily.Food,
                emptyDrinkSectionId,
                "Nightcap List",
                "NIGHTCAP LIST",
                (int)MenuFamily.Drink,
                itemId,
                "Everything Toast",
                "EVERYTHING TOAST",
                "Moves between breakfast and lunch.",
                (int)MenuTab.Breakfast,
                (int)MenuTab.Lunch);

            await context.Database.MigrateAsync();

            var assignments = await context.MenuItemSectionAssignments
                .Where(assignment => assignment.MenuItemId == itemId)
                .OrderBy(assignment => assignment.MenuSectionId)
                .ToListAsync();
            var breakfastSectionTabs = await context.MenuSectionTabs
                .Where(link => link.MenuSectionId == breakfastSectionId)
                .OrderBy(link => link.Tab)
                .Select(link => link.Tab)
                .ToArrayAsync();
            var emptyDrinkSectionTabs = await context.MenuSectionTabs
                .Where(link => link.MenuSectionId == emptyDrinkSectionId)
                .OrderBy(link => link.Tab)
                .Select(link => link.Tab)
                .ToArrayAsync();

            var promotedAssignment = Assert.Single(assignments);
            Assert.Equal(breakfastSectionId, promotedAssignment.MenuSectionId);
            Assert.Equal(9, promotedAssignment.SortOrder);
            Assert.Equal([MenuTab.Breakfast, MenuTab.Lunch], breakfastSectionTabs);
            Assert.Equal([MenuTab.Drinks], emptyDrinkSectionTabs);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
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

    private static async Task<IReadOnlyList<string>> GetColumnNamesAsync(string connectionString, string tableName)
    {
        var columnNames = new List<string>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
            ORDER BY COLUMN_NAME;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnNames.Add(reader.GetString(0));
        }

        return columnNames;
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

