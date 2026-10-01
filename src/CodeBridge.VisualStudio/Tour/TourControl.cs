#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CodeBridge.Flow;
using CodeBridge.VisualStudio.Editor;

namespace CodeBridge.VisualStudio.Tour
{
    /// <summary>
    /// The CodeBridge Tour: a story in chapters with animated block explanations, an "open this example" button and
    /// the C# equivalent of every example. Built in code so it follows the Visual Studio theme like the flow editor.
    /// </summary>
    internal sealed class TourControl : UserControl
    {
        private static readonly Color Accent = Color.FromRgb(0, 122, 204);

        private readonly EditorSettings _settings;
        private readonly StackPanel _chapterList = new StackPanel();
        private readonly StackPanel _content = new StackPanel { MaxWidth = 780, Margin = new Thickness(32, 24, 32, 40), HorizontalAlignment = HorizontalAlignment.Left };
        private readonly ScrollViewer _scroll;
        private readonly List<BlockDemoView> _demos = new List<BlockDemoView>();
        private readonly List<Ellipse> _travellers = new List<Ellipse>();
        private int _index = -1;

        public event Action<TourChapter>? OpenExampleRequested;
        public event Action<string, string>? OpenCodeRequested;

        public TourControl() : this(EditorSettings.Load())
        {
        }

        internal TourControl(EditorSettings settings)
        {
            _settings = settings;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            Focusable = true;

            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CodeBridge.VisualStudio;component/Editor/EditorTheme.xaml", UriKind.Relative) });
            AddBrush("VsWindowBackground", "#1e1e1e");
            AddBrush("VsToolWindowBackground", "#252526");
            AddBrush("VsToolWindowHeader", "#2d2d30");
            AddBrush("VsToolWindowBorder", "#3f3f46");
            AddBrush("VsToolWindowText", "#f1f1f1");
            AddBrush("VsGrayText", "#999999");
            AddBrush("VsInputBackground", "#333337");
            AddBrush("VsInputBorder", "#434346");
            AddBrush("VsHighlight", "#007acc");
            VsTheme.Bind(Resources);
            SetResourceReference(BackgroundProperty, "VsWindowBackground");

            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var side = new Border { BorderThickness = new Thickness(0, 0, 1, 0), Padding = new Thickness(0, 14, 0, 14) };
            side.SetResourceReference(Border.BackgroundProperty, "VsToolWindowBackground");
            side.SetResourceReference(Border.BorderBrushProperty, "VsToolWindowBorder");
            var sideStack = new StackPanel();
            sideStack.Children.Add(Themed(new TextBlock { Text = "CodeBridge Tour", FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(18, 0, 18, 2) }, "VsToolWindowText"));
            sideStack.Children.Add(Themed(new TextBlock { Text = "From a blinking LED to your own services", FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(18, 0, 18, 14) }, "VsGrayText"));
            sideStack.Children.Add(_chapterList);
            side.Child = sideStack;
            Grid.SetColumn(side, 0);
            root.Children.Add(side);

            _scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _content };
            Grid.SetColumn(_scroll, 1);
            root.Children.Add(_scroll);
            Content = root;

            BuildChapterList();
            Loaded += (_, __) =>
            {
                if (_index < 0)
                    ShowChapter(Math.Max(0, Math.Min(_settings.TourLastChapter, TourContent.Chapters.Count - 1)));
                else
                    StartAnimations();
            };
            Unloaded += (_, __) => StopAnimations();
        }

        public int CurrentChapter => _index;

        // ------------------------------------------------------------------ chapter list

