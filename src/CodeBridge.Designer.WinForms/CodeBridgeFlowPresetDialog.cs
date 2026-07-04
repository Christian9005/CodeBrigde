using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms;

internal sealed class CodeBridgeFlowPresetDialog : Form
{
    private readonly BoardProfile _boardProfile;
    private readonly ComboBox _presetInput = new();
    private readonly ComboBox _pinInput = new();
    private readonly NumericUpDown _delayInput = new();
    private readonly CheckBox _activeLowInput = new();
    private readonly Label _delayLabel = new();
    private readonly Label _pinDescription = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public CodeBridgeFlowPresetDialog(BoardProfile? boardProfile = null)
    {
        _boardProfile = boardProfile ?? BuiltInBoardProfiles.Default;

        Text = "Load CodeBridge Preset";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 270);
        Font = DesignerTheme.UiFont;

        BuildLayout();
        PopulatePresets();
        WireEvents();
        UpdatePinOptions();
    }

    public FlowDocument Document => CreateDocument();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(14)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _presetInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _presetInput.Dock = DockStyle.Fill;

        _pinInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _pinInput.Dock = DockStyle.Fill;

        _delayInput.Minimum = 100;
        _delayInput.Maximum = 60000;
        _delayInput.Increment = 100;
        _delayInput.Value = 1500;
        _delayInput.Dock = DockStyle.Fill;

        _activeLowInput.Text = "Active-low output";
        _activeLowInput.Dock = DockStyle.Fill;

        _pinDescription.Dock = DockStyle.Fill;
        _pinDescription.ForeColor = Color.FromArgb(80, 86, 91);
        _pinDescription.Padding = new Padding(0, 6, 0, 0);

        _okButton.Text = "Load";
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Width = 86;

        _cancelButton.Text = "Cancel";
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Width = 86;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        buttons.Controls.Add(_okButton);
        buttons.Controls.Add(_cancelButton);

        AddLabel(root, "Preset", 0);
        AddLabel(root, "GPIO", 1);
        _delayLabel.Text = "Delay ms";
        _delayLabel.Dock = DockStyle.Fill;
        _delayLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_delayLabel, 0, 2);

        root.Controls.Add(_presetInput, 1, 0);
        root.Controls.Add(_pinInput, 1, 1);
        root.Controls.Add(_delayInput, 1, 2);
        root.Controls.Add(_activeLowInput, 1, 3);
        root.Controls.Add(_pinDescription, 1, 4);
        root.Controls.Add(buttons, 0, 5);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private static void AddLabel(TableLayoutPanel panel, string text, int row)
    {
        panel.Controls.Add(
            new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            },
            0,
            row);
    }

    private void PopulatePresets()
    {
        _presetInput.Items.Add(new PresetItem("Blink GPIO Once", CodeBridgePresetKind.BlinkOnce));
        _presetInput.Items.Add(new PresetItem("Digital Read + Debug", CodeBridgePresetKind.DigitalReadDebug));
        _presetInput.Items.Add(new PresetItem("Analog Stream Dashboard", CodeBridgePresetKind.AnalogStreamDashboard));
        _presetInput.SelectedIndex = 0;
    }

    private void WireEvents()
    {
        _presetInput.SelectedIndexChanged += (_, _) => UpdatePinOptions();
        _pinInput.SelectedIndexChanged += (_, _) =>
        {
            UpdatePinDescription();
            UpdateAnalogSampleLimit();
        };
    }

    private void UpdatePinOptions()
    {
        var selectedPin = SelectedPin;
        var pins = SelectedKind switch
        {
            CodeBridgePresetKind.DigitalReadDebug => _boardProfile.DigitalReadPinOptions,
            CodeBridgePresetKind.AnalogStreamDashboard => _boardProfile.AnalogReadPinOptions,
            _ => _boardProfile.DigitalWritePinOptions
        };

        _pinInput.Items.Clear();
        foreach (var pin in pins)
            _pinInput.Items.Add(new PinItem(pin));

        var selectedPinSupportsPreset = SelectedKind != CodeBridgePresetKind.AnalogStreamDashboard ||
            _boardProfile.FindPin(selectedPin)?.SupportsAnalogRead == true;
        SelectPin(selectedPin == 0 || !selectedPinSupportsPreset
            ? (SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard ? 32 : 2)
            : selectedPin);

        _delayLabel.Text = SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard ? "Sample Hz" : "Delay ms";
        _delayInput.Enabled = SelectedKind is CodeBridgePresetKind.BlinkOnce or CodeBridgePresetKind.AnalogStreamDashboard;
        _delayInput.Minimum = SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard ? 1 : 100;
        _delayInput.Maximum = SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard ? GetAnalogSampleMax() : 60000;
        _delayInput.Increment = SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard ? 100 : 100;
        _delayInput.Value = SelectedKind == CodeBridgePresetKind.AnalogStreamDashboard
            ? Math.Min(1000, _delayInput.Maximum)
            : Math.Clamp(_delayInput.Value, 100m, 60000m);
        _activeLowInput.Enabled = SelectedKind == CodeBridgePresetKind.BlinkOnce;
        UpdatePinDescription();
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

    private void UpdatePinDescription()
    {
        if (_pinInput.SelectedItem is not PinItem item)
        {
            _pinDescription.Text = string.Empty;
            return;
        }

        var pin = _boardProfile.FindPin(item.Number);
        _pinDescription.Text = pin?.Description ?? string.Empty;
    }

    private void UpdateAnalogSampleLimit()
    {
        if (SelectedKind != CodeBridgePresetKind.AnalogStreamDashboard)
            return;

        _delayInput.Maximum = GetAnalogSampleMax();
        if (_delayInput.Value > _delayInput.Maximum)
            _delayInput.Value = _delayInput.Maximum;
    }

    private decimal GetAnalogSampleMax()
    {
        var pin = _boardProfile.FindPin(SelectedPin);
        var max = pin is null
            ? _boardProfile.Runtime.MaxAnalogSampleRateHz
            : Math.Min(pin.MaxAnalogSampleRateHz, _boardProfile.Runtime.MaxAnalogSampleRateHz);

        return Math.Max(1, max);
    }

    private FlowDocument CreateDocument()
    {
        var document = SelectedKind switch
        {
            CodeBridgePresetKind.DigitalReadDebug => BuiltInFlowPresets.CreateDigitalReadDebug(SelectedPin),
            CodeBridgePresetKind.AnalogStreamDashboard => BuiltInFlowPresets.CreateAnalogStreamDashboard(
                SelectedPin,
                (int)_delayInput.Value),
            _ => BuiltInFlowPresets.CreateBlinkOnce(
                SelectedPin,
                (int)_delayInput.Value,
                _activeLowInput.Checked)
        };

        document.BoardId = _boardProfile.Id;
        NormalizeDocumentForBoardProfile(document);
        return document;
    }

    private void NormalizeDocumentForBoardProfile(FlowDocument document)
    {
        foreach (var sample in document.Nodes.Where(node => node.Type == BuiltInBlockCatalog.SampleChannel))
        {
            if (!_boardProfile.Runtime.SupportsHardwareTimers &&
                sample.Parameters.TryGetValue("mode", out var mode) &&
                string.Equals(Convert.ToString(mode), "HardwareTimer", StringComparison.OrdinalIgnoreCase))
            {
                sample.Parameters["mode"] = "Polling";
            }

            if (sample.Parameters.TryGetValue("sampleRateHz", out var sampleRateValue))
            {
                var sampleRate = Convert.ToInt32(sampleRateValue, System.Globalization.CultureInfo.InvariantCulture);
                sample.Parameters["sampleRateHz"] = Math.Min(sampleRate, (int)GetAnalogSampleMax());
            }

            if (sample.Parameters.TryGetValue("bufferCapacity", out var bufferValue))
            {
                var buffer = Convert.ToInt32(bufferValue, System.Globalization.CultureInfo.InvariantCulture);
                sample.Parameters["bufferCapacity"] = Math.Min(buffer, _boardProfile.Runtime.MaxBufferCapacity);
            }

            if (sample.Parameters.TryGetValue("batchSize", out var batchValue) &&
                sample.Parameters.TryGetValue("bufferCapacity", out var normalizedBufferValue))
            {
                var batch = Convert.ToInt32(batchValue, System.Globalization.CultureInfo.InvariantCulture);
                var buffer = Convert.ToInt32(normalizedBufferValue, System.Globalization.CultureInfo.InvariantCulture);
                sample.Parameters["batchSize"] = Math.Min(batch, buffer);
            }
        }
    }

    private CodeBridgePresetKind SelectedKind =>
        _presetInput.SelectedItem is PresetItem item ? item.Kind : CodeBridgePresetKind.BlinkOnce;

    private int SelectedPin =>
        _pinInput.SelectedItem is PinItem item ? item.Number : 2;

    private sealed record PresetItem(string Label, CodeBridgePresetKind Kind)
    {
        public override string ToString() => Label;
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

internal enum CodeBridgePresetKind
{
    BlinkOnce,
    DigitalReadDebug,
    AnalogStreamDashboard
}
