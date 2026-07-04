using CodeBridge.Core;
using CodeBridge.Core.Abstractions;
using CodeBridge.Core.Enums;
using CodeBridge.ESP32;
using CodeBridge.Flow;
using CodeBridge.Transport.Serial;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class BoardConnectionService
{
    public static async Task<IBoard> ConnectAsync(
        BoardConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TransportMode == CodeBridgeTransportMode.Serial)
            return await ConnectSerialAsync(request, cancellationToken);

        if (!IsEsp32Profile(request.BoardProfile))
            throw new InvalidOperationException($"{request.BoardProfile.DisplayName} currently supports CodeBridge over USB Serial. Switch Mode to Serial, upload firmware, then connect.");

        if (string.IsNullOrWhiteSpace(request.Host))
            throw new InvalidOperationException("Enter the ESP32 WiFi host before connecting.");

        return await CodeBridgeBuilder
            .Connect()
            .WiFi(request.Host.Trim(), request.TcpPort)
            .ToESP32()
            .BuildAsync(cancellationToken);
    }

    public static string FormatConnectionFailure(Exception ex, BoardProfile boardProfile, string? portName)
    {
        if (ex is TimeoutException || ex.Message.Contains("CODEBRIDGE_READY", StringComparison.OrdinalIgnoreCase))
        {
            var port = string.IsNullOrWhiteSpace(portName) ? "the selected port" : portName;
            return string.Join(
                Environment.NewLine,
                $"{boardProfile.DisplayName} was found on {port}, but it is not running CodeBridge firmware yet.",
                "",
                "Use Upload FW to flash the matching firmware, then press Connect again.",
                "",
                $"Details: {ex.Message}");
        }

        return ex.Message;
    }

    public static async Task DisposeBoardAsync(IBoard board)
    {
        if (board is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            board.Dispose();
    }

    public static bool IsEsp32Profile(BoardProfile boardProfile) =>
        string.Equals(boardProfile.Id, BuiltInBoardProfiles.Esp32DevKit.Id, StringComparison.OrdinalIgnoreCase);

    private static async Task<IBoard> ConnectSerialAsync(
        BoardConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var portName = request.PortName.Trim();
        if (string.IsNullOrWhiteSpace(portName))
            throw new InvalidOperationException("Select a serial port before connecting.");

        if (IsEsp32Profile(request.BoardProfile))
        {
            return await CodeBridgeBuilder
                .Connect()
                .Serial(portName, request.BaudRate)
                .ToESP32()
                .BuildAsync(cancellationToken);
        }

        var transport = new SerialTransport(portName, request.BaudRate);
        var board = new CodeBridgeProtocolBoard(
            transport,
            request.BoardProfile.DisplayName,
            GetBoardFamily(request.BoardProfile));

        try
        {
            await board.ConnectAsync(cancellationToken);
            return board;
        }
        catch
        {
            await board.DisposeAsync();
            throw;
        }
    }

    private static BoardFamily GetBoardFamily(BoardProfile boardProfile) =>
        string.Equals(boardProfile.Id, BuiltInBoardProfiles.ArduinoUno.Id, StringComparison.OrdinalIgnoreCase)
            ? BoardFamily.Arduino
            : BoardFamily.ESP32;
}
