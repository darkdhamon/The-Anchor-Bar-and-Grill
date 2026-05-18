using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Anchor.Infrastructure.Data;

namespace Anchor.Infrastructure.Tests.Support;

internal sealed class SqliteIdentityTestContext : IAsyncDisposable
{
    private readonly ServiceProvider rootProvider;
    private readonly AsyncServiceScope scope;

    private SqliteIdentityTestContext(SqliteConnection connection, ServiceProvider rootProvider, AsyncServiceScope scope)
    {
        Connection = connection;
        this.rootProvider = rootProvider;
        this.scope = scope;
    }

    public SqliteConnection Connection { get; }

    public ApplicationDbContext DbContext => scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    public UserManager<ApplicationUser> UserManager => scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    public RoleManager<IdentityRole> RoleManager => scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    public static async Task<SqliteIdentityTestContext> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.SignIn.RequireConfirmedAccount = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var rootProvider = services.BuildServiceProvider();
        var scope = rootProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        return new SqliteIdentityTestContext(connection, rootProvider, scope);
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await rootProvider.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

