using Anchor.Infrastructure;
using Anchor.Infrastructure.Data;
using Anchor.Domain;
using Anchor.Domain.Identity;
using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Configuration;
using Anchor.Domain.Identity.Users;
using Anchor.Web.Components;
using Anchor.Web.Components.Account;
using Anchor.Web.Configuration;
using Anchor.Web.Data;
using Anchor.Web.Images;
using Anchor.Web.Issues;
using Anchor.Web.Time;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IClaimsTransformation, ApplicationUserClaimsRefreshTransformation>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorizationBuilder()
    .AddAnchorAuthorizationPolicies();

builder.Services.Configure<RestaurantTimeOptions>(builder.Configuration.GetSection(RestaurantTimeOptions.SectionName));
var restaurantTimeZoneId = builder.Configuration[$"{RestaurantTimeOptions.SectionName}:{nameof(RestaurantTimeOptions.TimeZoneId)}"];
builder.Services.AddSingleton<TimeProvider>(_ => RestaurantTimeProvider.CreateSystemClock(restaurantTimeZoneId));
builder.Services.Configure<AnchorIdentityOptions>(builder.Configuration.GetSection(AnchorIdentityConfigurationKeys.SectionName));
builder.Services.AddAnchorDomainServices();
builder.Services.AddGitHubIssueServices(builder.Configuration);
builder.Services.AddProductionExceptionIssueReporting(builder.Configuration);
builder.Services.AddScoped<IConfirmedAccountConfigurationStore, JsonConfirmedAccountConfigurationStore>();
builder.Services.AddScoped<IMenuItemImageStorage, LocalMenuItemImageStorage>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAnchorInfrastructure(connectionString);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var bootstrapService = scope.ServiceProvider.GetRequiredService<IIdentityBootstrapService>();
    await bootstrapService.BootstrapAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseMiddleware<ProductionExceptionIssueMiddleware>();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPublicEventEndpoints();
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program
{
}
