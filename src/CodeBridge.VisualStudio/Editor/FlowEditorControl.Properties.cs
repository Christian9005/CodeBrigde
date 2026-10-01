#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CodeBridge.Flow;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>Properties panel for the current selection (block parameters, flow settings, connection details).</summary>
    public partial class FlowEditorControl
    {
        private DispatcherTimer? _commitTimer;

        private void UpdatePropertiesPanel()
        {
            if (PropertiesContainer == null)
                return;

            PropertiesContainer.Children.Clear();

            if (_selection.Count == 0 && _selectedConnectionId != null)
            {
                BuildConnectionProperties();
                return;
            }

            if (_selection.Count == 0)
            {
                BuildFlowProperties();
                return;
            }

            if (_selection.Count > 1)
            {
                AddTitle($"{_selection.Count} blocks selected");
                AddMeta("Drag to move them together. Ctrl+C / Ctrl+D copy or duplicate, Del removes them.");
                return;
            }

            BuildNodeProperties(_selection[0]);
        }

        // ---------------------------------------------------------------- flow (nothing selected)

        private void BuildFlowProperties()
        {
            AddTitle("Flow");
            AddMeta($"{_catalog.DisplayName}  •  {_document.Nodes.Count} blocks  •  {_document.Connections.Count} connections");
            AddDivider();

            AddLabel("Name");
            var name = new TextBox { Text = _document.Name, Height = 24, Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(4, 2, 4, 2) };
            StyleInput(name);
            name.TextChanged += (s, e) =>
            {
                _document.Name = name.Text;
                MarkDirty();
                ScheduleCommit();
            };
            PropertiesContainer.Children.Add(name);

            AddLabel("How to build a flow");
            var help = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                LineHeight = 17,
                Text = "• Drag a block from the Toolbox, double-click it, or double-click the canvas to search.\n" +
                       "• Drag from a port to another port to connect. Outputs feed inputs.\n" +
                       "• Ctrl+Z / Ctrl+Y undo and redo, Ctrl+C / V / D copy, paste and duplicate.\n" +
                       "• Mouse wheel zooms. Middle button, or Space + drag, pans.\n" +
                       "• Pick the board and port in the toolbar, upload the firmware once, then press Run."
            };
            help.SetResourceReference(TextBlock.ForegroundProperty, "VsGrayText");
            PropertiesContainer.Children.Add(help);
        }

        // ---------------------------------------------------------------- connection

        private void BuildConnectionProperties()
        {
            var connection = _document.Connections.FirstOrDefault(c => c.Id == _selectedConnectionId);
            if (connection == null)
            {
                BuildFlowProperties();
                return;
            }

            AddTitle("Connection");
            AddMeta($"{NodeTitle(connection.FromNodeId)}.{connection.FromPort}  →  {NodeTitle(connection.ToNodeId)}.{connection.ToPort}");
            AddDivider();

            var delete = new Button { Content = "Delete connection", Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left };
            delete.Click += (s, e) => DeleteSelection();
            PropertiesContainer.Children.Add(delete);
        }

        private string NodeTitle(string nodeId) =>
            _nodeControlMap.TryGetValue(nodeId, out var node) && node.Definition != null ? node.Definition.DisplayName : nodeId;

        // ---------------------------------------------------------------- block

        private void BuildNodeProperties(FlowNodeControl control)
        {
            var node = control.NodeData;
            var definition = control.Definition;

            AddTitle(definition?.DisplayName ?? node.Type);
            AddMeta($"{definition?.Category ?? "Unknown"}  •  {node.Id}");
            if (definition != null)
            {
                var description = new TextBlock { Text = BlockHelp.SummaryFor(definition), TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) };
                description.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
                PropertiesContainer.Children.Add(description);

                var tip = BlockHelp.Get(definition.Type)?.Tip;
                if (!string.IsNullOrEmpty(tip))
                {
                    var tipBlock = new TextBlock { Text = "Tip: " + tip, TextWrapping = TextWrapping.Wrap, FontSize = 10.5, FontStyle = FontStyles.Italic, Margin = new Thickness(0, 0, 0, 8) };
                    tipBlock.SetResourceReference(TextBlock.ForegroundProperty, "VsGrayText");
                    PropertiesContainer.Children.Add(tipBlock);
                }
            }

            AddDivider();

            if (definition == null)
            {
                AddMeta($"'{node.Type}' is not available for {_catalog.DisplayName}. The block stays in the file; change the board or delete it.");
                return;
            }

            if (definition.Properties.Count == 0)
            {
                AddMeta("This block has no parameters.");
                return;
            }

            var advanced = new StackPanel();
            foreach (var property in definition.Properties)
            {
                var target = property.IsAdvanced ? advanced : PropertiesContainer;
                AddPropertyEditor(target, control, property);
            }

            if (advanced.Children.Count > 0)
            {
                var expander = new Expander { Header = "Advanced", Content = advanced, Margin = new Thickness(0, 4, 0, 0) };
                expander.SetResourceReference(Control.ForegroundProperty, "VsToolWindowText");
                PropertiesContainer.Children.Add(expander);
            }
        }

        private void AddPropertyEditor(Panel target, FlowNodeControl control, FlowPropertyDefinition property)
        {
            var node = control.NodeData;
            var current = BuiltInBlockCatalog.GetParameterValue(node, property);

            var label = new TextBlock { Text = Humanize(property.Name) + (property.Required ? " *" : string.Empty), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
            target.Children.Add(label);

            if (property.Options != null && property.Options.Count > 0)
            {
                target.Children.Add(BuildOptionCombo(control, property, current));
            }
            else if (property.ValueKind == FlowValueKind.Boolean)
            {
                var check = new CheckBox { Content = "Enabled", IsChecked = SafeBool(current), Margin = new Thickness(0, 0, 0, 12) };
                check.SetResourceReference(Control.ForegroundProperty, "VsToolWindowText");
                check.Checked += (s, e) => SetParameter(control, property, true, immediate: true);
                check.Unchecked += (s, e) => SetParameter(control, property, false, immediate: true);
                target.Children.Add(check);
            }
            else
            {
                target.Children.Add(BuildTextEditor(control, property, current));
            }
        }

        private ComboBox BuildOptionCombo(FlowNodeControl control, FlowPropertyDefinition property, object? current)
        {
            var combo = new ComboBox { Height = 24, Margin = new Thickness(0, 0, 0, 12) };
            StyleInput(combo);

            foreach (var option in property.Options!)
            {
                combo.Items.Add(new ComboBoxItem { Content = option.DisplayName, Tag = option.Value, ToolTip = option.Description });
            }

            var selectedIndex = -1;
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (Equals(((ComboBoxItem)combo.Items[i]).Tag?.ToString(), current?.ToString()))
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex < 0 && current != null)
            {
                // The saved value does not exist on this board (for example a pin of another board): show it instead of hiding it.
                combo.Items.Insert(0, new ComboBoxItem { Content = $"{current} (not available on {_catalog.DisplayName})", Tag = current });
                selectedIndex = 0;
            }

            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : (combo.Items.Count > 0 ? 0 : -1);
            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedItem is ComboBoxItem item)
                    SetParameter(control, property, item.Tag, immediate: true);
            };
            return combo;
        }

        private TextBox BuildTextEditor(FlowNodeControl control, FlowPropertyDefinition property, object? current)
        {
            var box = new TextBox
            {
                Text = Convert.ToString(current, CultureInfo.InvariantCulture) ?? string.Empty,
                Height = 24,
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(4, 2, 4, 2)
            };
            StyleInput(box);

            box.TextChanged += (s, e) =>
            {
                object? value;
                var valid = true;
                switch (property.ValueKind)
                {
                    case FlowValueKind.Integer:
                        valid = int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer);
                        value = integer;
                        break;
                    case FlowValueKind.Number:
                        valid = double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number);
                        value = number;
                        break;
                    default:
                        value = box.Text;
                        break;
                }

                // Invalid numbers are flagged in red and never written to the document.
                box.BorderBrush = valid ? (Brush)FindResource("VsInputBorder") : Brushes.IndianRed;
                box.ToolTip = valid ? null : $"Enter a valid {(property.ValueKind == FlowValueKind.Integer ? "whole number" : "number")} (use '.' for decimals).";
                if (valid)
                    SetParameter(control, property, value, immediate: false);
            };
            return box;
        }

        private void SetParameter(FlowNodeControl control, FlowPropertyDefinition property, object? value, bool immediate)
        {
            control.NodeData.Parameters[property.Name] = value!;
            control.RefreshSummary();
            MarkDirty();

            if (immediate)
                Commit();
            else
                ScheduleCommit();
        }

        private void ScheduleCommit()
        {
            if (_commitTimer == null)
            {
                _commitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _commitTimer.Tick += (s, e) =>
                {
                    _commitTimer!.Stop();
                    Commit();
                };
            }

            _commitTimer.Stop();
            _commitTimer.Start();
        }

        // ---------------------------------------------------------------- building blocks

        private void AddTitle(string text)
        {
            var title = new TextBlock { Text = text, FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
            title.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
            PropertiesContainer.Children.Add(title);
        }

        private void AddMeta(string text)
        {
            var meta = new TextBlock { Text = text, FontSize = 10.5, Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap };
            meta.SetResourceReference(TextBlock.ForegroundProperty, "VsGrayText");
            PropertiesContainer.Children.Add(meta);
        }

        private void AddDivider()
        {
            var divider = new Border { Height = 1, Margin = new Thickness(0, 0, 0, 12) };
            divider.SetResourceReference(Border.BackgroundProperty, "VsToolWindowBorder");
            PropertiesContainer.Children.Add(divider);
        }

        private void AddLabel(string text)
        {
            var label = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "VsToolWindowText");
            PropertiesContainer.Children.Add(label);
        }

        private void StyleInput(Control input)
        {
            input.SetResourceReference(Control.BackgroundProperty, "VsInputBackground");
            input.SetResourceReference(Control.ForegroundProperty, "VsToolWindowText");
            input.SetResourceReference(Control.BorderBrushProperty, "VsInputBorder");
        }

        private static bool SafeBool(object? value)
        {
            try
            {
                return value != null && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>"intervalMs" becomes "Interval (ms)", "sampleRateHz" becomes "Sample Rate (Hz)".</summary>
        internal static string Humanize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var builder = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                    builder.Append(' ');

                builder.Append(i == 0 ? char.ToUpperInvariant(c) : c);
            }

            var text = builder.ToString();
            foreach (var pair in new[] { new[] { " Ms", " (ms)" }, new[] { " Us", " (µs)" }, new[] { " Hz", " (Hz)" } })
            {
                if (text.EndsWith(pair[0], StringComparison.Ordinal))
                    return text.Substring(0, text.Length - pair[0].Length) + pair[1];
            }

            return text;
        }
    }
}
