using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CodeBridge.VisualStudio;

internal enum CodeBridgeSetupAction
{
    None,
    InstallPackage,
    RemovePackage,
    AddStarterForm
}

internal sealed class CodeBridgeSetupControl : UserControl
{
    private readonly Button _installButton = new();
    private readonly Button _removeButton = new();
    private readonly Button _starterButton = new();

    public event EventHandler<CodeBridgeSetupAction>? ActionSelected;

    public CodeBridgeSetupControl()
    {
        Font = new Font("Segoe UI", 9F);
        BackColor = SetupTheme.Background;
        ForeColor = SetupTheme.Text;

        SetupTheme.Changed += OnThemeChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SetupTheme.Changed -= OnThemeChanged;

        base.Dispose(disposing);
    }

    private CodeBridgeSetupViewModel? _model;

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        BackColor = SetupTheme.Background;
        ForeColor = SetupTheme.Text;
        if (_model is not null)
            UpdateModel(_model);
    }

    public void UpdateModel(CodeBridgeSetupViewModel model)
    {
        _model = model;
        foreach (Control existing in Controls.Cast<Control>().ToArray())
            existing.Dispose();
        Controls.Clear();
        BuildLayout(model);
    }

    private void BuildLayout(CodeBridgeSetupViewModel model)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(58)));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(150)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(72)));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(42)));

        var title = new Label
        {
            Text = "CodeBridge Visual Studio Setup",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Regular),
            ForeColor = SetupTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var subtitle = new Label
        {
            Text = "Project setup, Toolbox package, and starter workflow for ESP32 and Arduino Uno.",
            Dock = DockStyle.Fill,
            ForeColor = SetupTheme.Muted,
            TextAlign = ContentAlignment.TopLeft
        };
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(subtitle, 0, 1);

        var status = CreateStatusPanel(model);
        var components = CreateComponentsPanel(model);
        var nextSteps = CreateNextStepsPanel();
        var buttons = CreateButtonPanel(model);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(status, 0, 1);
        root.Controls.Add(components, 0, 2);
        root.Controls.Add(nextSteps, 0, 3);
        root.Controls.Add(buttons, 0, 4);

        Controls.Add(root);
    }

    private static Control CreateStatusPanel(CodeBridgeSetupViewModel model)
    {
        var panel = CreatePanel(2, 5);
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddStatusRow(panel, 0, "Project", model.ProjectName ?? "No active C# project selected");
        AddStatusRow(panel, 1, "Package", $"{model.PackageId} {model.PackageVersion}");
        AddStatusRow(panel, 2, "Installed", model.InstalledPackageVersion ?? "Not installed in selected project");
        AddStatusRow(panel, 3, "Package feed", model.PackageFeedPath ?? "Not found");
        AddStatusRow(panel, 4, "Toolbox", model.ToolboxCategory);
        return panel;
    }

    private static Control CreateComponentsPanel(CodeBridgeSetupViewModel model)
    {
        var panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(CreateSectionLabel("Toolbox components"), 0, 0);

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = SetupTheme.Panel,
            ForeColor = SetupTheme.Text,
            IntegralHeight = false
        };

        foreach (var component in model.Components)
            list.Items.Add(component);

        panel.Controls.Add(list, 0, 1);
        return panel;
    }

    private static Control CreateNextStepsPanel()
    {
        var panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(CreateSectionLabel("Recommended first run"), 0, 0);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SetupTheme.Muted,
            Text = "Add the starter form, rebuild, run the project, select the board COM port, use Upload FW if the board is fresh, then click Run Blink.",
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        return panel;
    }

    private Control CreateButtonPanel(CodeBridgeSetupViewModel model)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        ConfigureButton(_starterButton, "Add Starter Form", 150);
        ConfigureButton(_removeButton, "Remove Package", 132);
        ConfigureButton(_installButton, "Install / Update", 132);

        _installButton.Click += (_, _) => ActionSelected?.Invoke(this, CodeBridgeSetupAction.InstallPackage);
        _removeButton.Click += (_, _) => ActionSelected?.Invoke(this, CodeBridgeSetupAction.RemovePackage);
        _starterButton.Click += (_, _) => ActionSelected?.Invoke(this, CodeBridgeSetupAction.AddStarterForm);

        _installButton.Enabled = model.CanRunProjectActions;
        _removeButton.Enabled = model.CanRemovePackage;
        _starterButton.Enabled = model.CanRunProjectActions;

        panel.Controls.Add(_starterButton);
        panel.Controls.Add(_removeButton);
        panel.Controls.Add(_installButton);
        return panel;
    }

    private static TableLayoutPanel CreatePanel(int columns, int rows)
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            BackColor = SetupTheme.Panel,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular),
            ForeColor = SetupTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static void AddStatusRow(TableLayoutPanel panel, int row, string label, string value)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = SetupTheme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        panel.Controls.Add(new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            ForeColor = SetupTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 1, row);
    }

    private static void ConfigureButton(Button button, string text, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.FlatStyle = FlatStyle.System;
        button.Margin = new Padding(8, 6, 0, 0);
    }
}

