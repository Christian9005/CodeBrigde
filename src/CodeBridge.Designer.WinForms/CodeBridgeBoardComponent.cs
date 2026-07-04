using System.ComponentModel;
using System.Drawing;
using CodeBridge.Core.Abstractions;
using CodeBridge.Designer.WinForms.Hardware;
using CodeBridge.Flow;
using CodeBridge.Flow.Execution;
using CodeBridge.Transport;

namespace CodeBridge.Designer.WinForms;

[ToolboxItem(true)]
[ToolboxBitmap(typeof(CodeBridgeBoardComponent), "CodeBridge.Designer.WinForms.Resources.CodeBridgeEsp32Component.bmp")]
[Designer(typeof(CodeBridgeBoardComponentDesigner))]
[DesignerCategory("Component")]
[DisplayName("CodeBridge Board")]
[Description("Owns CodeBridge hardware onboarding, firmware upload, and board connectivity for WinForms flows.")]
[DefaultProperty(nameof(BoardId))]
[DefaultEvent(nameof(Connected))]
public sealed class CodeBridgeBoardComponent : Component, IAsyncDisposable
{
    private IBoard? _board;
    private string _boardId = BuiltInBoardProfiles.Default.Id;
    private bool _autoDetectBoard = true;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<Exception>? ConnectionFailed;
    public event EventHandler<FirmwareUploadResult>? FirmwareUploaded;
    public event EventHandler<FirmwareUploadResult>? FirmwareUploadFailed;

    [Category("CodeBridge")]
    [DefaultValue("esp32-devkit")]
    [TypeConverter(typeof(BoardProfileIdTypeConverter))]
    [Description("Target board profile used for pin filtering, validation, firmware upload, and connection behavior.")]
    public string BoardId
    {
        get => _boardId;
        set => _boardId = BuiltInBoardProfiles.FindById(value)?.Id ?? BuiltInBoardProfiles.Default.Id;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BoardName => CurrentBoard.DisplayName;

    [Category("CodeBridge")]
    [DefaultValue(true)]
    [Description("Automatically switches the board profile when the selected serial port can be recognized as ESP32 or Arduino.")]
    public bool AutoDetectBoard
    {
        get => _autoDetectBoard;
        set => _autoDetectBoard = value;
    }

    [Category("CodeBridge")]
    [DefaultValue(CodeBridgeTransportMode.Serial)]
    public CodeBridgeTransportMode TransportMode { get; set; } = CodeBridgeTransportMode.Serial;

    [Category("CodeBridge")]
    [DefaultValue("")]
    [Description("Serial port used when TransportMode is Serial, for example COM3.")]
    public string PortName { get; set; } = string.Empty;

    [Category("CodeBridge")]
    [DefaultValue(115200)]
    public int BaudRate { get; set; } = 115200;

    [Category("CodeBridge")]
    [DefaultValue("192.168.1.100")]
    [Description("ESP32 host used when TransportMode is WiFi.")]
    public string Host { get; set; } = "192.168.1.100";

    [Category("CodeBridge")]
    [DefaultValue(8080)]
    [Description("ESP32 TCP port used when TransportMode is WiFi.")]
    public int TcpPort { get; set; } = 8080;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IBoard? Board => _board;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BoardProfile CurrentBoard => BuiltInBoardProfiles.FindById(BoardId) ?? BuiltInBoardProfiles.Default;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsConnected => _board is { IsConnected: true };

    public void DetectFirstSerialPort()
    {
        var port = BoardDiscovery.DiscoverPorts().FirstOrDefault();
        if (port is null)
            return;

        TransportMode = CodeBridgeTransportMode.Serial;
        PortName = port.Name;

        var suggestedBoard = BoardPortDetector.SuggestBoardForPort(port.Name);
        if (suggestedBoard is not null)
            BoardId = suggestedBoard.Id;
    }

    public async Task<IBoard> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_board is { IsConnected: true })
            return _board;

        try
        {
            var boardProfile = ResolveBoardForCurrentPort();
            _board = await BoardConnectionService.ConnectAsync(
                new BoardConnectionRequest(
                    boardProfile,
                    TransportMode,
                    PortName,
                    BaudRate,
                    Host,
                    TcpPort),
                cancellationToken);

            Connected?.Invoke(this, EventArgs.Empty);
            return _board;
        }
        catch (Exception ex)
        {
            ConnectionFailed?.Invoke(this, ex);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_board is null)
            return;

        await BoardConnectionService.DisposeBoardAsync(_board);
        _board = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public async Task<FirmwareUploadResult> UploadFirmwareAsync(CancellationToken cancellationToken = default)
    {
        var uploader = FirmwareUploaderFactory.Create(new FirmwareUploadRequest(ResolveBoardForCurrentPort(), PortName));
        var result = await uploader.UploadAsync(cancellationToken);

        if (result.Success)
            FirmwareUploaded?.Invoke(this, result);
        else
            FirmwareUploadFailed?.Invoke(this, result);

        return result;
    }

    public async Task<FlowExecutionResult> RunAsync(
        CodeBridgeFlowControl flow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flow);
        var board = await ConnectAsync(cancellationToken);
        return await flow.RunAsync(board, cancellationToken);
    }

    private BoardProfile ResolveBoardForCurrentPort()
    {
        if (!AutoDetectBoard || TransportMode != CodeBridgeTransportMode.Serial)
            return CurrentBoard;

        var suggestedBoard = BoardPortDetector.SuggestBoardForPort(PortName);
        if (suggestedBoard is null)
            return CurrentBoard;

        BoardId = suggestedBoard.Id;
        return suggestedBoard;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _board?.Dispose();
            _board = null;
        }

        base.Dispose(disposing);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}

public sealed class BoardProfileIdTypeConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) =>
        new(BuiltInBoardProfiles.All.Select(board => board.Id).ToArray());
}
