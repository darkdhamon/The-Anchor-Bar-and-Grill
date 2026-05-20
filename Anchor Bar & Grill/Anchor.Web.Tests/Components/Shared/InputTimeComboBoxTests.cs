using Anchor.Web.Components.Shared;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Anchor.Web.Tests.Components.Shared;

public sealed class InputTimeComboBoxTests : BunitContext
{
    public InputTimeComboBoxTests()
    {
        var timeComboBoxModule = JSInterop.SetupModule("./Components/Shared/InputTimeComboBox.razor.js");
        timeComboBoxModule.SetupVoid("scrollRelevantOption", _ => true);
    }

    [Fact]
    public void Initial_twenty_four_hour_value_is_rendered_in_twelve_hour_display()
    {
        var model = new TimeInputModel
        {
            Value = "16:00"
        };

        var cut = RenderTimeComboBox(model);

        Assert.Equal("4:00 PM", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Focused_combobox_keeps_the_full_option_list_and_marks_the_nearest_time()
    {
        var model = new TimeInputModel
        {
            Value = "12:50 PM"
        };

        var cut = RenderTimeComboBox(model);
        cut.Find("input").Focus();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(48, cut.FindAll(".time-combobox__option").Count);

            var nearestOption = cut.FindAll(".time-combobox__option")
                .Single(option => string.Equals(option.TextContent.Trim(), "1:00 PM", StringComparison.Ordinal));

            Assert.Contains("time-combobox__option--nearest", nearestOption.ClassList);
        });
    }

    [Fact]
    public void Selecting_an_option_updates_the_bound_value()
    {
        var model = new TimeInputModel();

        var cut = RenderTimeComboBox(model);
        cut.Find("input").Focus();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".time-combobox__option")));

        cut.FindAll(".time-combobox__option")
            .Single(option => string.Equals(option.TextContent.Trim(), "11:30 AM", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("11:30 AM", model.Value);
            Assert.Equal("11:30 AM", cut.Find("input").GetAttribute("value"));
        });
    }

    [Fact]
    public void Blur_normalizes_shorthand_input()
    {
        var model = new TimeInputModel();
        var cut = RenderTimeComboBox(model);
        var input = cut.Find("input");

        input.Input("1300");
        input.TriggerEvent("onblur", new FocusEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1:00 PM", model.Value);
            Assert.Equal("1:00 PM", input.GetAttribute("value"));
        });
    }

    [Fact]
    public void Clearing_a_focused_value_keeps_the_input_blank_until_commit()
    {
        var model = new TimeInputModel
        {
            Value = "11:00 AM"
        };

        var cut = RenderTimeComboBox(model);
        var input = cut.Find("input");

        input.Focus();
        input.Input(string.Empty);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, model.Value);
            Assert.Equal(string.Empty, input.GetAttribute("value"));
        });
    }

    [Fact]
    public void Change_then_blur_normalizes_committed_time_input()
    {
        var model = new TimeInputModel();
        var cut = RenderTimeComboBox(model);
        var input = cut.Find("input");

        input.Change("1300");
        input.TriggerEvent("onblur", new FocusEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1:00 PM", model.Value);
            Assert.Equal("1:00 PM", input.GetAttribute("value"));
        });
    }

    private IRenderedComponent<InputTimeComboBox> RenderTimeComboBox(TimeInputModel model)
    {
        return Render<InputTimeComboBox>(parameters => parameters
            .Add(component => component.Value, model.Value)
            .Add(component => component.ValueChanged, value => model.Value = value)
            .Add(component => component.ValueExpression, () => model.Value));
    }

    private sealed class TimeInputModel
    {
        public string? Value { get; set; }
    }
}
