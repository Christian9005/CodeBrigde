#nullable enable
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>
    /// Hosts the canvas and provides wheel zoom (towards the cursor), pan (middle button, or Space + left button),
    /// zoom-to-fit and reset. Left-drag on the canvas is left to the editor (selection rectangle).
    /// </summary>
    public class ZoomBorder : Border
    {
        public const double MinZoom = 0.25;
        public const double MaxZoom = 2.5;

        private UIElement? _child;
        private Point _panStart;
        private Point _panOrigin;
        private bool _isPanning;
        private TranslateTransform _translate = new TranslateTransform();
        private ScaleTransform _scale = new ScaleTransform();
        private readonly TransformGroup _group = new TransformGroup();

        public event EventHandler? ZoomChanged;

        public ZoomBorder()
        {
            _group.Children.Add(_scale);
            _group.Children.Add(_translate);
            ClipToBounds = true;
            Focusable = true;
            MouseWheel += OnMouseWheel;
            PreviewMouseDown += OnPreviewMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            LostMouseCapture += (_, __) => EndPan();
        }

        public double Zoom => _scale.ScaleX;

        public bool IsPanning => _isPanning;

        public override UIElement? Child
        {
            get => base.Child;
            set
            {
                if (value != null && value != _child)
                {
                    _child = value;
                    base.Child = value;

                    value.RenderTransform = _group;
                    value.RenderTransformOrigin = new Point(0, 0);
                }
            }
        }

        /// <summary>Makes a tiled background brush follow the canvas pan and zoom so the grid looks infinite.</summary>
        public void AttachBackgroundTransform(Brush brush)
        {
            brush.Transform = _group;
        }

        public void ResetZoom()
        {
            _scale.ScaleX = 1.0;
            _scale.ScaleY = 1.0;
            _translate.X = 0;
            _translate.Y = 0;
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Multiplies the zoom around the centre of the viewport.</summary>
        public void ZoomBy(double factor) => ZoomAt(factor, new Point(ActualWidth / 2, ActualHeight / 2));

        /// <summary>Scales and centres so that <paramref name="canvasBounds"/> (canvas units) fills the viewport.</summary>
        public void FitTo(Rect canvasBounds)
        {
            if (canvasBounds.IsEmpty || ActualWidth < 10 || ActualHeight < 10)
            {
                ResetZoom();
                return;
            }

            const double margin = 60;
            var zoom = Math.Min(
                (ActualWidth - margin * 2) / Math.Max(canvasBounds.Width, 1),
                (ActualHeight - margin * 2) / Math.Max(canvasBounds.Height, 1));
            zoom = Math.Max(MinZoom, Math.Min(1.0, zoom));

            _scale.ScaleX = zoom;
            _scale.ScaleY = zoom;
            _translate.X = (ActualWidth - canvasBounds.Width * zoom) / 2 - canvasBounds.X * zoom;
            _translate.Y = (ActualHeight - canvasBounds.Height * zoom) / 2 - canvasBounds.Y * zoom;
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Converts a point in viewport (this control) coordinates to canvas coordinates.</summary>
        public Point ViewportToCanvas(Point viewportPoint) => new Point(
            (viewportPoint.X - _translate.X) / _scale.ScaleX,
            (viewportPoint.Y - _translate.Y) / _scale.ScaleY);

        public Point ViewportCenterInCanvas() => ViewportToCanvas(new Point(ActualWidth / 2, ActualHeight / 2));

        private void ZoomAt(double factor, Point viewportPoint)
        {
            var newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, _scale.ScaleX * factor));
            if (Math.Abs(newZoom - _scale.ScaleX) < 0.0001)
                return;

            var canvasPoint = ViewportToCanvas(viewportPoint);
            _scale.ScaleX = newZoom;
            _scale.ScaleY = newZoom;
            _translate.X = viewportPoint.X - canvasPoint.X * newZoom;
            _translate.Y = viewportPoint.Y - canvasPoint.Y * newZoom;
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_child == null)
                return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                _translate.X += e.Delta > 0 ? 60 : -60;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                _translate.Y += e.Delta > 0 ? 60 : -60;
            }
            else
            {
                ZoomAt(e.Delta > 0 ? 1.15 : 1 / 1.15, e.GetPosition(this));
            }

            e.Handled = true;
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var spacePan = e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space);
            if (e.ChangedButton != MouseButton.Middle && !spacePan)
                return;

            _isPanning = true;
            _panStart = e.GetPosition(this);
            _panOrigin = new Point(_translate.X, _translate.Y);
            Cursor = Cursors.SizeAll;
            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
                return;

            var position = e.GetPosition(this);
            _translate.X = _panOrigin.X + (position.X - _panStart.X);
            _translate.Y = _panOrigin.Y + (position.Y - _panStart.Y);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                ReleaseMouseCapture();
                EndPan();
            }
        }

        private void EndPan()
        {
            _isPanning = false;
            ClearValue(CursorProperty);
        }
    }
}
