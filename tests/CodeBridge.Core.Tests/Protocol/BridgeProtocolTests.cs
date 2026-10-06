using CodeBridge.Core.Protocol;

namespace CodeBridge.Core.Tests.Protocol;

public class BridgeProtocolTests
{
    // ── BuildCommand Tests ──────────────────────────────────

    [Fact]
    public void BuildCommand_NoArgs_ReturnsCommandWithTerminator()
    {
        var result = BridgeProtocol.BuildCommand("PING");
        Assert.Equal("PING\n", result);
    }

    [Fact]
    public void BuildCommand_SingleArg_ReturnsFormattedCommand()
    {
        var result = BridgeProtocol.BuildCommand("DR", 2);
        Assert.Equal("DR:2\n", result);
    }

    [Fact]
    public void BuildCommand_MultipleArgs_JoinedWithSeparator()
    {
        var result = BridgeProtocol.BuildCommand("DW", 2, 1);
        Assert.Equal("DW:2:1\n", result);
    }

    [Fact]
    public void BuildCommand_ThreeArgs_PwmFormat()
    {
        var result = BridgeProtocol.BuildCommand("PW", 5, 128, 5000);
        Assert.Equal("PW:5:128:5000\n", result);
    }

    [Fact]
    public void BuildCommand_StringArg_IncludedCorrectly()
    {
        var result = BridgeProtocol.BuildCommand("IW", 104, "FF01");
        Assert.Equal("IW:104:FF01\n", result);
    }

    [Theory]
    [InlineData("PM", new object[] { 2, 1 }, "PM:2:1\n")]
    [InlineData("DW", new object[] { 13, 0 }, "DW:13:0\n")]
    [InlineData("AR", new object[] { 34 }, "AR:34\n")]
    [InlineData("VER", new object[] { }, "VER\n")]
    public void BuildCommand_VariousCommands_CorrectFormat(string cmd, object[] args, string expected)
    {
        var result = BridgeProtocol.BuildCommand(cmd, args);
        Assert.Equal(expected, result);
    }

    // ── ParseResponse Tests ─────────────────────────────────

    [Fact]
    public void ParseResponse_OkNoData_ReturnsSuccessEmptyData()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK");
        Assert.True(success);
        Assert.Equal("", data);
    }

    [Fact]
    public void ParseResponse_OkWithData_ReturnsSuccessAndData()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:PONG");
        Assert.True(success);
        Assert.Equal("PONG", data);
    }

    [Fact]
    public void ParseResponse_OkWithNumericData_ReturnsValue()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:1");
        Assert.True(success);
        Assert.Equal("1", data);
    }

    [Fact]
    public void ParseResponse_OkWithJsonData_ReturnsFullJson()
    {
        var json = "{\"chip\":\"ESP32\",\"freq\":240}";
        var (success, data) = BridgeProtocol.ParseResponse($"OK:{json}");
        Assert.True(success);
        Assert.Equal(json, data);
    }

    [Fact]
    public void ParseResponse_OkWithCommaList_ReturnsFullList()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:60,104,112");
        Assert.True(success);
        Assert.Equal("60,104,112", data);
    }

    [Fact]
    public void ParseResponse_Error_ReturnsFailureWithMessage()
    {
        var (success, data) = BridgeProtocol.ParseResponse("ERR:Invalid pin");
        Assert.False(success);
        Assert.Equal("Invalid pin", data);
    }

    [Fact]
    public void ParseResponse_ErrorNoMessage_ReturnsUnknownError()
    {
        var (success, data) = BridgeProtocol.ParseResponse("ERR");
        Assert.False(success);
        Assert.Equal("Unknown error", data);
    }

    [Fact]
    public void ParseResponse_InvalidResponse_ReturnsFailure()
    {
        var (success, data) = BridgeProtocol.ParseResponse("GARBAGE");
        Assert.False(success);
        Assert.Contains("Invalid response", data);
    }

    [Fact]
    public void ParseResponse_WithWhitespace_TrimsCorrectly()
    {
        var (success, data) = BridgeProtocol.ParseResponse("  OK:PONG  \n");
        Assert.True(success);
        Assert.Equal("PONG", data);
    }

    [Fact]
    public void ParseResponse_EmptyOkData_ReturnsEmptyString()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:");
        Assert.True(success);
        Assert.Equal("", data);
    }

    // ── Constants Tests ─────────────────────────────────────

    [Fact]
    public void Constants_CommandNames_AreCorrect()
    {
        Assert.Equal("0.9.0", BridgeProtocol.EXPECTED_FIRMWARE_VERSION);
        Assert.Equal("PM", BridgeProtocol.CMD_PIN_MODE);
        Assert.Equal("DW", BridgeProtocol.CMD_DIGITAL_WRITE);
        Assert.Equal("DR", BridgeProtocol.CMD_DIGITAL_READ);
        Assert.Equal("AR", BridgeProtocol.CMD_ANALOG_READ);
        Assert.Equal("PW", BridgeProtocol.CMD_PWM_WRITE);
        Assert.Equal("IS", BridgeProtocol.CMD_I2C_SCAN);
        Assert.Equal("IW", BridgeProtocol.CMD_I2C_WRITE);
        Assert.Equal("IR", BridgeProtocol.CMD_I2C_READ);
        Assert.Equal("IWR", BridgeProtocol.CMD_I2C_WREG);
        Assert.Equal("IRR", BridgeProtocol.CMD_I2C_RREG);
        Assert.Equal("PING", BridgeProtocol.CMD_PING);
        Assert.Equal("INFO", BridgeProtocol.CMD_INFO);
        Assert.Equal("RST", BridgeProtocol.CMD_RESET);
        Assert.Equal("VER", BridgeProtocol.CMD_VERSION);
    }

    [Fact]
    public void Constants_Separator_IsColon()
    {
        Assert.Equal(':', BridgeProtocol.SEPARATOR);
    }

    [Fact]
    public void Constants_Terminator_IsNewline()
    {
        Assert.Equal("\n", BridgeProtocol.TERMINATOR);
    }
}
