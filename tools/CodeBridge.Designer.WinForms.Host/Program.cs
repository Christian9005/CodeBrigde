namespace CodeBridge.Designer.WinForms.Host;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new DesignerHostForm());
    }
}
