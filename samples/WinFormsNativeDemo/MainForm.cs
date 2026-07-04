using CodeBridge.Core;
using CodeBridge.Designer.WinForms;
using CodeBridge.Flow;
using CodeBridge.Flow.Execution;
using CodeBridge.Transport;

namespace CodeBridge.Samples.WinFormsNativeDemo;

public sealed class MainForm : Form
{
    private readonly CodeBridgeEsp32Component _esp32 = new();
    private readonly CodeBridgeFlowControl _flow = new();
    private readonly ComboBox _portInput = new();
    private readonly NumericUpDown _baudRateInput = new();
    private readonly ComboBox _pinInput = new();
    private readonly ComboBox _modeInput = new();
    private readonly NumericUpDown _loopIntervalInput = new();
    private readonly NumericUpDown _traceDelayInput = new();
    private readonly Button _refreshButton = new();
    private readonly Button _connectButton = new();
    private readonly Button _disconnectButton = new();
    private readonly Button _editButton = new();
    private readonly Button _presetButton = new();
    private readonly Button _blinkTemplateButton = new();
    private readonly Button _runButton = new();
    private readonly Button _stopButton = new();
    private readonly TextBox _log = new();
    private bool _closing;

    public MainForm()
    {
        Text = "CodeBridge WinForms Native Demo";
        MinimumSize = new Size(920, 560);
        Size = new Size(1080, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 247, 248);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        PopulatePins();
        WireEvents();
        LoadDefaultFlow();
        RefreshPorts();
        UpdateState();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14),
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 14,
            Padding = new Padding(0, 0, 14, 0)
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _portInput.DropDownStyle = ComboBoxStyle.DropDown;
        _portInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        _baudRateInput.Minimum = 9600;
        _baudRateInput.Maximum = 921600;
        _baudRateInput.Increment = 9600;
        _baudRateInput.Value = 115200;
        _baudRateInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        _pinInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _pinInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        _modeInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _modeInput.Items.AddRange(Enum.GetNames<FlowExecutionMode>());
        _modeInput.SelectedItem = FlowExecutionMode.Trigger.ToString();
        _modeInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        _loopIntervalInput.Minimum = 100;
        _loopIntervalInput.Maximum = 60000;
        _loopIntervalInput.Increment = 100;
        _loopIntervalInput.Value = 1000;
        _loopIntervalInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        _traceDelayInput.Minimum = 0;
        _traceDelayInput.Maximum = 5000;
        _traceDelayInput.Increment = 50;
        _traceDelayInput.Value = 120;
        _traceDelayInput.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        ConfigureButton(_refreshButton, "Refresh Ports");
        ConfigureButton(_connectButton, "Connect");
        ConfigureButton(_disconnectButton, "Disconnect");
        ConfigureButton(_editButton, "Edit Flow");
        ConfigureButton(_presetButton, "Choose Preset");
        ConfigureButton(_blinkTemplateButton, "Load Blink");
        ConfigureButton(_runButton, "Run");
        ConfigureButton(_stopButton, "Stop");

        AddRow(left, 0, "Port", _portInput);
        AddRow(left, 1, "Baud", _baudRateInput);
        AddRow(left, 2, string.Empty, _refreshButton);
        AddRow(left, 3, string.Empty, _connectButton);
        AddRow(left, 4, string.Empty, _disconnectButton);
        AddRow(left, 5, "GPIO", _pinInput);
        AddRow(left, 6, "Mode", _modeInput);
        AddRow(left, 7, "Loop ms", _loopIntervalInput);
        AddRow(left, 8, "Trace ms", _traceDelayInput);
        AddRow(left, 9, string.Empty, _editButton);
        AddRow(left, 10, string.Empty, _presetButton);
        AddRow(left, 11, string.Empty, _blinkTemplateButton);
        AddRow(left, 12, string.Empty, _runButton);
        AddRow(left, 13, string.Empty, _stopButton);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _flow.Dock = DockStyle.Fill;
        _flow.Margin = new Padding(0);

        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Font = new Font("Consolas", 9F);
        _log.BackColor = Color.FromArgb(30, 34, 37);
        _log.ForeColor = Color.FromArgb(235, 238, 236);
        _log.BorderStyle = BorderStyle.FixedSingle;

