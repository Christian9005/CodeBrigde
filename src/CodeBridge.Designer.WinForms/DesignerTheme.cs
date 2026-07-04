namespace CodeBridge.Designer.WinForms;

internal static class DesignerTheme
{
    public static readonly Color Workbench = Color.FromArgb(24, 25, 27);
    public static readonly Color Canvas = Color.FromArgb(12, 14, 18);
    public static readonly Color Surface = Color.FromArgb(29, 31, 35);
    public static readonly Color SurfaceRaised = Color.FromArgb(38, 41, 46);
    public static readonly Color SurfaceSunken = Color.FromArgb(18, 20, 24);
    public static readonly Color InputBackground = Color.FromArgb(21, 23, 27);
    public static readonly Color ButtonBackground = Color.FromArgb(43, 46, 52);
    public static readonly Color OutputBackground = Color.FromArgb(14, 16, 20);
    public static readonly Color SurfaceHover = Color.FromArgb(50, 54, 61);
    public static readonly Color SurfaceSelected = Color.FromArgb(28, 72, 122);
    public static readonly Color Border = Color.FromArgb(62, 66, 74);
    public static readonly Color BorderSubtle = Color.FromArgb(40, 43, 49);
    public static readonly Color GridMajor = Color.FromArgb(42, 46, 54);
    public static readonly Color GridMinor = Color.FromArgb(29, 33, 40);
    public static readonly Color Text = Color.FromArgb(236, 241, 248);
    public static readonly Color MutedText = Color.FromArgb(155, 164, 176);
    public static readonly Color DisabledText = Color.FromArgb(99, 107, 119);
    public static readonly Color PortText = Color.FromArgb(195, 204, 216);
    public static readonly Color Copper = Color.FromArgb(58, 130, 246);
    public static readonly Color AccentSoft = Color.FromArgb(101, 163, 255);
    public static readonly Color Signal = Color.FromArgb(0, 216, 143);
    public static readonly Color Warning = Color.FromArgb(245, 170, 48);
    public static readonly Color Error = Color.FromArgb(248, 81, 99);
    public static readonly Color Magenta = Color.FromArgb(194, 86, 255);

    public static Font UiFont { get; } = new("Segoe UI", 9F, FontStyle.Regular);
    public static Font SmallFont { get; } = new("Segoe UI", 8.25F, FontStyle.Regular);
    public static Font TitleFont { get; } = new("Segoe UI Semibold", 9.5F, FontStyle.Regular);
    public static Font SectionFont { get; } = new("Segoe UI Semibold", 8F, FontStyle.Regular);
    public static Font MonoFont { get; } = new("Consolas", 9F, FontStyle.Regular);
}
