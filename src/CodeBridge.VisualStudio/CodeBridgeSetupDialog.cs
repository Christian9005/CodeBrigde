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

internal sealed class CodeBridgeSetupDialog : Form
{
    private readonly Button _installButton = new();
    private readonly Button _removeButton = new();
    private readonly Button _starterButton = new();
    private readonly Button _closeButton = new();

    public CodeBridgeSetupDialog(CodeBridgeSetupViewModel model)
    {
        SelectedAction = CodeBridgeSetupAction.None;

        Text = "CodeBridge Setup";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(760, 540);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.FromArgb(241, 241, 241);

        BuildLayout(model);
    }

    public CodeBridgeSetupAction SelectedAction { get; private set; }

    private void BuildLayout(CodeBridgeSetupViewModel model)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var title = new Label
        {
            Text = "CodeBridge Visual Studio Setup",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Regular),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var subtitle = new Label
        {
            Text = "Project setup, Toolbox package, and ESP32 starter workflow.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(190, 190, 190),
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
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.FromArgb(241, 241, 241),
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
            ForeColor = Color.FromArgb(210, 210, 210),
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

        ConfigureButton(_closeButton, "Close", 96);
        ConfigureButton(_starterButton, "Add Starter Form", 150);
        ConfigureButton(_removeButton, "Remove Package", 132);
        ConfigureButton(_installButton, "Install / Update", 132);

        _closeButton.Click += (_, _) =>
        {
            SelectedAction = CodeBridgeSetupAction.None;
            DialogResult = DialogResult.Cancel;
            Close();
        };
        _installButton.Click += (_, _) =>
        {
            SelectedAction = CodeBridgeSetupAction.InstallPackage;
            DialogResult = DialogResult.OK;
            Close();
        };
        _removeButton.Click += (_, _) =>
        {
            SelectedAction = CodeBridgeSetupAction.RemovePackage;
            DialogResult = DialogResult.OK;
            Close();
        };
        _starterButton.Click += (_, _) =>
        {
            SelectedAction = CodeBridgeSetupAction.AddStarterForm;
            DialogResult = DialogResult.OK;
            Close();
        };

        _installButton.Enabled = model.CanRunProjectActions;
        _removeButton.Enabled = model.CanRemovePackage;
        _starterButton.Enabled = model.CanRunProjectActions;

        panel.Controls.Add(_closeButton);
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
            BackColor = Color.FromArgb(37, 37, 38),
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
            ForeColor = Color.White,
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
            ForeColor = Color.FromArgb(173, 173, 173),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        panel.Controls.Add(new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(241, 241, 241),
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

internal sealed class WindowHandleWrapper : IWin32Window
{
    public WindowHandleWrapper(IntPtr handle)
    {
        Handle = handle;
    }

    public IntPtr Handle { get; }
}
