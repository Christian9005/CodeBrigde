using CodeBridge.Transport;
using CodeBridge.Transport.Simulation;

namespace CodeBridge.Core.Tests.Transport;

public class CommandTapTests
{
    [Theory]
    [InlineData("PM:2:1", "OK", 2, "mode", 1)]
    [InlineData("DW:2:1", "OK", 2, "digital", 1)]
    [InlineData("DW:2:0", "OK", 2, "digital", 0)]
    [InlineData("DR:4", "OK:1", 4, "input", 1)]
    [InlineData("AR:34", "OK:2048", 34, "analog", 2048)]
    [InlineData("PW:4:200:5000", "OK", 4, "pwm", 200)]
    [InlineData("SV:13:90", "OK", 13, "servo", 90)]
    [InlineData("SD:13", "OK", 13, "servo-off", 0)]
    [InlineData("TN:15:440:500", "OK", 15, "tone", 440)]
    [InlineData("NT:15", "OK", 15, "tone", 0)]
    public void Pin_commands_become_pin_activity(string command, string response, int pin, string kind, double value)
    {
        var activity = PinActivity.Parse(command, response);

        Assert.Equal(new PinActivity(pin, kind, value), activity);
    }

    [Theory]
    [InlineData("PING", "OK:PONG")]
    [InlineData("VER", "OK:0.9.0")]
    [InlineData("DW:99:1", "ERR:Invalid pin")]
    [InlineData("DW:2", "OK")]
    [InlineData("DW:x:1", "OK")]
    [InlineData("", "OK")]
    [InlineData("OLED:1", "OK")]
    public void Anything_else_is_ignored(string command, string response)
    {
        Assert.Null(PinActivity.Parse(command, response));
    }

    [Fact]
    public async Task The_tap_reports_every_command_with_its_answer_and_changes_nothing()
    {
        var sim = new SimulatedTransport();
        var seen = new List<(string Command, string Response)>();
        var tap = new CommandTapTransport(sim, (command, response) => seen.Add((command, response)));
        await tap.ConnectAsync();

        var answer = await tap.SendCommandAsync("DW:2:1\n");

        Assert.Equal("OK", answer);
        Assert.True(sim.GetOutput(2));
        Assert.Equal(("DW:2:1", "OK"), Assert.Single(seen));
        Assert.True(tap.IsConnected);
    }

    [Fact]
    public async Task A_failing_observer_never_breaks_the_conversation()
    {
        var tap = new CommandTapTransport(new SimulatedTransport(), (_, _) => throw new InvalidOperationException("boom"));
        await tap.ConnectAsync();

        Assert.Equal("OK:PONG", await tap.SendCommandAsync("PING\n"));
    }
}
