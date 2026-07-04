using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using CodeBridge.Flow;
using CodeBridge.Flow.Serialization;

namespace CodeBridge.Designer.WinForms;

internal sealed class CodeBridgeFlowControlDesigner : ControlDesigner
{
    private DesignerActionListCollection? _actionLists;
    private readonly DesignerVerbCollection _verbs;

    public CodeBridgeFlowControlDesigner()
    {
        _verbs =
        [
            new DesignerVerb("Edit CodeBridge Flow...", (_, _) => EditFlow()),
            new DesignerVerb("Load Preset...", (_, _) => LoadPresetDialog()),
            new DesignerVerb("Load Blink GPIO 2 Preset", (_, _) => LoadBlinkPreset()),
            new DesignerVerb("Load Active-Low Blink Preset", (_, _) => LoadActiveLowBlinkPreset()),
            new DesignerVerb("Load Read Debug Preset", (_, _) => LoadReadDebugPreset()),
            new DesignerVerb("Set Trigger Mode", (_, _) => SetExecutionMode(FlowExecutionMode.Trigger)),
            new DesignerVerb("Set Loop Mode", (_, _) => SetExecutionMode(FlowExecutionMode.Loop))
        ];
    }

    public override DesignerVerbCollection Verbs => _verbs;

    public override DesignerActionListCollection ActionLists =>
        _actionLists ??=
        [
            new CodeBridgeFlowControlActionList(this)
        ];

    public override void DoDefaultAction() => EditFlow();

    internal void EditFlow()
    {
        if (Control is not CodeBridgeFlowControl flowControl)
            return;

        var changeService = (IComponentChangeService?)GetService(typeof(IComponentChangeService));
        flowControl.EditFlow(changeService);
    }

    internal void LoadPresetDialog()
    {
        if (Control is not CodeBridgeFlowControl flowControl)
            return;

        var changeService = (IComponentChangeService?)GetService(typeof(IComponentChangeService));
        flowControl.ShowPresetDialog(changeService);
    }

    internal void LoadBlinkPreset() =>
        SetFlowDocument(BuiltInFlowPresets.CreateBlinkOnce());

    internal void LoadActiveLowBlinkPreset() =>
        SetFlowDocument(BuiltInFlowPresets.CreateBlinkOnce(activeLow: true));

    internal void LoadReadDebugPreset() =>
        SetFlowDocument(BuiltInFlowPresets.CreateDigitalReadDebug());

    internal void SetExecutionMode(FlowExecutionMode mode)
    {
        if (Control is not CodeBridgeFlowControl flowControl)
            return;

        SetProperty(flowControl, nameof(CodeBridgeFlowControl.ExecutionMode), mode);
    }

    internal void SetProperty(object component, string propertyName, object? value)
    {
        var property = TypeDescriptor.GetProperties(component)[propertyName];
        if (property is null)
            return;

        var changeService = (IComponentChangeService?)GetService(typeof(IComponentChangeService));
        var oldValue = property.GetValue(component);
        changeService?.OnComponentChanging(component, property);
        property.SetValue(component, value);
        changeService?.OnComponentChanged(component, property, oldValue, value);
    }

    private void SetFlowDocument(FlowDocument document)
    {
        if (Control is not CodeBridgeFlowControl flowControl)
            return;

        SetProperty(flowControl, nameof(CodeBridgeFlowControl.FlowJson), FlowDocumentJson.Serialize(document));
    }
}

internal sealed class CodeBridgeFlowControlActionList : DesignerActionList
{
    private readonly CodeBridgeFlowControlDesigner _designer;
    private readonly CodeBridgeFlowControl _control;

    public CodeBridgeFlowControlActionList(CodeBridgeFlowControlDesigner designer)
        : base(designer.Control)
    {
        _designer = designer;
        _control = (CodeBridgeFlowControl)designer.Control;
    }

    public string FlowName
    {
        get => _control.FlowName;
        set => _designer.SetProperty(_control, nameof(CodeBridgeFlowControl.FlowName), value);
    }

    public FlowExecutionMode ExecutionMode
    {
        get => _control.ExecutionMode;
        set => _designer.SetProperty(_control, nameof(CodeBridgeFlowControl.ExecutionMode), value);
    }

    public int LoopIntervalMs
    {
        get => _control.LoopIntervalMs;
        set => _designer.SetProperty(_control, nameof(CodeBridgeFlowControl.LoopIntervalMs), value);
    }

    public int TraceDelayMs
    {
        get => _control.TraceDelayMs;
        set => _designer.SetProperty(_control, nameof(CodeBridgeFlowControl.TraceDelayMs), value);
    }

    public void EditFlow() => _designer.EditFlow();

    public void LoadPresetDialog() => _designer.LoadPresetDialog();

    public void LoadBlinkPreset() => _designer.LoadBlinkPreset();

    public void LoadActiveLowBlinkPreset() => _designer.LoadActiveLowBlinkPreset();

    public void LoadReadDebugPreset() => _designer.LoadReadDebugPreset();

    public void UseTriggerMode() => _designer.SetExecutionMode(FlowExecutionMode.Trigger);

    public void UseLoopMode() => _designer.SetExecutionMode(FlowExecutionMode.Loop);

    public override DesignerActionItemCollection GetSortedActionItems()
    {
        return
        [
            new DesignerActionHeaderItem("CodeBridge"),
            new DesignerActionMethodItem(this, nameof(EditFlow), "Edit Flow...", "CodeBridge", "Open the visual CodeBridge flow editor.", includeAsDesignerVerb: true),
            new DesignerActionHeaderItem("Presets"),
            new DesignerActionMethodItem(this, nameof(LoadPresetDialog), "Load Preset...", "CodeBridge Presets", "Choose a starter flow, GPIO, delay, and output polarity.", includeAsDesignerVerb: true),
            new DesignerActionMethodItem(this, nameof(LoadBlinkPreset), "Load Blink GPIO 2", "CodeBridge Presets", "Replace the flow with a ready-to-run GPIO 2 blink.", includeAsDesignerVerb: true),
            new DesignerActionMethodItem(this, nameof(LoadActiveLowBlinkPreset), "Load Active-Low Blink", "CodeBridge Presets", "Replace the flow with an active-low GPIO 2 blink.", includeAsDesignerVerb: true),
            new DesignerActionMethodItem(this, nameof(LoadReadDebugPreset), "Load Read Debug", "CodeBridge Presets", "Replace the flow with a GPIO 2 read and debug trace.", includeAsDesignerVerb: true),
            new DesignerActionPropertyItem(nameof(FlowName), "Flow Name", "CodeBridge", "Friendly name shown on the WinForms surface."),
            new DesignerActionPropertyItem(nameof(ExecutionMode), "Execution Mode", "CodeBridge", "Trigger runs once; Loop repeats until stopped."),
            new DesignerActionPropertyItem(nameof(LoopIntervalMs), "Loop Interval (ms)", "CodeBridge", "Delay between loop iterations."),
            new DesignerActionPropertyItem(nameof(TraceDelayMs), "Trace Delay (ms)", "CodeBridge", "Delay after each node event so execution is visible."),
            new DesignerActionMethodItem(this, nameof(UseTriggerMode), "Use Trigger Mode", "CodeBridge", "Run the flow once when called.", includeAsDesignerVerb: false),
            new DesignerActionMethodItem(this, nameof(UseLoopMode), "Use Loop Mode", "CodeBridge", "Repeat the flow until stopped.", includeAsDesignerVerb: false)
        ];
    }
}
