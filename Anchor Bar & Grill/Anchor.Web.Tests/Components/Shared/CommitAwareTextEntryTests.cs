using Anchor.Web.Components.Shared;
using Bunit;

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
}
