#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>
    /// Maps the editor's brush palette onto the active Visual Studio theme (Light, Dark, Blue, High Contrast)
    /// and keeps it in sync when the user switches themes. Outside Visual Studio (unit tests, designer)
    /// the dark defaults declared in XAML stay in place.
    /// </summary>
    internal static class VsTheme
    {
        private static readonly List<WeakReference<ResourceDictionary>> Tracked = new List<WeakReference<ResourceDictionary>>();
        private static bool _subscribed;

        public static void Bind(ResourceDictionary resources)
        {
            if (Tracked.Count >= 256)
                Tracked.RemoveAll(reference => !reference.TryGetTarget(out _));

            Tracked.Add(new WeakReference<ResourceDictionary>(resources));

            try
            {
                ApplyAndSubscribe(resources);
            }
            catch (Exception ex) when (ex is IOException || ex is TypeLoadException || ex is InvalidOperationException)
            {
                // Not hosted in Visual Studio (unit tests, designer): the XAML dark defaults stay.
            }
        }

        // Kept out of Bind so the JIT only touches Visual Studio assemblies inside the try block above.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ApplyAndSubscribe(ResourceDictionary resources)
        {
            Apply(resources);

            if (_subscribed)
                return;

            VSColorTheme.ThemeChanged += OnThemeChanged;
            _subscribed = true;
        }

        private static void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            for (var i = Tracked.Count - 1; i >= 0; i--)
            {
                if (Tracked[i].TryGetTarget(out var dictionary))
                    Apply(dictionary);
                else
                    Tracked.RemoveAt(i);
            }
        }

        private static void Apply(ResourceDictionary resources)
        {
            var background = Themed(EnvironmentColors.ToolWindowBackgroundColorKey);
            var text = Themed(EnvironmentColors.ToolWindowTextColorKey);
            var border = Themed(EnvironmentColors.ToolWindowBorderColorKey);
            var gray = Themed(EnvironmentColors.SystemGrayTextColorKey);
            var inputBackground = Themed(EnvironmentColors.ComboBoxBackgroundColorKey);
            var inputBorder = Themed(EnvironmentColors.ComboBoxBorderColorKey);

            Set(resources, "VsWindowBackground", background);
            Set(resources, "VsToolWindowBackground", background);
            Set(resources, "VsToolWindowHeader", Blend(background, text, 0.06));
            Set(resources, "VsToolWindowBorder", border);
            Set(resources, "VsToolWindowText", text);
            Set(resources, "VsGrayText", gray);
            Set(resources, "VsInputBackground", inputBackground);
            Set(resources, "VsInputBorder", inputBorder);
            Set(resources, "VsCanvasGridMajor", Blend(background, text, 0.10));
            Set(resources, "VsCanvasGridMinor", Blend(background, text, 0.05));

            // Node cards (FlowNodeControl resources)
            Set(resources, "VsNodeBackground", background);
            Set(resources, "VsNodeHeader", Blend(background, text, 0.06));
            Set(resources, "VsNodeBorder", border);
            Set(resources, "VsNodeText", text);
        }

        private static Color Themed(ThemeResourceKey key)
        {
            var c = VSColorTheme.GetThemedColor(key);
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        private static Color Blend(Color from, Color to, double amount) => Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));

        private static void Set(ResourceDictionary resources, string key, Color color)
        {
            // Mutate in place so StaticResource consumers that captured the brush update too.
            if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
                brush.Color = color;
            else if (resources.Contains(key))
                resources[key] = new SolidColorBrush(color);
        }
    }
}
