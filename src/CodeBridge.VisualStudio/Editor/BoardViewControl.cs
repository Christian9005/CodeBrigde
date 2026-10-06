#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>
    /// A picture of the microcontroller board that shows what the flow is doing to it: output pins light up, PWM pins fill like a
    /// bar, servos show their angle, analog inputs show their reading and the built-in LED glows. It is fed by the host's "pin"
    /// messages, so it works the same with a real board and with the simulator.
    /// </summary>
    internal sealed class BoardViewControl : UserControl
    {
        private static readonly Color Green = Color.FromRgb(0, 216, 143);
        private static readonly Color Blue = Color.FromRgb(58, 130, 246);
        private static readonly Color Amber = Color.FromRgb(245, 170, 48);
        private static readonly Color Purple = Color.FromRgb(194, 86, 255);
        private static readonly Color Pink = Color.FromRgb(248, 113, 164);
        private static readonly Color Idle = Color.FromRgb(58, 62, 70);
        private const int MaxLogLines = 6;

        internal static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        private readonly TextBlock _title = new TextBlock { FontWeight = FontWeights.SemiBold, FontSize = 12 };
        private readonly TextBlock _source = new TextBlock { FontSize = 10, Margin = new Thickness(0, 1, 0, 0) };
        private readonly TextBlock _chipName = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap };
        private readonly Grid _pcb = new Grid();
        private readonly StackPanel _leftPins = new StackPanel();
        private readonly StackPanel _rightPins = new StackPanel();
        private readonly StackPanel _log = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        private readonly Ellipse _led = new Ellipse { Width = 14, Height = 14, Fill = new SolidColorBrush(Amber), Opacity = 0.12 };
        private readonly TextBlock _ledLabel = new TextBlock { FontSize = 9, Foreground = Brushes.White, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private readonly Dictionary<int, PinPad> _pads = new Dictionary<int, PinPad>();
        private readonly List<string> _lines = new List<string>();
        private int _ledPin = -1;
        private int _analogMax = 4095;
        private bool _pcbBuilt;

        public BoardViewControl()
        {
            _title.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
            _source.SetResourceReference(TextBlock.ForegroundProperty, "VsGrayText");

            _led.Effect = new DropShadowEffect { Color = Amber, BlurRadius = 14, ShadowDepth = 0, Opacity = 0 };

            var root = new StackPanel { Margin = new Thickness(10) };
            root.Children.Add(_title);
            root.Children.Add(_source);
            root.Children.Add(BuildPcb());
            root.Children.Add(_log);

            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = root
            };
        }

        private FrameworkElement BuildPcb()
        {
            var usb = new Border
            {
                Width = 34,
                Height = 12,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -4, 0, 6),
                ToolTip = "USB"
            };

            var ledRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(8, 0, 0, 6) };
            ledRow.Children.Add(_led);
            ledRow.Children.Add(_ledLabel);

            var chip = new Border
            {
                Width = 62,
                MinHeight = 64,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(24, 26, 31)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 74, 84)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0),
                Child = _chipName
            };

            _pcb.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _pcb.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _pcb.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _leftPins.HorizontalAlignment = HorizontalAlignment.Right;
            _rightPins.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(_leftPins, 0);
            Grid.SetColumn(chip, 1);
            Grid.SetColumn(_rightPins, 2);
            _pcb.Children.Add(_leftPins);
            _pcb.Children.Add(chip);
            _pcb.Children.Add(_rightPins);

            var body = new StackPanel();
            body.Children.Add(usb);
            body.Children.Add(ledRow);
            body.Children.Add(_pcb);

            return new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(14, 56, 44)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(31, 94, 74)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 10, 8, 10),
                Child = body
            };
        }

        /// <summary>Draws the board: its name, its pins (number and description), the ADC range and which pin owns the built-in LED.</summary>
        public void SetBoard(string title, string chip, IEnumerable<(int Pin, string Label)> pins, int ledPin, int analogMax)
        {
            _title.Text = title;
            _chipName.Text = chip;
            _ledPin = ledPin;
            _analogMax = Math.Max(1, analogMax);
            _ledLabel.Text = ledPin >= 0 ? $"built-in LED (pin {ledPin})" : string.Empty;
            _led.Visibility = ledPin >= 0 ? Visibility.Visible : Visibility.Collapsed;

            _pads.Clear();
            _leftPins.Children.Clear();
            _rightPins.Children.Clear();
            var ordered = pins.OrderBy(p => p.Pin).ToList();
            var half = (ordered.Count + 1) / 2;
            for (var i = 0; i < ordered.Count; i++)
            {
                var pad = AddPad(ordered[i].Pin, ordered[i].Label, left: i < half);
                _pads[ordered[i].Pin] = pad;
            }

            _pcbBuilt = true;
            Reset();
        }

        public void SetSource(string text) => _source.Text = text;

        /// <summary>Everything back to idle (a new run starts).</summary>
        public void Reset()
        {
            foreach (var pad in _pads.Values)
                pad.Clear();

            SetLed(0);
            _lines.Clear();
            _log.Children.Clear();
        }

        /// <summary>Shows what happened on a pin. Kinds are the ones the host reports: mode, digital, input, analog, pwm, servo, servo-off, tone.</summary>
        public void Apply(int pin, string kind, double value)
        {
            if (!_pcbBuilt)
                return;

            if (!_pads.TryGetValue(pin, out var pad))
            {
                // a pin the catalog hides (reserved ones) still gets a pad when a flow touches it
                pad = AddPad(pin, "GPIO " + pin.ToString(CultureInfo.InvariantCulture), left: _pads.Count % 2 == 0);
                _pads[pin] = pad;
            }

            pad.Show(kind, value, _analogMax);
            if (pin == _ledPin)
                UpdateLedFrom(kind, value);

            AddLog(pin, kind, value);
        }

        // -------------------------------------------------------------- built-in LED

        private void UpdateLedFrom(string kind, double value)
        {
            if (kind == "digital")
                SetLed(value != 0 ? 1.0 : 0.0);
            else if (kind == "pwm")
                SetLed(BoardViewControl.Clamp(value / 255.0, 0, 1));
        }

        private void SetLed(double brightness)
        {
            _led.Opacity = 0.12 + 0.88 * brightness;
            if (_led.Effect is DropShadowEffect glow)
                glow.Opacity = brightness;
        }

        internal double LedBrightness => (_led.Opacity - 0.12) / 0.88;

        // -------------------------------------------------------------- activity log

        private void AddLog(int pin, string kind, double value)
        {
            var text = Describe(pin, kind, value);
            if (text == null)
                return;

            _lines.Insert(0, text);
            if (_lines.Count > MaxLogLines)
                _lines.RemoveAt(_lines.Count - 1);

            _log.Children.Clear();
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = new TextBlock { Text = _lines[i], FontSize = 10, FontFamily = new FontFamily("Consolas"), Opacity = i == 0 ? 1.0 : 0.65 };
                line.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
                _log.Children.Add(line);
            }
        }

        internal static string? Describe(int pin, string kind, double value)
        {
            var name = "GPIO " + pin.ToString(CultureInfo.InvariantCulture);
            switch (kind)
            {
                case "mode": return $"{name}: set as {ModeName((int)value)}";
                case "digital": return $"{name} → {(value != 0 ? "HIGH" : "LOW")}";
                case "input": return $"{name} reads {(value != 0 ? "HIGH" : "LOW")}";
                case "analog": return $"{name} reads {value.ToString("0", CultureInfo.InvariantCulture)}";
                case "pwm": return $"{name} PWM {value.ToString("0", CultureInfo.InvariantCulture)}/255";
                case "servo": return $"{name} servo → {value.ToString("0", CultureInfo.InvariantCulture)}°";
                case "servo-off": return $"{name} servo released";
                case "tone": return value > 0 ? $"{name} tone {value.ToString("0", CultureInfo.InvariantCulture)} Hz" : $"{name} tone off";
                default: return null;
            }
        }

        internal static string ModeName(int mode)
        {
            switch (mode)
            {
                case 0: return "input";
                case 1: return "output";
                case 2: return "input (pull-up)";
                case 3: return "input (pull-down)";
                case 4: return "analog";
                default: return "mode " + mode.ToString(CultureInfo.InvariantCulture);
            }
        }

        // -------------------------------------------------------------- pads

        internal string PadText(int pin) => _pads.TryGetValue(pin, out var pad) ? pad.ValueText : string.Empty;

        internal string PadKind(int pin) => _pads.TryGetValue(pin, out var pad) ? pad.Kind : string.Empty;

        internal double PadFill(int pin) => _pads.TryGetValue(pin, out var pad) ? pad.FillRatio : 0;

        internal int PadCount => _pads.Count;

        private PinPad AddPad(int pin, string label, bool left)
        {
            var pad = new PinPad(pin, label);
            var number = new TextBlock
            {
                Text = pin.ToString(CultureInfo.InvariantCulture),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 226, 214)),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 20,
                TextAlignment = left ? TextAlignment.Right : TextAlignment.Left,
                Margin = left ? new Thickness(0, 0, 4, 0) : new Thickness(4, 0, 0, 0),
                ToolTip = label
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            if (left)
            {
                row.Children.Add(number);
                row.Children.Add(pad);
            }
            else
            {
                row.Children.Add(pad);
                row.Children.Add(number);
            }

            (left ? _leftPins : _rightPins).Children.Add(row);
            return pad;
        }

        /// <summary>One pin of the board: a small pad that changes colour and fill with what happens on it.</summary>
        private sealed class PinPad : Border
        {
            private readonly Rectangle _bar = new Rectangle { HorizontalAlignment = HorizontalAlignment.Left, Width = 0, RadiusX = 2, RadiusY = 2 };
            private readonly TextBlock _text = new TextBlock { FontSize = 9, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.White };
            private readonly Border _flash = new Border { Background = Brushes.White, Opacity = 0, CornerRadius = new CornerRadius(3), IsHitTestVisible = false };
            private readonly string _label;
            private const double PadWidth = 52;

            public PinPad(int pin, string label)
            {
                Pin = pin;
                _label = label;
                Width = PadWidth;
                Height = 16;
                CornerRadius = new CornerRadius(3);
                Background = new SolidColorBrush(Idle);
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 106));
                BorderThickness = new Thickness(1);
                ClipToBounds = true;

                var grid = new Grid();
                grid.Children.Add(_bar);
                grid.Children.Add(_text);
                grid.Children.Add(_flash);
                Child = grid;
                Clear();
            }

            public int Pin { get; }

            public string ValueText => _text.Text;

            public string Kind { get; private set; } = string.Empty;

            public double FillRatio { get; private set; }

            private bool _output;

            public void Clear()
            {
                Kind = string.Empty;
                FillRatio = 0;
                _output = false;
                _bar.Width = 0;
                _text.Text = string.Empty;
                BorderBrush = new SolidColorBrush(Color.FromRgb(88, 94, 106));
                ToolTip = _label;
            }

            public void Show(string kind, double value, int analogMax)
            {
                switch (kind)
                {
                    case "mode":
                        _output = (int)value == 1;
                        BorderBrush = new SolidColorBrush(_output ? Green : Blue);
                        if (Kind == string.Empty)
                            _text.Text = _output ? "OUT" : "IN";
                        break;

                    case "digital":
                        Fill(Green, value != 0 ? 1.0 : 0.0, value != 0 ? "HIGH" : "LOW");
                        break;

                    case "input":
                        Fill(Blue, value != 0 ? 1.0 : 0.0, value != 0 ? "IN 1" : "IN 0");
                        break;

                    case "analog":
                        Fill(Blue, BoardViewControl.Clamp(value / analogMax, 0, 1), value.ToString("0", CultureInfo.InvariantCulture));
                        break;

                    case "pwm":
                        Fill(Amber, BoardViewControl.Clamp(value / 255.0, 0, 1), value.ToString("0", CultureInfo.InvariantCulture));
                        break;

                    case "servo":
                        Fill(Purple, BoardViewControl.Clamp(value / 180.0, 0, 1), value.ToString("0", CultureInfo.InvariantCulture) + "°");
                        break;

                    case "servo-off":
                        Fill(Purple, 0, "off");
                        break;

                    case "tone":
                        Fill(Pink, value > 0 ? 1.0 : 0.0, value > 0 ? "♪" : "off");
                        break;

                    default:
                        return;
                }

                if (kind != "mode")
                {
                    Kind = kind;
                    ToolTip = $"{_label}\n{BoardViewControl.Describe(Pin, kind, value)}";
                    Pulse();
                }
            }

            private void Fill(Color color, double ratio, string text)
            {
                FillRatio = ratio;
                _bar.Fill = new SolidColorBrush(color);
                _bar.Width = Math.Max(0, (PadWidth - 2) * ratio);
                _text.Text = text;
                BorderBrush = new SolidColorBrush(color);
            }

            /// <summary>A quick flash so activity is visible even when the value did not change.</summary>
            private void Pulse()
            {
                try
                {
                    _flash.BeginAnimation(OpacityProperty, new DoubleAnimation(0.55, 0, TimeSpan.FromMilliseconds(500)));
                }
                catch (InvalidOperationException)
                {
                    // animations are not available (headless tests)
                }
            }
        }
    }
}
