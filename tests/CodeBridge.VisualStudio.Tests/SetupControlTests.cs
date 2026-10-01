using System.Threading;
using System.Windows;
using CodeBridge.VisualStudio;

namespace CodeBridge.VisualStudio.Tests;

public sealed class SetupControlTests
{
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); } catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException(failure.ToString());
    }

    private static CodeBridgeSetupViewModel Model(string? installed) => new()
    {
        ProjectName = "CodeBridgeApp1",
        ProjectPath = @"C:\Users\someone\source\repos\CodeBridgeApp1\CodeBridgeApp1.csproj",
        PackageId = "CodeBridge.Designer.WinForms",
        PackageVersion = "0.5.6",
        PackageFeedPath = @"c:\users\someone\appdata\local\microsoft\visualstudio\18.0_4fff2074\extensions\5xi10mwd.4xl\Packages",
        InstalledPackageVersion = installed,
        ToolboxCategory = "CodeBridge",
        Components = new[] { "CodeBridge Flow (Control)", "CodeBridge Board (ComponentTray)" }
    };

    [Fact]
    public void The_page_can_be_rebuilt_many_times_after_an_action()
    {
        // Regression: the old WinForms page disposed its buttons when refreshed after Install/Update and then reused them
        // ("Cannot access a disposed object. Object name: 'Button'").
        RunSta(() =>
        {
            var control = new CodeBridgeSetupControl();
            var window = new Window { Content = control, Width = 480, Height = 600, ShowInTaskbar = false, Left = -5000, Top = -5000 };
            window.Show();

            for (var i = 0; i < 5; i++)
            {
                control.UpdateModel(Model(i % 2 == 0 ? null : "0.5.6"));
                control.UpdateLayout();
            }

            window.Close();
        });
    }

    [Theory]
    [InlineData(260)]
    [InlineData(480)]
    [InlineData(1100)]
    public void The_page_reflows_at_any_width_without_horizontal_overflow(double width)
    {
        RunSta(() =>
        {
            var control = new CodeBridgeSetupControl();
            control.UpdateModel(Model("0.5.6"));
            var window = new Window { Content = control, Width = width, Height = 700, ShowInTaskbar = false, Left = -5000, Top = -5000 };
            window.Show();
            control.UpdateLayout();

            var scroll = (System.Windows.Controls.ScrollViewer)control.Content;
            Assert.True(scroll.ExtentWidth <= scroll.ViewportWidth + 1, $"Content ({scroll.ExtentWidth}) is wider than the window ({scroll.ViewportWidth}).");
            window.Close();
        });
    }

    [Fact]
    public void Actions_are_raised_by_the_buttons()
    {
        RunSta(() =>
        {
            var control = new CodeBridgeSetupControl();
            var raised = new List<CodeBridgeSetupAction>();
            control.ActionSelected += (_, action) => raised.Add(action);
            control.UpdateModel(Model("0.5.6"));

            var buttons = new List<System.Windows.Controls.Button>();
            Collect(control, buttons);
            foreach (var button in buttons.Where(b => b.IsEnabled))
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.Contains(CodeBridgeSetupAction.InstallPackage, raised);
            Assert.Contains(CodeBridgeSetupAction.AddStarterForm, raised);
            Assert.Contains(CodeBridgeSetupAction.RemovePackage, raised);
        });
    }

    private static void Collect(DependencyObject parent, List<System.Windows.Controls.Button> buttons)
    {
        if (parent is System.Windows.Controls.Button button && button.Content is string text && text != "Take the CodeBridge Tour")
            buttons.Add(button);

        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            Collect(child, buttons);
    }
}
