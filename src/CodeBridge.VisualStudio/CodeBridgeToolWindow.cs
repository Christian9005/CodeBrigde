using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio;

[Guid("9e4e69b5-6804-45aa-b6de-bbdd3c5a6d5b")]
public class CodeBridgeToolWindow : ToolWindowPane
{
    private readonly CodeBridgeSetupControl _control;

    public CodeBridgeToolWindow() : base(null)
    {
        Caption = "CodeBridge Setup";
        _control = new CodeBridgeSetupControl();
        Content = _control;
    }

    internal CodeBridgeSetupControl SetupControl => _control;
}