        private void BuildChapterList()
        {
            _chapterList.Children.Clear();
            for (var i = 0; i < TourContent.Chapters.Count; i++)
            {
                var index = i;
                var chapter = TourContent.Chapters[i];
                var selected = i == _index;
                var visited = _settings.TourVisited.Contains(i);

                var badge = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush(selected ? Accent : Color.FromArgb(60, 140, 140, 140)),
                    Child = new TextBlock
                    {
                        Text = visited && !selected ? "✓" : i.ToString(),
                        Foreground = selected ? Brushes.White : (Brush)FindResource("VsToolWindowText"),
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                texts.Children.Add(Themed(new TextBlock { Text = chapter.Title, FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal, TextTrimming = TextTrimming.CharacterEllipsis }, "VsToolWindowText"));
                texts.Children.Add(Themed(new TextBlock { Text = chapter.Level, FontSize = 10.5 }, "VsGrayText"));

                var row = new DockPanel { LastChildFill = true };
                row.Children.Add(badge);
                row.Children.Add(texts);

                var item = new Border { Padding = new Thickness(18, 8, 12, 8), Cursor = Cursors.Hand, Child = row, Background = selected ? new SolidColorBrush(Color.FromArgb(38, 0, 122, 204)) : Brushes.Transparent };
                item.MouseEnter += (s, e) => { if (index != _index) item.Background = new SolidColorBrush(Color.FromArgb(24, 140, 140, 140)); };
                item.MouseLeave += (s, e) => { if (index != _index) item.Background = Brushes.Transparent; };
                item.MouseLeftButtonUp += (s, e) => ShowChapter(index);
                _chapterList.Children.Add(item);
            }
        }

        // ------------------------------------------------------------------ chapter page

        public void ShowChapter(int index)
        {
            if (index < 0 || index >= TourContent.Chapters.Count)
                return;

            StopAnimations();
            _index = index;
            _settings.TourVisited.Add(index);
            _settings.TourLastChapter = index;
            _settings.Save();

            var chapter = TourContent.Chapters[index];
            _content.Children.Clear();
            _demos.Clear();
            _travellers.Clear();

            _content.Children.Add(Pill(index == 0 ? "WELCOME" : $"CHAPTER {index}  ·  {chapter.Level.ToUpperInvariant()}"));
            _content.Children.Add(Themed(new TextBlock { Text = chapter.Title, FontSize = 28, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 2), TextWrapping = TextWrapping.Wrap }, "VsToolWindowText"));
            _content.Children.Add(Themed(new TextBlock { Text = chapter.Subtitle, FontSize = 15, Margin = new Thickness(0, 0, 0, 16), TextWrapping = TextWrapping.Wrap }, "VsGrayText"));

            foreach (var paragraph in chapter.Story)
                _content.Children.Add(Themed(new TextBlock { Text = paragraph, TextWrapping = TextWrapping.Wrap, LineHeight = 21, Margin = new Thickness(0, 0, 0, 10) }, "VsToolWindowText"));

            if (chapter.Diagram != DiagramKind.None)
                _content.Children.Add(BuildDiagram(chapter.Diagram));

            if (chapter.Demos.Length > 0)
                _content.Children.Add(BuildDemoStrip(chapter));

            if (chapter.Steps.Length > 0)
                _content.Children.Add(BuildSteps(chapter));

            _content.Children.Add(BuildActions(chapter, index));

            if (chapter.Code.Length > 0)
                _content.Children.Add(BuildCode(chapter));

            BuildChapterList();
            _scroll.ScrollToTop();
            StartAnimations();
        }

