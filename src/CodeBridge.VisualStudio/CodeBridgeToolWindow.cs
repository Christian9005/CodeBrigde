using System;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio;

[Guid("9e4e69b5-6804-45aa-b6de-bbdd3c5a6d5b")]
public class CodeBridgeToolWindow : ToolWindowPane
{
    private CodeBridgeSetupControl _control;

    public CodeBridgeToolWindow() : base(null)
    {
        this.Caption = "CodeBridge Setup";
        _control = new CodeBridgeSetupControl();
        
        var host = new WindowsFormsHost();
        host.Child = _control;
        this.Content = host;
    }

    internal CodeBridgeSetupControl SetupControl => _control;
}
