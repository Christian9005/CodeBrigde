using CodeBridge.Core.Protocol;
using Xunit;

namespace CodeBridge.Core.Tests.Protocol;

/// <summary>
/// Tests for the new protocol commands (SPI, OneWire, Servo, NeoPixel, etc.)
/// </summary>
public class ExtendedProtocolCommandTests
{
    // ── SPI Commands ─────────────────────────────────────────

    [Fact]
    public void SpiTransfer_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_TRANSFER, 5, "AABB");
        Assert.Equal("ST:5:AABB\n", cmd);
    }

    [Fact]
    public void SpiWrite_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_WRITE, 10, "FF00");
        Assert.Equal("SW:10:FF00\n", cmd);
    }

    [Fact]
    public void SpiRead_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_READ, 5, 4);
        Assert.Equal("SR:5:4\n", cmd);
    }

    [Fact]
    public void SpiConfig_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SPI_CONFIG, 1000000, 0);
        Assert.Equal("SC:1000000:0\n", cmd);
    }

    // ── OneWire Commands ─────────────────────────────────────

    [Fact]
    public void OneWireScan_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_SCAN, 4);
        Assert.Equal("OWS:4\n", cmd);
    }

    [Fact]
    public void OneWireTemp_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, 4);
        Assert.Equal("OWT:4\n", cmd);
    }

    [Fact]
    public void OneWireTemp_With_Address()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OW_TEMP, 4, "28FF1234");
        Assert.Equal("OWT:4:28FF1234\n", cmd);
    }

    // ── Servo Commands ───────────────────────────────────────

    [Fact]
    public void ServoAttach_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_ATTACH, 13);
        Assert.Equal("SA:13\n", cmd);
    }

    [Fact]
    public void ServoWrite_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_WRITE, 13, 90);
        Assert.Equal("SV:13:90\n", cmd);
    }

    [Fact]
    public void ServoRead_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_READ, 13);
        Assert.Equal("SVR:13\n", cmd);
    }

    [Fact]
    public void ServoDetach_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_DETACH, 13);
        Assert.Equal("SD:13\n", cmd);
    }

    [Fact]
    public void ServoPulseWidth_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_SERVO_US, 13, 1500);
        Assert.Equal("SU:13:1500\n", cmd);
    }

    // ── NeoPixel Commands ────────────────────────────────────

    [Fact]
    public void NeoPixelInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_INIT, 16, 30);
        Assert.Equal("NI:16:30\n", cmd);
    }

    [Fact]
    public void NeoPixelSetPixel_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_SET, 0, 255, 0, 128);
        Assert.Equal("NS:0:255:0:128\n", cmd);
    }

    [Fact]
    public void NeoPixelSetAll_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_ALL, 255, 255, 255);
        Assert.Equal("NA:255:255:255\n", cmd);
    }

    [Fact]
    public void NeoPixelShow_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_SHOW);
        Assert.Equal("NH\n", cmd);
    }

    [Fact]
    public void NeoPixelClear_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_CLEAR);
        Assert.Equal("NC\n", cmd);
    }

    [Fact]
    public void NeoPixelBrightness_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NEO_BRIGHT, 128);
        Assert.Equal("NB:128\n", cmd);
    }

    // ── Buzzer/Tone Commands ─────────────────────────────────

    [Fact]
    public void Tone_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_TONE, 25, 1000, 500);
        Assert.Equal("TN:25:1000:500\n", cmd);
    }

    [Fact]
    public void NoTone_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_NO_TONE, 25);
        Assert.Equal("NT:25\n", cmd);
    }

    // ── DHT Sensor Commands ──────────────────────────────────

    [Fact]
    public void DhtRead_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DHT_READ, 4, 22);
        Assert.Equal("DHTR:4:22\n", cmd);
    }

    [Fact]
    public void DhtInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_DHT_INIT, 4, 11);
        Assert.Equal("DHTI:4:11\n", cmd);
    }

    // ── Ultrasonic Commands ──────────────────────────────────

    [Fact]
    public void UltrasonicRead_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_ULTRA_READ, 12, 14);
        Assert.Equal("USR:12:14\n", cmd);
    }

    // ── Motor Commands ───────────────────────────────────────

    [Fact]
    public void MotorInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_INIT, 27, 26, 14);
        Assert.Equal("MI:27:26:14\n", cmd);
    }

    [Fact]
    public void MotorSpeed_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_SPEED, 27, 75);
        Assert.Equal("MS:27:75\n", cmd);
    }

    [Fact]
    public void MotorSpeed_Reverse_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_MOTOR_SPEED, 27, -50);
        Assert.Equal("MS:27:-50\n", cmd);
    }

    [Fact]
    public void StepperInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_STEPPER_INIT, 16, 17, 18, 19, 2048);
        Assert.Equal("STI:16:17:18:19:2048\n", cmd);
    }

    [Fact]
    public void StepperStep_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_STEPPER_STEP, 16, 512, 10);
        Assert.Equal("STS:16:512:10\n", cmd);
    }

    // ── Display Commands ─────────────────────────────────────

    [Fact]
    public void OledInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_INIT, 128, 64, "0x3C");
        Assert.Equal("OI:128:64:0x3C\n", cmd);
    }

    [Fact]
    public void OledText_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_TEXT, 0, 0, 1, "Hello");
        Assert.Equal("OT:0:0:1:Hello\n", cmd);
    }

    [Fact]
    public void OledLine_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_OLED_LINE, 0, 0, 127, 63, 1);
        Assert.Equal("OL:0:0:127:63:1\n", cmd);
    }

    [Fact]
    public void LcdInit_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_INIT, "0x27", 16, 2);
        Assert.Equal("LI:0x27:16:2\n", cmd);
    }

    [Fact]
    public void LcdText_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_TEXT, 0, 0, "Hola Mundo");
        Assert.Equal("LT:0:0:Hola Mundo\n", cmd);
    }

    [Fact]
    public void LcdBacklight_Command_Format()
    {
        var cmd = BridgeProtocol.BuildCommand(BridgeProtocol.CMD_LCD_BACKLIGHT, 1);
        Assert.Equal("LB:1\n", cmd);
    }

    // ── Parse Responses ──────────────────────────────────────

    [Fact]
    public void Parse_DhtResponse_Temp_And_Humidity()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:25.3:62.1");
        Assert.True(success);
        var parts = data.Split(':');
        Assert.Equal("25.3", parts[0]);
        Assert.Equal("62.1", parts[1]);
    }

    [Fact]
    public void Parse_UltrasonicResponse_Distance()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:34.52");
        Assert.True(success);
        Assert.Equal("34.52", data);
    }

    [Fact]
    public void Parse_OneWireScan_MultipleAddresses()
    {
        var (success, data) = BridgeProtocol.ParseResponse("OK:28FF1234,28FF5678");
        Assert.True(success);
        var addrs = data.Split(',');
        Assert.Equal(2, addrs.Length);
    }
}
