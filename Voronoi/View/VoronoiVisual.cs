using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using VoronoiApp.Algo.Primitives;
using Voronoi = VoronoiApp.Algo.Voronoi;

namespace VoronoiApp.View
{
    public class VoronoiVisual : FrameworkElement
    {
        VisualCollection _visuals;
        DrawingVisual _visual;
        Voronoi _voronoi;
        bool _showPoints;
        bool _showDelaunay;
        bool _showVoronoi;
        bool _showDiagnostics;

        Pen _diagnosticsRedPen;
        Brush _diagnosticsRedBrush;

        Pen _diagnosticsYellowPen;
        Brush _diagnosticsYellowBrush;

        Pen _lineDelaunayPen;
        Brush _dotDelaunayBrush;
        double _dotSize = 1.5;
        Pen _lineVoronoiPen;

        HashSet<Edge> _delaunayRenderEdges = new HashSet<Edge>();


        protected override int VisualChildrenCount
        {
            get { return _visuals.Count; }
        }



        public VoronoiVisual()
        {
            _diagnosticsRedPen = new Pen(Brushes.Red, 1);
            _diagnosticsRedPen.Freeze();
            _diagnosticsRedBrush = new SolidColorBrush(Colors.Red);
            _diagnosticsRedBrush.Freeze();

            _diagnosticsYellowPen = new Pen(Brushes.Yellow, 1);
            _diagnosticsYellowPen.Freeze();
            _diagnosticsYellowBrush = new SolidColorBrush(Colors.Yellow);
            _diagnosticsYellowBrush.Freeze();

            _lineDelaunayPen = new Pen(Brushes.DimGray, 0.5);
            _lineDelaunayPen.Freeze();

            _dotDelaunayBrush = new SolidColorBrush(Colors.Yellow);
            _dotDelaunayBrush.Freeze();

            _lineVoronoiPen = new Pen(Brushes.Magenta, 1.5);
            _lineVoronoiPen.Freeze();

            _visuals = new VisualCollection(this);

            Loaded += delegate(object s, RoutedEventArgs e)
            {
                _visual = new DrawingVisual();
                _visuals.Add(_visual);
            };
        }

        public void Visualize(Voronoi voronoi, bool showDiagnostics, bool showPoints, bool showDelaunay, bool showVoronoi)
        {
            _showDiagnostics = showDiagnostics;
            _showPoints = showPoints;
            _showDelaunay = showDelaunay;
            _showVoronoi = showVoronoi;
            _voronoi = voronoi;

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_visual == null)
                return;

            using (DrawingContext dc = _visual.RenderOpen())
            {
                if (_showDelaunay)
                    RenderDelaunay(dc);

                if (_showPoints)
                    RenderPointSet(dc);

                if (_showVoronoi)
                    RenderVoronoi(dc);

                if (_showDiagnostics)
                    RenderDiagnostics(dc);
            }
        }

        protected void RenderPointSet(DrawingContext dc)
        {
            // Render points of input dataset
            foreach (var p in _voronoi.Points)
                dc.DrawEllipse(_dotDelaunayBrush, null, p, _dotSize, _dotSize);
        }

        protected void RenderDiagnostics(DrawingContext dc)
        {
            // Render shapes used for diagnostics/debugging
            foreach (var geo in _voronoi.DiagnosticGeometry)
            {
                if (geo.Type == DiagGeometryType.Vertex)
                {
                    // Point
                    foreach (var p in geo.Vertices)
                        dc.DrawEllipse(GetDiagnosticsBrush(geo.Color), null, p, 2, 2);
                }
                else if (geo.Type == DiagGeometryType.Line)
                {
                    // Line
                    var start = geo.Vertices[0];
                    for (var i = 1; i < geo.Vertices.Length; i++)
                    {
                        var p = geo.Vertices[i];
                        dc.DrawLine(GetDiagnosticsPen(geo.Color), start, p);
                        start = p;
                    }
                }
                else if (geo.Type == DiagGeometryType.Circle)
                {
                    // Circle
                    var center = geo.Vertices[0];
                    var radius = geo.Vertices[1].X;
                    dc.DrawEllipse(null, GetDiagnosticsPen(geo.Color), geo.Vertices[0], radius, radius);
                }
            }
        }

        protected void RenderVoronoi(DrawingContext dc)
        {
            // Render Voronoi edges
            foreach (var vedge in _voronoi.VoronoiEdges)
                dc.DrawLine(_lineVoronoiPen, vedge.Start, vedge.End);
        }
        
        protected void RenderDelaunay(DrawingContext dc)
        {
            _delaunayRenderEdges.Clear();

            // Render Delaunay edges (only once)
            foreach (var tri in _voronoi.Delaunay)
            {
                _delaunayRenderEdges.Add(tri.GetEdgeA());
                _delaunayRenderEdges.Add(tri.GetEdgeB());
                _delaunayRenderEdges.Add(tri.GetEdgeC());
            }

            foreach (var edge in _delaunayRenderEdges)
                dc.DrawLine(_lineDelaunayPen, edge.Start, edge.End);
        }

        private Pen GetDiagnosticsPen(DiagColor color)
        {
            if (color == DiagColor.Yellow)
                return _diagnosticsYellowPen;

            return _diagnosticsRedPen;
        }

        private Brush GetDiagnosticsBrush(DiagColor color)
        {
            if (color == DiagColor.Yellow)
                return _diagnosticsYellowBrush;

            return _diagnosticsRedBrush;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