        right.Controls.Add(_flow, 0, 0);
        right.Controls.Add(_log, 0, 2);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
    }

    private void WireEvents()
    {
        _refreshButton.Click += (_, _) => RefreshPorts();
        _connectButton.Click += async (_, _) => await ConnectAsync();
        _disconnectButton.Click += async (_, _) => await DisconnectAsync();
        _editButton.Click += (_, _) => _flow.ShowEditor(this);
        _presetButton.Click += (_, _) => LoadPresetFromDialog();
        _blinkTemplateButton.Click += (_, _) => LoadDefaultFlow();
        _runButton.Click += async (_, _) => await RunFlowAsync();
        _stopButton.Click += (_, _) => _flow.Stop();
        _modeInput.SelectedIndexChanged += (_, _) => SyncFlowSettings();
        _loopIntervalInput.ValueChanged += (_, _) => SyncFlowSettings();
        _traceDelayInput.ValueChanged += (_, _) => SyncFlowSettings();

        _esp32.Connected += (_, _) => AppendLog("ESP32 connected.");
        _esp32.Disconnected += (_, _) => AppendLog("ESP32 disconnected.");
        _esp32.ConnectionFailed += (_, ex) => AppendLog("Connection failed: " + ex.Message);

        _flow.FlowStarted += (_, _) => AppendLog("Flow started.");
        _flow.FlowCompleted += (_, result) => AppendLog($"Flow completed. Nodes: {result.NodeOutputs.Count}");
        _flow.FlowFailed += (_, ex) => AppendLog("Flow failed: " + ex.Message);
        _flow.FlowTrace += (_, trace) => AppendLog($"{FormatTraceKind(trace.Kind)} {trace.NodeId}");
        _flow.FlowStopped += (_, _) =>
        {
            AppendLog("Flow stopped.");
            UpdateState();
        };
    }

    private void PopulatePins()
    {
        _pinInput.Items.Clear();

        foreach (var option in BuiltInBoardProfiles.Esp32DevKit.DigitalWritePinOptions)
            _pinInput.Items.Add(new PinItem(option));

        SelectPin(2);
    }

    private void RefreshPorts()
    {
        var selected = _portInput.Text;
        _portInput.Items.Clear();

        foreach (var port in BoardDiscovery.DiscoverPorts())
            _portInput.Items.Add(port.Name);

        if (!string.IsNullOrWhiteSpace(selected))
            _portInput.Text = selected;
        else if (_portInput.Items.Count > 0)
            _portInput.SelectedIndex = 0;

        AppendLog(_portInput.Items.Count == 0
            ? "No serial ports detected."
            : $"Detected {_portInput.Items.Count} serial port(s).");
    }

    private async Task ConnectAsync()
    {
        SyncConnectionSettings();
        UpdateState(connecting: true);

        try
        {
            await _esp32.ConnectAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "CodeBridge connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateState();
        }
    }

    private async Task DisconnectAsync()
    {
        _flow.Stop();
        await _esp32.DisconnectAsync();
        UpdateState();
    }

    private async Task RunFlowAsync()
    {
        SyncConnectionSettings();
        SyncFlowSettings();
        UpdateState(running: true);

        try
        {
            await _esp32.RunAsync(_flow);
        }
        catch (Exception ex) when (!_closing)
        {
            MessageBox.Show(this, ex.Message, "CodeBridge flow", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateState();
        }
    }

    private void SyncConnectionSettings()
    {
        _esp32.TransportMode = CodeBridgeTransportMode.Serial;
        _esp32.PortName = _portInput.Text.Trim();
        _esp32.BaudRate = (int)_baudRateInput.Value;
    }

    private void SyncFlowSettings()
    {
        if (Enum.TryParse<FlowExecutionMode>(_modeInput.Text, out var mode))
            _flow.ExecutionMode = mode;

        _flow.LoopIntervalMs = (int)_loopIntervalInput.Value;
        _flow.TraceDelayMs = (int)_traceDelayInput.Value;
    }

    private void LoadPresetFromDialog()
    {
        if (_flow.ShowPresetDialog(this))
            AppendLog($"Loaded preset: {_flow.FlowName}.");
    }

    private void LoadDefaultFlow()
    {
        var pin = SelectedPin;
        _flow.FlowName = "ESP32 Blink Flow";
        _flow.LoadBlinkPreset(pin, delayMs: 500);
        AppendLog($"Loaded blink flow for GPIO {pin}.");
    }

    private void UpdateState(bool connecting = false, bool running = false)
    {
        var isRunning = running || _flow.IsRunning;
        var isConnected = _esp32.IsConnected;

        _connectButton.Enabled = !connecting && !isConnected && !isRunning;
        _disconnectButton.Enabled = isConnected && !isRunning;
        _runButton.Enabled = !connecting && !isRunning;
        _stopButton.Enabled = isRunning;
        _refreshButton.Enabled = !connecting && !isRunning;
        _portInput.Enabled = !isConnected && !isRunning;
        _baudRateInput.Enabled = !isConnected && !isRunning;
        _modeInput.Enabled = !isRunning;
        _loopIntervalInput.Enabled = !isRunning;
        _traceDelayInput.Enabled = !isRunning;
        _pinInput.Enabled = !isRunning;
        _editButton.Enabled = !isRunning;
        _presetButton.Enabled = !isRunning;
        _blinkTemplateButton.Enabled = !isRunning;
    }

    private void SelectPin(int pin)
    {
        for (var index = 0; index < _pinInput.Items.Count; index++)
        {
            if (_pinInput.Items[index] is PinItem item && item.Number == pin)
            {
                _pinInput.SelectedIndex = index;
                return;
            }
        }

        if (_pinInput.Items.Count > 0)
            _pinInput.SelectedIndex = 0;
    }

    private int SelectedPin => _pinInput.SelectedItem is PinItem item ? item.Number : 2;

    private static string FormatTraceKind(FlowExecutionEventKind kind)
    {
        return kind switch
        {
            FlowExecutionEventKind.NodeStarted => "started",
            FlowExecutionEventKind.NodeCompleted => "completed",
            FlowExecutionEventKind.NodeSkipped => "skipped",
            FlowExecutionEventKind.NodeFailed => "failed",
            _ => kind.ToString()
        };
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Height = 30;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 3, 0, 3);
    }

    private static void AddRow(TableLayoutPanel panel, int row, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(54, 61, 66)
        };

        control.Margin = new Padding(0, 3, 0, 3);
        panel.Controls.Add(labelControl, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private void AppendLog(string message)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => AppendLog(message)));
            return;
        }

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        _closing = true;
        _flow.Stop();
        await _esp32.DisposeAsync();
        base.OnFormClosing(e);
    }

    private sealed class PinItem
    {
        public PinItem(FlowPropertyOption option)
        {
            Label = option.Label;
            Number = Convert.ToInt32(option.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public string Label { get; }
        public int Number { get; }

        public override string ToString() => Label;
    }
}
