#nullable enable
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>A bezier cable between two ports. Hit testing is widened so a 2px wire is easy to click.</summary>
    public class FlowConnectionPath : Shape
    {
        public static readonly DependencyProperty StartPointProperty = DependencyProperty.Register("StartPoint", typeof(Point), typeof(FlowConnectionPath), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty EndPointProperty = DependencyProperty.Register("EndPoint", typeof(Point), typeof(FlowConnectionPath), new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly Pen HitPen = CreateHitPen();

        public Point StartPoint { get => (Point)GetValue(StartPointProperty); set => SetValue(StartPointProperty, value); }
        public Point EndPoint { get => (Point)GetValue(EndPointProperty); set => SetValue(EndPointProperty, value); }

        /// <summary>Id of the <c>FlowConnection</c> this cable draws (empty for the drag preview).</summary>
        public string ConnectionId { get; set; } = string.Empty;

        protected override Geometry DefiningGeometry
        {
            get
            {
                var geometry = new StreamGeometry();
                using (var context = geometry.Open())
                {
                    context.BeginFigure(StartPoint, false, false);
                    var dx = EndPoint.X - StartPoint.X;
                    // Forward wires bend proportionally to the gap; backward wires get a tighter loop instead of a huge S.
                    double offset = dx >= 0 ? Math.Max(30, dx / 2) : Math.Min(80, 30 + Math.Abs(dx) / 4);
                    context.BezierTo(
                        new Point(StartPoint.X + offset, StartPoint.Y),
                        new Point(EndPoint.X - offset, EndPoint.Y),
                        EndPoint, true, false);
                }

                geometry.Freeze();
                return geometry;
            }
        }

        protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        {
            var widened = DefiningGeometry.GetWidenedPathGeometry(HitPen);
            return widened.FillContains(hitTestParameters.HitPoint) ? new PointHitTestResult(this, hitTestParameters.HitPoint) : null;
        }

        private static Pen CreateHitPen()
        {
            var pen = new Pen(Brushes.Black, 12);
            pen.Freeze();
            return pen;
        }
    }
}
