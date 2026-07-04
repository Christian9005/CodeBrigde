using CodeBridge.Designer.WinForms;

namespace CodeBridge.Designer.WinForms.Host;

public sealed class DesignerHostForm : Form
{
    private readonly CodeBridgeFlowDesignerControl _designer = new();

    public DesignerHostForm()
    {
        Text = "CodeBridge Designer Host";
        MinimumSize = new Size(1100, 720);
        Size = new Size(1320, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(28, 31, 34);
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;

        _designer.Dock = DockStyle.Fill;
        _designer.TextChanged += (_, _) => Text = _designer.Text;
        Controls.Add(_designer);
        Text = _designer.Text;
    }
}