internal sealed class CodeBridgeSetupViewModel
{
    public string? ProjectName { get; set; }

    public string? ProjectPath { get; set; }

    public string PackageId { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public string? PackageFeedPath { get; set; }

    public string? InstalledPackageVersion { get; set; }

    public string ToolboxCategory { get; set; } = string.Empty;

    public string[] Components { get; set; } = Array.Empty<string>();

    public bool CanRunProjectActions => !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(PackageFeedPath);

    public bool CanRemovePackage => !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(InstalledPackageVersion);
}

/// <summary>Colors taken from the active Visual Studio theme, with dark defaults outside Visual Studio.</summary>
internal static class SetupTheme
{
    private static readonly Color FallbackBackground = Color.FromArgb(30, 30, 30);
    private static readonly Color FallbackText = Color.FromArgb(241, 241, 241);
    private static readonly Color FallbackMuted = Color.FromArgb(173, 173, 173);

    private static bool _hostAvailable = true;
    private static bool _subscribed;

    public static event EventHandler? Changed
    {
        add
        {
            ChangedInternal += value;
            Subscribe();
        }
        remove => ChangedInternal -= value;
    }

    private static event EventHandler? ChangedInternal;

    public static Color Background => Read(ThemeColor.Background, FallbackBackground);

    public static Color Text => Read(ThemeColor.Text, FallbackText);

    public static Color Muted => Read(ThemeColor.Muted, FallbackMuted);

    public static Color Panel => Blend(Background, Text, 0.06f);

    private enum ThemeColor
    {
        Background,
        Text,
        Muted
    }

    private static void Subscribe()
    {
        if (_subscribed || !_hostAvailable)
            return;

        try
        {
            SubscribeCore();
            _subscribed = true;
        }
        catch (Exception ex) when (ex is System.IO.IOException || ex is TypeLoadException || ex is InvalidOperationException)
        {
            _hostAvailable = false;
        }
    }

    // Separate methods: the JIT only loads Visual Studio assemblies inside the try blocks above/below.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void SubscribeCore() =>
        Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += _ => ChangedInternal?.Invoke(null, EventArgs.Empty);

    private static Color Read(ThemeColor color, Color fallback)
    {
        if (!_hostAvailable)
            return fallback;

        try
        {
            return ReadCore(color);
        }
        catch (Exception ex) when (ex is System.IO.IOException || ex is TypeLoadException || ex is InvalidOperationException)
        {
            _hostAvailable = false;
            return fallback;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static Color ReadCore(ThemeColor color)
    {
        var key = color switch
        {
            ThemeColor.Background => Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowBackgroundColorKey,
            ThemeColor.Text => Microsoft.VisualStudio.PlatformUI.EnvironmentColors.ToolWindowTextColorKey,
            _ => Microsoft.VisualStudio.PlatformUI.EnvironmentColors.SystemGrayTextColorKey
        };

        return Microsoft.VisualStudio.PlatformUI.VSColorTheme.GetThemedColor(key);
    }

    private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));
}

internal sealed class WindowHandleWrapper : IWin32Window
{
    public WindowHandleWrapper(IntPtr handle)
    {
        Handle = handle;
    }

    public IntPtr Handle { get; }
}
