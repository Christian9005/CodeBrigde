using CodeBridge.Core.Protocol;

namespace CodeBridge.Core.Tests.Protocol;

public class WifiProtocolCommandTests
{
    // ── WiFi Command Constants ──────────────────────────────

    [Fact]
    public void Constants_WifiCommands_AreCorrect()
    {
        Assert.Equal("WCFG", BridgeProtocol.CMD_WIFI_CONFIG);
        Assert.Equal("WSTAT", BridgeProtocol.CMD_WIFI_STATUS);
        Assert.Equal("WSCAN", BridgeProtocol.CMD_WIFI_SCAN);
    }

    // ── BuildCommand for WiFi ───────────────────────────────

    [Fact]
    public void BuildCommand_WifiConfig_CorrectFormat()
    {
        var result = BridgeProtocol.BuildCommand("WCFG", "MyNetwork", "MyPassword123");
        Assert.Equal("WCFG:MyNetwork:MyPassword123\n", result);
    }

    [Fact]
    public void BuildCommand_WifiStatus_CorrectFormat()
    {
        var result = BridgeProtocol.BuildCommand("WSTAT");
        Assert.Equal("WSTAT\n", result);
    }

    [Fact]
    public void BuildCommand_WifiScan_CorrectFormat()
    {
        var result = BridgeProtocol.BuildCommand("WSCAN");
        Assert.Equal("WSCAN\n", result);
    }

    // ── ParseResponse for WiFi responses ────────────────────

    [Fact]
    public void ParseResponse_WifiConfigSuccess_ReturnsIpAndPort()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:192.168.1.100:8080");
        Assert.True(success);
        Assert.Equal("192.168.1.100:8080", data);
    }

    [Fact]
    public void ParseResponse_WifiConfigFailure_ReturnsError()
    {
        var (success, data) = BridgeProtocol.ParseResponse("ERR:WiFi connection failed");
        Assert.False(success);
        Assert.Equal("WiFi connection failed", data);
    }

    [Fact]
    public void ParseResponse_WifiStatus_ReturnsJson()
    {
        var json = """{"enabled":true,"connected":true,"ssid":"MyNet","ip":"192.168.1.100","rssi":-45,"port":8080}""";
        var (success, data) = BridgeProtocol.ParseResponse($"OK:{json}");
        Assert.True(success);
        Assert.Contains("192.168.1.100", data);
        Assert.Contains("MyNet", data);
    }

    [Fact]
    public void ParseResponse_WifiScan_ReturnsNetworkList()
    {
        var json = """{"networks":[{"ssid":"Net1","rssi":-30,"enc":true},{"ssid":"Net2","rssi":-60,"enc":false}]}""";
        var (success, data) = BridgeProtocol.ParseResponse($"OK:{json}");
        Assert.True(success);
        Assert.Contains("Net1", data);
        Assert.Contains("Net2", data);
    }
}
