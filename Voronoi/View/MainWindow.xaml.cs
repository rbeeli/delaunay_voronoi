using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VoronoiApp.Algo;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp.View
{
    public partial class MainWindow : Window
    {
        Stopwatch _watch = Stopwatch.StartNew();
        double _lastRenderTime;
        bool _showPoints = true;
        bool _showDelaunay = true;
        bool _showVoronoi;
        bool _showDiagnostics;
        int _delaunayMethod = 1; // 0 = incremental, 1 = sweep-circle

        List<MovingPoint> _points;

        Point _lastMovePoint;
        double _lastMoveSec;
        double _lastMoveElapsed;
        double _lastMoveDistance;
        Vector _lastMoveDirection;
        MovingPoint _lastCursorPoint;
        int _lastCursorPointIdx;



        public MainWindow()
        {
            InitializeComponent();

            KeyUp += OnKeyUp;

            VoronoiVisual.MouseLeftButtonUp += OnMouseLeftButtonUp;
            VoronoiVisual.MouseMove += OnMouseMove;
            VoronoiVisual.MouseLeave += OnMouseLeave;

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(OnLoaded));
        }

        private void OnLoaded()
        {
            _points = DataPresets.LoadPreset(1, ViewportWidth(), ViewportHeight()); // Random points

            _lastRenderTime = _watch.Elapsed.TotalMilliseconds;
            CompositionTarget.Rendering += CompositionTarget_Rendering; // Live-rendering
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            RemoveLiveCursorPoint();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(VoronoiVisual);

            _lastMoveDistance = Math.Sqrt(Math.Pow(position.X + _lastMovePoint.X, 2) + Math.Pow(position.Y + _lastMovePoint.Y, 2));
            _lastMoveDirection = (position - _lastMovePoint) / _lastMoveDistance;

            _lastMovePoint = position;
            _lastMoveElapsed = _watch.Elapsed.TotalSeconds - _lastMoveSec;
            _lastMoveSec = _watch.Elapsed.TotalSeconds;

            // Remove previous cursor point
            RemoveLiveCursorPoint();

            // Add live cursor point
            AddLiveCursorPoint(position);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(VoronoiVisual);
            var speed = _lastMoveDistance / _lastMoveElapsed;

            if (_watch.Elapsed.TotalSeconds - _lastMoveSec > 0.2)
                speed = 0;

            // Add point at cursor with cursor speed and direction
            _points.Add(new MovingPoint(position, speed, _lastMoveDirection));
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.C:
                    ClearPoints();
                    break;

                case Key.Up:
                    AddPoints(1);
                    break;

                case Key.Down:
                    RemovePoints(1);
                    break;

                case Key.W:
                    AddPoints(25);
                    break;

                case Key.S:
                    RemovePoints(25);
                    break;

                case Key.Q:
                    AdjustPointMovingSpeed(1 / 0.8);
                    break;

                case Key.A:
                    AdjustPointMovingSpeed(0.8);
                    break;

                case Key.P:
                    _showPoints = !_showPoints;
                    break;

                case Key.D:
                    _showDelaunay = !_showDelaunay;
                    break;

                case Key.V:
                    _showVoronoi = !_showVoronoi;
                    break;

                case Key.H:
                    _showDiagnostics = !_showDiagnostics;
                    break;

                case Key.D1:
                case Key.NumPad1:
                    SetPoints(DataPresets.LoadPreset(1, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.D2:
                case Key.NumPad2:
                    SetPoints(DataPresets.LoadPreset(2, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.D3:
                case Key.NumPad3:
                    SetPoints(DataPresets.LoadPreset(3, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.D4:
                case Key.NumPad4:
                    SetPoints(DataPresets.LoadPreset(4, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.D5:
                case Key.NumPad5:
                    SetPoints(DataPresets.LoadPreset(5, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.D6:
                case Key.NumPad6:
                    SetPoints(DataPresets.LoadPreset(6, ViewportWidth(), ViewportHeight()));
                    break;

                case Key.N:
                    _delaunayMethod = 0;
                    break;

                case Key.M:
                    _delaunayMethod = 1;
                    break;
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Determine time elapsed since last computation pass
            var elapsed = _watch.Elapsed.TotalMilliseconds - _lastRenderTime;
            _lastRenderTime = _watch.Elapsed.TotalMilliseconds;

            // Update moving points position & direction
            _points.ForEach(p => UpdateMovingPoint(p, elapsed));

            // Compute delaunay & voronoi
            var voronoi = ComputeVoronoi(out double delaunayElapsedMs, out double voronoiElapsedMs);

            // Update calculation time counter
            StatusText.Text =
                $"Points:    {_points.Count} \n" +
                $"Method:    {(_delaunayMethod == 0 ? "Incremental" : "Sweep-Circle")} \n" +
                $"Delaunay:  {(_showDelaunay ? "✓" : "❌")} ({delaunayElapsedMs:0.00} ms) \n" +
                $"Voronoi:   {(_showVoronoi ? "✓" : "❌")} ({voronoiElapsedMs:0.00} ms)";

            // Render voronoi
            VoronoiVisual.Visualize(voronoi, _showDiagnostics, _showPoints, _showDelaunay, _showVoronoi);
        }

        private Voronoi ComputeVoronoi(out double delaunayElapsedMs, out double voronoiElapsedMs)
        {
            // Compute Delaunay & Voronoi
            Voronoi voronoi = null;

            if (_delaunayMethod == 0)
            {
                var points = _points.Select(e => e.Loc).Distinct().ToArray();
                voronoi = new VoronoiIncremental(points, _showDiagnostics);
            }
            else
            {
                var points = _points.Select(e => e.Loc).ToArray();
                voronoi = new VoronoiSweepCircle(points, _showDiagnostics);
            }

            // Calculate Delaunay triangulation
            var calcTimeStart = _watch.Elapsed.TotalMilliseconds;
            voronoi.CalculateDelaunay();
            delaunayElapsedMs = _watch.Elapsed.TotalMilliseconds - calcTimeStart;

            // Calculate Voronoi
            calcTimeStart = _watch.Elapsed.TotalMilliseconds;
            if (_showVoronoi)
                voronoi.CalculateVoronoi(ViewportWidth(), ViewportHeight());
            voronoiElapsedMs = _watch.Elapsed.TotalMilliseconds - calcTimeStart;

            return voronoi;
        }
        
        private void SetPoints(List<MovingPoint> points)
        {
            RemoveLiveCursorPoint();
            _points = points;
        }

        private void AddPoints(int count)
        {
            for (var i = 0; i < count; i++)
                _points.Add(DataPresets.GenerateMovingPoint(ViewportWidth(), ViewportHeight()));
        }

        private void RemovePoints(int count)
        {
            // Remove live cursor point since its index would be wrong after removal of points
            RemoveLiveCursorPoint();

            if (_points.Count >= count)
                _points.RemoveRange(_points.Count - count, count);
        }

        private void ClearPoints()
        {
            RemoveLiveCursorPoint();
            _points.Clear();
        }

        private void AdjustPointMovingSpeed(double factor)
        {
            _points.ForEach(p => p.Speed *= factor);
        }

        private void AddLiveCursorPoint(Point position)
        {
            _lastCursorPoint = new MovingPoint(position);
            _lastCursorPointIdx = _points.Count;
            _points.Add(_lastCursorPoint);
        }

        private void RemoveLiveCursorPoint()
        {
            if (_lastCursorPointIdx == -1)
                return;

            _points.RemoveAt(_lastCursorPointIdx);

            _lastCursorPointIdx = -1;
        }

        private void UpdateMovingPoint(MovingPoint p, double elapsedMs)
        {
            var maxX = ViewportWidth();
            var maxY = ViewportHeight();

            if (p.Loc.X > maxX)
                p.Loc = new Point(maxX, p.Loc.Y);

            if (p.Loc.Y > maxY)
                p.Loc = new Point(p.Loc.X, maxY);

            var newLoc = p.Loc + p.Speed * (elapsedMs / 1000) * p.Direction;
            var hitBounds = false;

            if (newLoc.X < 0 || newLoc.X > maxX)
            {
                p.Direction = new Vector(-p.Direction.X, p.Direction.Y);
                hitBounds = true;
            }

            if (newLoc.Y < 0 || newLoc.Y > maxY)
            {
                p.Direction = new Vector(p.Direction.X, -p.Direction.Y);
                hitBounds = true;
            }

            if (hitBounds)
                p.Loc = p.Loc + p.Speed * (elapsedMs / 1000) * p.Direction;
            else
                p.Loc = newLoc;
        }

        private double ViewportWidth()
        {
            return VoronoiVisual.ActualWidth;
        }

        private double ViewportHeight()
        {
            return VoronoiVisual.ActualHeight;
        }
    }
}
