using System;
using System.Collections.Generic;
using System.Windows;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp.Algo
{
    public abstract class Voronoi
    {
        public Point[] Points { get; }

        public List<Triangle> Delaunay { get; protected set; }

        public List<Point> VoronoiPoints { get; protected set; }
        public List<Edge> VoronoiEdges { get; protected set; }

        public bool BuildDiagonstics { get; }
        public List<DiagnosticGeometry> DiagnosticGeometry { get; }



        /// <summary>
        /// Base constructor.
        /// </summary>
        public Voronoi(Point[] points, bool buildDiagnostics = false)
        {
            // Point data source, used for Delaunay and Voronoi
            Points = points;

            // Should additional geometry for diagnostic/visualization purposes be built?
            BuildDiagonstics = buildDiagnostics;
            if (BuildDiagonstics)
                DiagnosticGeometry = new List<DiagnosticGeometry>();
        }

        /// <summary>
        /// Calculates boundary box coordinates to enclose all points.
        /// </summary>
        protected (Point min, Point max) CalculateBoundaryBox()
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            for (var i = 0; i < Points.Length; i++)
            {
                var p = Points[i];

                if (p.X > maxX)
                    maxX = p.X;

                if (p.X < minX)
                    minX = p.X;

                if (p.Y > maxY)
                    maxY = p.Y;

                if (p.Y < minY)
                    minY = p.Y;
            }

            return (new Point(minX, minY), new Point(maxX, maxY));
        }
        
        /// <summary>
        /// If there are 3 or less points, not Delaunay trianglation needs to be performed.
        /// </summary>
        /// <returns></returns>
        protected bool CheckDelaunayConditions()
        {
            if (Points.Length < 3)
            {
                SetDelaunayResult(new List<Triangle>());
                return false;
            }

            return true;
        }

        protected virtual void SetDelaunayResult(List<Triangle> delaunay)
        {
            Delaunay = delaunay;

            if (BuildDiagonstics)
            {
                foreach (var tri in Delaunay)
                {
                    var radius = Math.Sqrt(GeoMath.EuclideanDistance2(tri.Circumcenter, tri.A));
                    DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Circle, DiagColor.Yellow, tri.Circumcenter, new Point(radius, 0)));
                }
            }
        }

        public abstract void CalculateDelaunay();

        public abstract void CalculateVoronoi(double viewportWidth, double viewportHeight);
    }
}
