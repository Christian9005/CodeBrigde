#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio.Editor
{
    internal enum IconKind
    {
        Run,
        Stop,
        Undo,
        Redo,
        Refresh,
        Upload,
        Connect,
        Delete,
        Copy,
        Paste,
        Clear,
        Output,
        ZoomIn,
        ZoomOut,
        Fit,
        Arrange
    }

    /// <summary>
    /// Theme-aware icons (KnownMonikers through CrispImage) with a Segoe MDL2 glyph fallback when the editor runs
    /// outside Visual Studio (unit tests, designer). Control styling lives in EditorTheme.xaml.
    /// </summary>
    internal static class NativeUi
    {
        private static bool _hostAvailable = true;

        public static FrameworkElement CreateIcon(IconKind kind)
        {
            if (_hostAvailable)
            {
                try
                {
                    return CreateCrispIcon(kind);
                }
                catch (Exception ex) when (ex is IOException || ex is TypeLoadException || ex is InvalidOperationException)
                {
                    _hostAvailable = false;
                }
            }

            return new TextBlock
            {
                Text = Glyph(kind),
                FontFamily = new FontFamily("Segoe MDL2 Assets, Segoe UI Symbol"),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static FrameworkElement CreateCrispIcon(IconKind kind)
        {
            var glyphOnly = kind == IconKind.ZoomIn || kind == IconKind.ZoomOut || kind == IconKind.Fit || kind == IconKind.Arrange;
            if (glyphOnly)
            {
                return new TextBlock
                {
                    Text = Glyph(kind),
                    FontFamily = new FontFamily("Segoe MDL2 Assets, Segoe UI Symbol"),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            var image = new CrispImage
            {
                Moniker = Moniker(kind),
                Width = 16,
                Height = 16
            };
            image.SetResourceReference(ImageThemingUtilities.ImageBackgroundColorProperty, EnvironmentColors.ToolWindowBackgroundColorKey);
            return image;
        }

        private static ImageMoniker Moniker(IconKind kind)
        {
            switch (kind)
            {
                case IconKind.Run: return KnownMonikers.Run;
                case IconKind.Stop: return KnownMonikers.Stop;
                case IconKind.Undo: return KnownMonikers.Undo;
                case IconKind.Redo: return KnownMonikers.Redo;
                case IconKind.Refresh: return KnownMonikers.Refresh;
                case IconKind.Upload: return KnownMonikers.Upload;
                case IconKind.Connect: return KnownMonikers.USB;
                case IconKind.Delete: return KnownMonikers.Cancel;
                case IconKind.Copy: return KnownMonikers.Copy;
                case IconKind.Paste: return KnownMonikers.Paste;
                case IconKind.Clear: return KnownMonikers.ClearWindowContent;
                case IconKind.Output: return KnownMonikers.Console;
                default: return KnownMonikers.QuestionMark;
            }
        }

        private static string Glyph(IconKind kind)
        {
            switch (kind)
            {
                case IconKind.Run: return "";
                case IconKind.Stop: return "";
                case IconKind.Undo: return "";
                case IconKind.Redo: return "";
                case IconKind.Refresh: return "";
                case IconKind.Upload: return "";
                case IconKind.Connect: return "";
                case IconKind.Delete: return "";
                case IconKind.Copy: return "";
                case IconKind.Paste: return "";
                case IconKind.Clear: return "";
                case IconKind.Output: return "";
                case IconKind.ZoomIn: return "";
                case IconKind.ZoomOut: return "";
                case IconKind.Fit: return "";
                case IconKind.Arrange: return "\uE8FD";
                default: return "";
            }
        }
    }
}
