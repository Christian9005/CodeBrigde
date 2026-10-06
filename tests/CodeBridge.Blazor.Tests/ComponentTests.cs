using Bunit;
using CodeBridge.Blazor;
using CodeBridge.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBridge.Blazor.Tests;

public sealed class ComponentTests : TestContext
{
    public ComponentTests()
    {
        Services.AddLogging();
        Services.AddCodeBridge(o =>
        {
            o.Port = "simulator";
            o.AutoConnect = false;
        });
    }

    private BoardService Board => Services.GetRequiredService<BoardService>();

    [Fact]
    public async Task The_toggle_drives_the_pin_and_shows_its_state()
    {
        var cut = RenderComponent<PinToggle>(p => p.Add(c => c.Pin, 2).Add(c => c.Label, "Lamp"));
        Assert.Contains("OFF", cut.Markup);

        cut.Find("button.cb-switch").Click();
        cut.WaitForAssertion(() => Assert.Contains("ON", cut.Find(".cb-muted").TextContent));

        Assert.True(Board.Simulator!.GetOutput(2));
    }

    [Fact]
    public async Task An_active_low_toggle_inverts_the_electrical_level()
    {
        var cut = RenderComponent<PinToggle>(p => p.Add(c => c.Pin, 5).Add(c => c.ActiveLow, true));

        cut.Find("button.cb-switch").Click();
        cut.WaitForAssertion(() => Assert.Contains("ON", cut.Find(".cb-muted").TextContent));

        Assert.False(Board.Simulator!.GetOutput(5)); // ON means LOW for a relay wired active-low
    }

    [Fact]
    public async Task The_slider_writes_a_pwm_duty()
    {
        var cut = RenderComponent<PwmSlider>(p => p.Add(c => c.Pin, 4));

        cut.Find("input[type=range]").Change(128);
        cut.WaitForAssertion(() => Assert.Equal(128, Board.Simulator!.GetPwmDuty(4)));
    }

    [Fact]
    public async Task The_gauge_shows_a_scaled_reading_and_stops_polling_when_removed()
    {
        await Board.ReadAnalogAsync(34); // connects
        Board.Simulator!.SetAnalogInput(34, 2048);

        var cut = RenderComponent<AnalogGauge>(p => p.Add(c => c.Pin, 34).Add(c => c.Interval, 50).Add(c => c.ScaledMax, 100).Add(c => c.Unit, "%"));
        cut.WaitForAssertion(() => Assert.Contains("50", cut.Find(".cb-value").TextContent));

        DisposeComponents();
        var logged = Board.Simulator.CommandLog.Count;
        await Task.Delay(250);
        Assert.Equal(logged, Board.Simulator.CommandLog.Count);
    }

    [Fact]
    public async Task The_chart_keeps_a_bounded_history()
    {
        await Board.ReadAnalogAsync(34);
        var cut = RenderComponent<SensorChart>(p => p.Add(c => c.Pin, 34).Add(c => c.Interval, 20).Add(c => c.MaxPoints, 10));

        cut.WaitForAssertion(() => Assert.True(cut.Find("polyline").GetAttribute("points")!.Split(' ').Length >= 5), TimeSpan.FromSeconds(5));
        await Task.Delay(400);

        Assert.True(cut.Find("polyline").GetAttribute("points")!.Split(' ').Length <= 10);
    }

    [Fact]
    public async Task The_status_card_reports_the_connection_and_the_simulator()
    {
        var cut = RenderComponent<BoardStatus>();
        Assert.Contains("Not connected", cut.Markup);

        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("simulator", cut.Markup));
        Assert.Contains("ESP32-SIM", cut.Markup);
    }
}
