using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Anchor.Web.Components.Shared;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Anchor.Web.Tests.Components.Shared;

public sealed class CommitAwareTextEntryTests : BunitContext
{
    [Fact]
    public void Text_input_updates_the_bound_value_from_live_and_committed_text()
    {
        var model = new TextEntryModel();
        var cut = RenderTextInput(model);
        var input = cut.Find("input");

        input.Input("Typed value");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Typed value", model.Value);
            Assert.Equal("Typed value", input.GetAttribute("value"));
        });

        input.Change("Voice committed value");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Voice committed value", model.Value);
            Assert.Equal("Voice committed value", input.GetAttribute("value"));
        });
    }

    [Fact]
    public void Text_area_updates_the_bound_value_from_live_and_committed_text()
    {
        var model = new TextEntryModel();
        var cut = RenderTextArea(model);
        var textArea = cut.Find("textarea");

        textArea.Input("Typed paragraph");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Typed paragraph", model.Value);
            Assert.Equal("Typed paragraph", textArea.GetAttribute("value"));
        });

        textArea.Change("Voice committed paragraph");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Voice committed paragraph", model.Value);
            Assert.Equal("Voice committed paragraph", textArea.GetAttribute("value"));
        });
    }

    [Fact]
    public void Text_input_inherits_invalid_class_from_edit_context()
    {
        var model = new RequiredTextEntryModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);

        var cut = Render(builder =>
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddAttribute(1, nameof(EditForm.EditContext), editContext);
            builder.AddAttribute(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => childBuilder =>
            {
                childBuilder.OpenComponent<DataAnnotationsValidator>(0);
                childBuilder.CloseComponent();
                childBuilder.OpenComponent<CommitAwareInputText>(1);
                childBuilder.AddAttribute(2, nameof(CommitAwareInputText.Value), model.Value);
                childBuilder.AddAttribute(3, nameof(CommitAwareInputText.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                childBuilder.AddAttribute(4, nameof(CommitAwareInputText.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.InvokeAsync(() =>
        {
            editContext.NotifyFieldChanged(fieldIdentifier);
            editContext.Validate();
        });

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("input");
            Assert.Contains("invalid", input.ClassName, StringComparison.Ordinal);
            Assert.Equal("true", input.GetAttribute("aria-invalid"));
        });
    }

    [Fact]
    public void Text_area_inherits_invalid_class_from_edit_context()
    {
        var model = new RequiredTextEntryModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = FieldIdentifier.Create(() => model.Value);

        var cut = Render(builder =>
        {
            builder.OpenComponent<EditForm>(0);
            builder.AddAttribute(1, nameof(EditForm.EditContext), editContext);
            builder.AddAttribute(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => childBuilder =>
            {
                childBuilder.OpenComponent<DataAnnotationsValidator>(0);
                childBuilder.CloseComponent();
                childBuilder.OpenComponent<CommitAwareTextArea>(1);
                childBuilder.AddAttribute(2, nameof(CommitAwareTextArea.Value), model.Value);
                childBuilder.AddAttribute(3, nameof(CommitAwareTextArea.ValueChanged), EventCallback.Factory.Create<string?>(this, value => model.Value = value));
                childBuilder.AddAttribute(4, nameof(CommitAwareTextArea.ValueExpression), (Expression<Func<string?>>)(() => model.Value));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.InvokeAsync(() =>
        {
            editContext.NotifyFieldChanged(fieldIdentifier);
            editContext.Validate();
        });

        cut.WaitForAssertion(() =>
        {
            var textArea = cut.Find("textarea");
            Assert.Contains("invalid", textArea.ClassName, StringComparison.Ordinal);
            Assert.Equal("true", textArea.GetAttribute("aria-invalid"));
        });
    }

    private IRenderedComponent<CommitAwareInputText> RenderTextInput(TextEntryModel model) =>
        Render<CommitAwareInputText>(parameters => parameters
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, value => model.Value = value)
            .Add(component => component.ValueExpression, () => model.Value));

    private IRenderedComponent<CommitAwareTextArea> RenderTextArea(TextEntryModel model) =>
        Render<CommitAwareTextArea>(parameters => parameters
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, value => model.Value = value)
            .Add(component => component.ValueExpression, () => model.Value));

    private sealed class TextEntryModel
    {
        public string? Value { get; set; }
    }

    private sealed class RequiredTextEntryModel
    {
        [Required]
        public string? Value { get; set; }
    }
}
