using System.IO;
using System.Threading;
using CodeBridge.VisualStudio.Editor;

namespace CodeBridge.VisualStudio.Tests;

public sealed class WifiSetupTests
{
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); } catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException(failure.ToString());
    }

    [Fact]
    public void A_pairing_token_round_trips_and_is_never_stored_in_clear_text()
    {
        const string host = "board.codebridge-tests.invalid";
        const string token = "0123456789abcdef0123456789abcdef";
        try
        {
            BoardTokens.Set(host, token);

            Assert.Equal(token, BoardTokens.Get(host));
            Assert.Equal(token, BoardTokens.Get("  BOARD.codebridge-tests.INVALID "));

            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeBridge", "board-tokens.json");
            Assert.DoesNotContain(token, File.ReadAllText(file));
        }
        finally
        {
            BoardTokens.Remove(host);
        }

        Assert.Null(BoardTokens.Get(host));
    }

    [Theory]
    [InlineData("COM3", true)]
    [InlineData("com12", true)]
    [InlineData("/dev/ttyUSB0", true)]
    [InlineData("192.168.1.50", false)]
    [InlineData("esp32.local", false)]
    public void Serial_ports_are_told_apart_from_Wi_Fi_hosts(string target, bool serial)
    {
        Assert.Equal(serial, BoardTokens.IsSerialPort(target));
    }

    [Fact]
    public void The_Wi_Fi_window_builds_with_the_editor_theme()
    {
        RunSta(() =>
        {
            var window = new WifiSetupWindow("COM9");

            Assert.NotNull(window.Content);
            Assert.True(window.Resources.Contains("VsToolWindowText"));
            Assert.Contains("Wi-Fi", window.Title);
        });
    }
}
