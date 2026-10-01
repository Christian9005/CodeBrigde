using System;
using System.Windows.Forms;

namespace $safeprojectname$
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new CodeBridgeStarterForm());
        }
    }
}