        private FrameworkElement BuildDemoStrip(TourChapter chapter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
            panel.Children.Add(Section("Meet the blocks"));
            var wrap = new WrapPanel();
            var catalog = FlowCatalog.ForBoard("esp32-devkit");

            foreach (var kind in chapter.Demos)
            {
                var type = BlockHelp.KnownTypes.FirstOrDefault(t => BlockHelp.Get(t)?.Demo == kind);
                var title = type == null ? kind.ToString() : (catalog.Get(type)?.DisplayName ?? kind.ToString());
                var summary = type == null ? string.Empty : BlockHelp.Get(type)!.Summary;

                var demo = new BlockDemoView(kind, (Brush)FindResource("VsToolWindowText"), (Brush)FindResource("VsGrayText"), (Brush)FindResource("VsToolWindowBorder"));
                _demos.Add(demo);

                var card = new StackPanel { Width = BlockDemoView.DemoWidth };
                card.Children.Add(Themed(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) }, "VsToolWindowText"));
                var frame = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Child = demo };
                frame.SetResourceReference(Border.BorderBrushProperty, "VsToolWindowBorder");
                frame.SetResourceReference(Border.BackgroundProperty, "VsToolWindowHeader");
                card.Children.Add(frame);
                card.Children.Add(Themed(new TextBlock { Text = summary, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) }, "VsGrayText"));

                var host = new Border { Margin = new Thickness(0, 0, 18, 12), Child = card };
                wrap.Children.Add(host);
            }

            panel.Children.Add(wrap);
            return panel;
        }

        private FrameworkElement BuildSteps(TourChapter chapter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
            panel.Children.Add(Section(chapter.Diagram == DiagramKind.HowItFits ? "Get ready" : "Try it"));

            for (var i = 0; i < chapter.Steps.Length; i++)
            {
                var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
                var number = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Accent) };
                number.Child = new TextBlock { Text = (i + 1).ToString(), Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(number);
                row.Children.Add(Themed(new TextBlock { Text = chapter.Steps[i], TextWrapping = TextWrapping.Wrap, LineHeight = 20, VerticalAlignment = VerticalAlignment.Center }, "VsToolWindowText"));
                panel.Children.Add(row);
            }

            return panel;
        }

        private FrameworkElement BuildActions(TourChapter chapter, int index)
        {
            var row = new WrapPanel { Margin = new Thickness(0, 14, 0, 6) };

            if (chapter.ExampleFolder != null)
            {
                var open = new Button { Content = "Open this example in my project", Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(0, 0, 10, 0), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Accent), Background = new SolidColorBrush(Accent), FontWeight = FontWeights.SemiBold };
                open.Click += (s, e) => OpenExampleRequested?.Invoke(chapter);
                row.Children.Add(open);
            }

            if (index > 0)
            {
                var previous = new Button { Content = "← Previous", Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(0, 0, 10, 0) };
                previous.Click += (s, e) => ShowChapter(index - 1);
                row.Children.Add(previous);
            }

            if (index < TourContent.Chapters.Count - 1)
            {
                var next = new Button { Content = "Next: " + TourContent.Chapters[index + 1].Title + " →", Padding = new Thickness(14, 7, 14, 7) };
                next.Click += (s, e) => ShowChapter(index + 1);
                row.Children.Add(next);
            }

            return row;
        }

        // ------------------------------------------------------------------ code viewer

        private FrameworkElement BuildCode(TourChapter chapter)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
            panel.Children.Add(Section("The same thing in C#"));

            var tabs = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            var viewer = new RichTextBox { IsReadOnly = true, FontFamily = new FontFamily("Consolas"), FontSize = 12, Padding = new Thickness(12), BorderThickness = new Thickness(1), MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
            viewer.SetResourceReference(Control.BackgroundProperty, "VsInputBackground");
            viewer.SetResourceReference(Control.BorderBrushProperty, "VsToolWindowBorder");
            viewer.SetResourceReference(Control.ForegroundProperty, "VsToolWindowText");

            var current = chapter.Code[0];
            var tabButtons = new List<Border>();

            void Select(int tabIndex)
            {
                current = chapter.Code[tabIndex];
                for (var i = 0; i < tabButtons.Count; i++)
                {
                    var active = i == tabIndex;
                    tabButtons[i].Background = active ? new SolidColorBrush(Accent) : Brushes.Transparent;
                    ((TextBlock)tabButtons[i].Child).Foreground = active ? Brushes.White : (Brush)FindResource("VsToolWindowText");
                }

                viewer.Document = Highlight(TourContent.ReadCode(current));
            }

            for (var i = 0; i < chapter.Code.Length; i++)
            {
                var tabIndex = i;
                var tab = new Border { Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 4, 0), CornerRadius = new CornerRadius(3), Cursor = Cursors.Hand, BorderThickness = new Thickness(1), Child = new TextBlock { Text = chapter.Code[i].Title, FontSize = 12 } };
                tab.SetResourceReference(Border.BorderBrushProperty, "VsToolWindowBorder");
                tab.MouseLeftButtonUp += (s, e) => Select(tabIndex);
                tabButtons.Add(tab);
                tabs.Children.Add(tab);
            }

            panel.Children.Add(tabs);
            panel.Children.Add(viewer);

            var buttons = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            var copy = new Button { Content = "Copy code", Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
            copy.Click += (s, e) =>
            {
                try { Clipboard.SetText(TourContent.ReadCode(current)); copy.Content = "Copied"; }
                catch (System.Runtime.InteropServices.ExternalException) { copy.Content = "Clipboard busy"; }
            };
            var open = new Button { Content = "Open in editor", Padding = new Thickness(14, 5, 14, 5) };
            open.Click += (s, e) => OpenCodeRequested?.Invoke(current.FileName, TourContent.ReadCode(current));
            buttons.Children.Add(copy);
            buttons.Children.Add(open);
            panel.Children.Add(buttons);

            Select(0);
            return panel;
        }

        private static readonly Regex Token = new Regex(
            @"(?<comment>//[^\r\n]*)|(?<string>\$?""(?:[^""\\]|\\.)*"")|(?<number>\b\d+(?:\.\d+)?\b)|(?<word>[A-Za-z_]\w*)",
            RegexOptions.Compiled);

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "using", "namespace", "public", "private", "internal", "static", "sealed", "class", "async", "await", "var", "const", "new", "return",
            "if", "else", "while", "for", "foreach", "in", "try", "catch", "finally", "throw", "null", "true", "false", "this", "void", "int", "string",
            "bool", "double", "is", "not", "or", "and", "out", "ref", "override", "get", "set", "readonly", "break", "continue", "default"
        };

        private System.Windows.Documents.FlowDocument Highlight(string code)
        {
            var background = ((SolidColorBrush)FindResource("VsToolWindowBackground")).Color;
            var dark = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) < 140;
            Brush text = (Brush)FindResource("VsToolWindowText");
            Brush comment = new SolidColorBrush(dark ? Color.FromRgb(106, 153, 85) : Color.FromRgb(0, 128, 0));
            Brush str = new SolidColorBrush(dark ? Color.FromRgb(214, 157, 133) : Color.FromRgb(163, 21, 21));
            Brush keyword = new SolidColorBrush(dark ? Color.FromRgb(86, 156, 214) : Color.FromRgb(0, 0, 255));
            Brush number = new SolidColorBrush(dark ? Color.FromRgb(181, 206, 168) : Color.FromRgb(9, 134, 88));
            Brush type = new SolidColorBrush(dark ? Color.FromRgb(78, 201, 176) : Color.FromRgb(43, 145, 175));

            var paragraph = new Paragraph { Margin = new Thickness(0) };
            var position = 0;
            foreach (Match match in Token.Matches(code))
            {
                if (match.Index > position)
                    paragraph.Inlines.Add(new Run(code.Substring(position, match.Index - position)) { Foreground = text });

                Brush brush = text;
                if (match.Groups["comment"].Success) brush = comment;
                else if (match.Groups["string"].Success) brush = str;
                else if (match.Groups["number"].Success) brush = number;
                else if (Keywords.Contains(match.Value)) brush = keyword;
                else if (char.IsUpper(match.Value[0]) && match.Value.Length > 2 && !match.Value.EndsWith("Async", StringComparison.Ordinal)) brush = type;

                paragraph.Inlines.Add(new Run(match.Value) { Foreground = brush });
                position = match.Index + match.Length;
            }

            if (position < code.Length)
                paragraph.Inlines.Add(new Run(code.Substring(position)) { Foreground = text });

            return new System.Windows.Documents.FlowDocument(paragraph) { PageWidth = 2000 };
        }

        // ------------------------------------------------------------------ diagrams

        private FrameworkElement BuildDiagram(DiagramKind kind)
        {
            var canvas = new Canvas { Width = 740, Height = kind == DiagramKind.AnyHost ? 190 : 120, Margin = new Thickness(0, 10, 0, 14), HorizontalAlignment = HorizontalAlignment.Left };

            if (kind == DiagramKind.HowItFits)
            {
                Box(canvas, 0, 24, 190, 72, "Visual Studio", ".cbflow designer · your C# code", Color.FromRgb(0, 122, 204));
                Arrow(canvas, 190, 60, 270, "USB or Wi-Fi");
                Box(canvas, 270, 24, 200, 72, "ESP32 / Arduino", "CodeBridge firmware", Color.FromRgb(0, 168, 112));
                Arrow(canvas, 470, 60, 540, "pins");
                Box(canvas, 540, 24, 200, 72, "The real world", "LEDs · sensors · servos · motors", Color.FromRgb(245, 170, 48));
                Traveller(canvas, 195, 60, 265, Color.FromRgb(194, 86, 255));
                Traveller(canvas, 475, 60, 535, Color.FromRgb(194, 86, 255));
            }
            else
            {
                var hosts = new[] { "Visual Studio designer (.cbflow)", "Console app", "ASP.NET web API", "Windows service", "WinForms app" };
                for (var i = 0; i < hosts.Length; i++)
                {
                    Box(canvas, 0, i * 38, 230, 30, hosts[i], null, i == 0 ? Color.FromRgb(0, 122, 204) : Color.FromRgb(104, 91, 190));
                    var connector = new Line { X1 = 232, Y1 = i * 38 + 15, X2 = 298, Y2 = 94, StrokeThickness = 1.5, Opacity = 0.6 };
                    connector.SetResourceReference(Shape.StrokeProperty, "VsGrayText");
                    canvas.Children.Insert(0, connector);
                    Traveller(canvas, 236, i * 38 + 15, 292, Color.FromRgb(194, 86, 255), delay: i * 0.25);
                }

                Box(canvas, 300, 40, 190, 108, "CodeBridge SDK", "NuGet: CodeBridge.ESP32", Color.FromRgb(0, 122, 204));
                Arrow(canvas, 490, 94, 560, "USB / Wi-Fi");
                Box(canvas, 560, 54, 180, 80, "ESP32 / Arduino", "same firmware", Color.FromRgb(0, 168, 112));
            }

            return canvas;
        }

        private void Box(Canvas canvas, double x, double y, double w, double h, string title, string? subtitle, Color color)
        {
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 12.5, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
            if (subtitle != null)
                stack.Children.Add(new TextBlock { Text = subtitle, Foreground = new SolidColorBrush(Color.FromArgb(215, 255, 255, 255)), FontSize = 10.5, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6, 3, 6, 0) });

            var box = new Border { Width = w, Height = h, CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(color), Child = stack };
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            canvas.Children.Add(box);
        }

        private void Arrow(Canvas canvas, double x1, double y, double x2, string label)
        {
            var line = new Path { Data = Geometry.Parse($"M{x1 + 6},{y} L{x2 - 6},{y} M{x2 - 14},{y - 5} L{x2 - 6},{y} L{x2 - 14},{y + 5}"), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
            line.SetResourceReference(Shape.StrokeProperty, "VsGrayText");
            canvas.Children.Add(line);

            var text = Themed(new TextBlock { Text = label, FontSize = 10.5, Width = x2 - x1, TextAlignment = TextAlignment.Center }, "VsGrayText");
            Canvas.SetLeft(text, x1);
            Canvas.SetTop(text, y + 8);
            canvas.Children.Add(text);
        }

        private void Traveller(Canvas canvas, double x1, double y, double x2, Color color, double delay = 0)
        {
            var dot = new Ellipse { Width = 9, Height = 9, Fill = new SolidColorBrush(color) };
            Canvas.SetLeft(dot, x1);
            Canvas.SetTop(dot, y - 4.5);
            dot.Tag = new[] { x1, x2, delay };
            canvas.Children.Add(dot);
            _travellers.Add(dot);
        }

        // ------------------------------------------------------------------ animations

        private void StartAnimations()
        {
            foreach (var demo in _demos)
                demo.Start();

            foreach (var dot in _travellers)
            {
                var span = (double[])dot.Tag;
                var animation = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
                animation.BeginTime = TimeSpan.FromSeconds(span[2]);
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(span[0], KeyTime.FromTimeSpan(TimeSpan.Zero)));
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(span[1], KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.4))));
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(span[0], KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.4))));
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(span[0], KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0))));
                dot.BeginAnimation(Canvas.LeftProperty, animation);
            }
        }

        private void StopAnimations()
        {
            foreach (var demo in _demos)
                demo.Stop();

            foreach (var dot in _travellers)
                dot.BeginAnimation(Canvas.LeftProperty, null);
        }

        // ------------------------------------------------------------------ small helpers

        private void AddBrush(string key, string hex) =>
            Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static T Themed<T>(T element, string brushKey) where T : FrameworkElement
        {
            if (element is TextBlock block)
                block.SetResourceReference(TextBlock.ForegroundProperty, brushKey);

            return element;
        }

        private FrameworkElement Section(string text)
        {
            var heading = Themed(new TextBlock { Text = text, FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8) }, "VsToolWindowText");
            return heading;
        }

        private static FrameworkElement Pill(string text) =>
            new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 2, 10, 3),
                Background = new SolidColorBrush(Color.FromArgb(48, 0, 122, 204)),
                Child = new TextBlock { Text = text, FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(70, 160, 230)) }
            };
    }
}
