using Anchor.Web.Components.Account;
using Anchor.Web.Components.Account.Shared;
using Bunit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Account;

public sealed class PasskeySubmitRenderTests : BunitContext
{
    public PasskeySubmitRenderTests()
    {
        Services.AddSingleton<IAntiforgery>(new StubAntiforgery());
    }

    [Fact]
    public void PasskeySubmit_DefaultsAutoRequestToFalse()
    {
        var cut = Render(CreatePasskeySubmitFragment(autoRequest: null));

        var element = cut.Find("passkey-submit");

        Assert.Equal("false", element.GetAttribute("auto-request"));
    }

    [Fact]
    public void PasskeySubmit_RendersAutoRequestWhenEnabled()
    {
        var cut = Render(CreatePasskeySubmitFragment(autoRequest: true));

        var element = cut.Find("passkey-submit");

        Assert.Equal("true", element.GetAttribute("auto-request"));
    }

    private static RenderFragment CreatePasskeySubmitFragment(bool? autoRequest)
    {
        var httpContext = new DefaultHttpContext();

        return builder =>
        {
            builder.OpenComponent<CascadingValue<HttpContext>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<HttpContext>.Value), httpContext);
            builder.AddAttribute(2, nameof(CascadingValue<HttpContext>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<PasskeySubmit>(0);
                childBuilder.AddAttribute(1, nameof(PasskeySubmit.Operation), PasskeyOperation.Request);
                childBuilder.AddAttribute(2, nameof(PasskeySubmit.Name), "Input.Passkey");
                childBuilder.AddAttribute(3, nameof(PasskeySubmit.EmailName), "Input.Email");

                if (autoRequest.HasValue)
                {
                    childBuilder.AddAttribute(4, nameof(PasskeySubmit.AutoRequest), autoRequest.Value);
                }

                childBuilder.AddAttribute(5, nameof(PasskeySubmit.ChildContent), (RenderFragment)(contentBuilder =>
                {
                    contentBuilder.AddContent(0, "Log in with a passkey");
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private sealed class StubAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) =>
            new("test-request-token", "test-cookie-token", "RequestVerificationToken", "__RequestVerificationToken");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) =>
            new("test-request-token", "test-cookie-token", "RequestVerificationToken", "__RequestVerificationToken");

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) =>
            Task.FromResult(true);

        public Task ValidateRequestAsync(HttpContext httpContext) =>
            Task.CompletedTask;

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
        }
    }
}
