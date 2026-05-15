using Anchor.Domain.Identity.Users;
using Anchor.Web.Components.Pages.Admin;
using Bunit;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class ManagedUserTemporaryPasswordEditorTests : BunitContext
{
    [Fact]
    public void Submit_passes_the_selected_user_id_and_new_temporary_password()
    {
        ResetManagedUserPasswordRequest? capturedRequest = null;

        var cut = Render<ManagedUserTemporaryPasswordEditor>(parameters => parameters
            .Add(component => component.User, BuildUser("user-1"))
            .Add(component => component.IsBusy, false)
            .Add(component => component.OnSave, request => capturedRequest = request));

        cut.Find("input[type='password']").Input("TempPass9!");
        cut.Find("form").Submit();

        Assert.NotNull(capturedRequest);
        Assert.Equal("user-1", capturedRequest.UserId);
        Assert.Equal("TempPass9!", capturedRequest.TemporaryPassword);
    }

    [Fact]
    public void Busy_state_disables_submit_button()
    {
        var cut = Render<ManagedUserTemporaryPasswordEditor>(parameters => parameters
            .Add(component => component.User, BuildUser("user-1"))
            .Add(component => component.IsBusy, true)
            .Add(component => component.OnSave, _ => { }));

        Assert.True(cut.Find("button[type='submit']").HasAttribute("disabled"));
    }

    private static ManagedUserSummary BuildUser(string userId) =>
        new(
            UserId: userId,
            Email: $"{userId}@anchor.test",
            FirstName: null,
            LastName: null,
            PhoneNumber: null,
            AccountConfirmed: true,
            EmailConfirmed: true,
            MustChangePassword: false,
            IsBootstrapAccount: false,
            Roles: []);
}
