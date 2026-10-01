using CodeBridge.Core.Enums;
using CodeBridge.Core.Protocol;
using CodeBridge.Core.Tests.Mocks;
using CodeBridge.ESP32;

namespace CodeBridge.Core.Tests.ESP32;

public class ArduinoFirmwareCompatibilityTests
{
    [Fact]
    public async Task ConnectAsync_AcceptsTheVersionShippedByTheArduinoUnoFirmware()
    {
        var expectedMajorMinor = string.Join('.', BridgeProtocol.EXPECTED_FIRMWARE_VERSION.Split('.').Take(2));
        var source = File.ReadAllText(FindRepoFile("firmware", "arduino-uno-bridge", "src", "main.cpp"));
        var shipped = System.Text.RegularExpressions.Regex.Match(source, "CODEBRIDGE_FIRMWARE_VERSION \"([^\"]+)\"").Groups[1].Value;
        Assert.StartsWith(expectedMajorMinor + ".", shipped);

        var transport = new MockTransport();
        transport.EnqueueResponse("OK:PONG");
        transport.EnqueueResponse($"OK:{shipped}");

        var board = new CodeBridgeProtocolBoard(transport, "Arduino Uno", BoardFamily.Arduino);
        await board.ConnectAsync();

        Assert.Equal(shipped, board.FirmwareVersion);
    }

    private static string FindRepoFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException(string.Join('/', parts));
    }
}

public class BuilderFamilyGuardTests
{
    [Fact]
    public void Build_ForNonEsp32Family_ThrowsInsteadOfReturningAnEsp32Board()
    {
        var builder = CodeBridge.Core.CodeBridgeBuilder.Connect().Serial("COM1").ToSTM32();

        Assert.Throws<NotSupportedException>(() => builder.Build());
    }

    [Fact]
    public void Build_ForEsp32Family_ReturnsBoard()
    {
        var board = CodeBridge.Core.CodeBridgeBuilder.Connect().Serial("COM1").ToESP32().Build();

        Assert.Equal(BoardFamily.ESP32, board.Family);
    }
}
