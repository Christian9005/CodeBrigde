using CodeBridge.Core.Protocol;

namespace CodeBridge.Core.Tests.Protocol;

/// <summary>
/// Tests for Phase 3 protocol commands: SPI, OneWire, Servo, NeoPixel, Tone, DHT, Ultrasonic.
/// </summary>
public class Phase3ProtocolCommandTests
{
    // ── SPI Commands ─────────────────────────────────────────

    [Fact]
    public void SPI_Command_Constants_AreCorrect()
    {
        Assert.Equal("ST", BridgeProtocol.CMD_SPI_TRANSFER);
        Assert.Equal("SW", BridgeProtocol.CMD_SPI_WRITE);
        Assert.Equal("SR", BridgeProtocol.CMD_SPI_READ);
        Assert.Equal("SC", BridgeProtocol.CMD_SPI_CONFIG);
    }

    [Fact]
    public void SpiTransfer_Command_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_TRANSFER, 5, "FF00AB");
        Assert.Equal("ST:5:FF00AB\n", cmd);
    }

    [Fact]
    public void SpiConfig_Command_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_CONFIG, 4000000, 2);
        Assert.Equal("SC:4000000:2\n", cmd);
    }

    // ── OneWire Commands ─────────────────────────────────────

    [Fact]
    public void OneWire_Command_Constants_AreCorrect()
    {
        Assert.Equal("OWS", BridgeProtocol.CMD_OW_SCAN);
        Assert.Equal("OWR", BridgeProtocol.CMD_OW_READ);
        Assert.Equal("OWW", BridgeProtocol.CMD_OW_WRITE);
        Assert.Equal("OWT", BridgeProtocol.CMD_OW_TEMP);
    }

    [Fact]
    public void OneWireScan_Command_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_SCAN, 4);
        Assert.Equal("OWS:4\n", cmd);
    }

    [Fact]
    public void OneWireTemp_Command_WithAddress_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, 4, "28FF12345678AB");
        Assert.Equal("OWT:4:28FF12345678AB\n", cmd);
    }

    // ── Servo Commands ───────────────────────────────────────

    [Fact]
    public void Servo_Command_Constants_AreCorrect()
    {
        Assert.Equal("SA", BridgeProtocol.CMD_SERVO_ATTACH);
        Assert.Equal("SV", BridgeProtocol.CMD_SERVO_WRITE);
        Assert.Equal("SVR", BridgeProtocol.CMD_SERVO_READ);
        Assert.Equal("SU", BridgeProtocol.CMD_SERVO_US);
        Assert.Equal("SD", BridgeProtocol.CMD_SERVO_DETACH);
    }

    [Fact]
    public void ServoAttach_WithMinMax_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_ATTACH, 13, 500, 2500);
        Assert.Equal("SA:13:500:2500\n", cmd);
    }

    [Fact]
    public void ServoWrite_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_WRITE, 13, 90);
        Assert.Equal("SV:13:90\n", cmd);
    }

    // ── NeoPixel Commands ────────────────────────────────────

    [Fact]
    public void NeoPixel_Command_Constants_AreCorrect()
    {
        Assert.Equal("NI", BridgeProtocol.CMD_NEO_INIT);
        Assert.Equal("NS", BridgeProtocol.CMD_NEO_SET);
        Assert.Equal("NA", BridgeProtocol.CMD_NEO_ALL);
        Assert.Equal("NH", BridgeProtocol.CMD_NEO_SHOW);
        Assert.Equal("NC", BridgeProtocol.CMD_NEO_CLEAR);
        Assert.Equal("NB", BridgeProtocol.CMD_NEO_BRIGHT);
        Assert.Equal("NR", BridgeProtocol.CMD_NEO_RANGE);
    }

    [Fact]
    public void NeoInit_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_INIT, 16, 30);
        Assert.Equal("NI:16:30\n", cmd);
    }

    [Fact]
    public void NeoSetPixel_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_SET, 0, 255, 0, 128);
        Assert.Equal("NS:0:255:0:128\n", cmd);
    }

    // ── Tone Commands ────────────────────────────────────────

    [Fact]
    public void Tone_Command_Constants_AreCorrect()
    {
        Assert.Equal("TN", BridgeProtocol.CMD_TONE);
        Assert.Equal("NT", BridgeProtocol.CMD_NO_TONE);
    }

    [Fact]
    public void Tone_Command_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_TONE, 25, 1000, 500);
        Assert.Equal("TN:25:1000:500\n", cmd);
    }

    // ── DHT Commands ─────────────────────────────────────────

    [Fact]
    public void DHT_Command_Constants_AreCorrect()
    {
        Assert.Equal("DHTR", BridgeProtocol.CMD_DHT_READ);
        Assert.Equal("DHTI", BridgeProtocol.CMD_DHT_INIT);
    }

    [Fact]
    public void DhtRead_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DHT_READ, 4, 22);
        Assert.Equal("DHTR:4:22\n", cmd);
    }

    // ── Ultrasonic Commands ──────────────────────────────────

    [Fact]
    public void Ultrasonic_Command_Constant_IsCorrect()
    {
        Assert.Equal("USR", BridgeProtocol.CMD_ULTRA_READ);
    }

    [Fact]
    public void UltrasonicRead_Formats_Correctly()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_ULTRA_READ, 5, 18);
        Assert.Equal("USR:5:18\n", cmd);
    }

    // ── Motor Commands ───────────────────────────────────────

    [Fact]
    public void Motor_Command_Constants_AreCorrect()
    {
        Assert.Equal("MI", BridgeProtocol.CMD_MOTOR_INIT);
        Assert.Equal("MS", BridgeProtocol.CMD_MOTOR_SPEED);
        Assert.Equal("MX", BridgeProtocol.CMD_MOTOR_STOP);
        Assert.Equal("STI", BridgeProtocol.CMD_STEPPER_INIT);
        Assert.Equal("STS", BridgeProtocol.CMD_STEPPER_STEP);
    }

    // ── Display Commands ─────────────────────────────────────

    [Fact]
    public void OLED_Command_Constants_AreCorrect()
    {
        Assert.Equal("OI", BridgeProtocol.CMD_OLED_INIT);
        Assert.Equal("OC", BridgeProtocol.CMD_OLED_CLEAR);
        Assert.Equal("OT", BridgeProtocol.CMD_OLED_TEXT);
        Assert.Equal("OP", BridgeProtocol.CMD_OLED_PIXEL);
        Assert.Equal("OL", BridgeProtocol.CMD_OLED_LINE);
        Assert.Equal("OR", BridgeProtocol.CMD_OLED_RECT);
        Assert.Equal("OE", BridgeProtocol.CMD_OLED_CIRCLE);
        Assert.Equal("OF", BridgeProtocol.CMD_OLED_FLUSH);
        Assert.Equal("OB", BridgeProtocol.CMD_OLED_BRIGHT);
    }

    [Fact]
    public void LCD_Command_Constants_AreCorrect()
    {
        Assert.Equal("LI", BridgeProtocol.CMD_LCD_INIT);
        Assert.Equal("LC", BridgeProtocol.CMD_LCD_CLEAR);
        Assert.Equal("LT", BridgeProtocol.CMD_LCD_TEXT);
        Assert.Equal("LB", BridgeProtocol.CMD_LCD_BACKLIGHT);
        Assert.Equal("LK", BridgeProtocol.CMD_LCD_CURSOR);
    }
}
