using CodeBridge.Flow;

namespace CodeBridge.Designer.WinForms.Hardware;

internal sealed record FirmwareUploadRequest(
    BoardProfile BoardProfile,
    string PortName);
