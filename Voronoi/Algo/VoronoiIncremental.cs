using System;
using System.Collections.Generic;
using System.Windows;
using VoronoiApp.Algo.DataStructures;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp.Algo
{
    public class VoronoiIncremental : Voronoi
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="points">Point set to build voroni/delaunay from.</param>
        /// <param name="buildDiagnostics">Indcates if diagnostics shapes/geometry for debugging is built.</param>
        public VoronoiIncremental(Point[] points, bool buildDiagnostics = false)
            : base(points, buildDiagnostics)
        { }

        protected Triangle CalculateSupertriangle()
        {
            // Create a supertriangle which is substantially bigger than the convex plane spanned by the point set
            // to solve the convexity issue (problem of concave structures occuring in hull).
            // https://www.codeguru.com/cpp/cpp/algorithms/general/article.php/c8901/Delaunay-Triangles.htm

            (Point min, Point max) = CalculateBoundaryBox();

            if (BuildDiagonstics)
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Line, DiagColor.Red, min, new Point(min.X, max.Y), new Point(max.X, max.Y), new Point(max.X, max.Y), new Point(max.X, min.Y), min));

            // Create supertriangle
            var dMax = Math.Max(max.X - min.X, max.Y - min.Y);
            var xMid = (max.X + min.X) / 2;
            var yMid = (max.Y + min.Y) / 2;

            return new Triangle(
                new Point(xMid - 20 * dMax, yMid - dMax),
                new Point(xMid, yMid + 20 * dMax),
                new Point(xMid + 20 * dMax, yMid - dMax)
            );
        }

        /// <summary>
        /// Calculates Delaunay triangulation via a naive implementation of the incremental Bowyer-Waston algorithm.
        /// Expected runtime is O(n²).
        /// </summary>
        /// <remarks>
        /// https://www.maths.tcd.ie/~martins7/Voro/Sean_Martin_Poster.pdf
        /// https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm
        /// https://www.codeguru.com/cpp/cpp/algorithms/general/article.php/c8901/Delaunay-Triangles.htm
        /// https://eclass.uoa.gr/modules/document/file.php/D42/%CE%94%CE%B9%CE%B1%CF%86%CE%AC%CE%BD%CE%B5%CE%B9%CE%B5%CF%82/2a.delaunay.pdf - Slide 40+
        /// </remarks>
        public override void CalculateDelaunay()
        {
            if (!CheckDelaunayConditions())
                return;

            var triangulation = new HashSet<Triangle>();

            // Start with the supertriangle, which contains all available points
            var supertriangle = CalculateSupertriangle();
            triangulation.Add(supertriangle);

            // Keep a list of triangles where the point is within its circumcircle
            var conflictingTriangles = new List<Triangle>();
            var polygonEdges = new List<Edge>();

            // Incrementally add points to existing set of triangles
            foreach (var p in Points)
            {
                // Find triangles where given point is in its circumcircle (conflicting)
                foreach (var tri in triangulation)
                {
                    if (GeoMath.IsPointInCircumcircle(tri, p))
                    {
                        conflictingTriangles.Add(tri);

                        // Remove neighbouring edges of conflicting triangles

                        // Edge A
                        var edgeA = tri.GetEdgeA();
                        if (polygonEdges.Contains(edgeA))
                            polygonEdges.Remove(edgeA);
                        else
                            polygonEdges.Add(edgeA);

                        // Edge B
                        var edgeB = tri.GetEdgeB();
                        if (polygonEdges.Contains(edgeB))
                            polygonEdges.Remove(edgeB);
                        else
                            polygonEdges.Add(edgeB);

                        // Edge C
                        var edgeC = tri.GetEdgeC();
                        if (polygonEdges.Contains(edgeC))
                            polygonEdges.Remove(edgeC);
                        else
                            polygonEdges.Add(edgeC);
                    }
                }

                // Remove conflicting triangles from hashset
                foreach (var tri in conflictingTriangles)
                    triangulation.Remove(tri);
                
                // Create new triangles by connecting all polygon vertices to the newly added vertex
                foreach (var edge in polygonEdges)
                    triangulation.Add(new Triangle(edge.Start, edge.End, p));

                conflictingTriangles.Clear();
                polygonEdges.Clear();
            }

            // Only add triangles to final triangulation result which do not share a vertex with the super-triangle


            var delaunay = new List<Triangle>(triangulation.Count);
            foreach (var tri in triangulation)
            {
                if (!supertriangle.HasVertex(tri.A) &&
                    !supertriangle.HasVertex(tri.B) &&
                    !supertriangle.HasVertex(tri.C))
                {
                    delaunay.Add(tri);
                }
            }

            SetDelaunayResult(delaunay);
        }

        public override void CalculateVoronoi(double viewportWidth, double viewportHeight)
        {
            if (Delaunay == null)
                throw new InvalidOperationException("Delaunay triangulation needs to be calculated first.");

            VoronoiEdges = new List<Edge>(Delaunay.Count * 2);
            VoronoiPoints = new List<Point>(Delaunay.Count);

            if (Delaunay.Count < 1)
                return; // Nothing to do

            // Use hashtables for efficient retrieval of triangles by edge
            var edges = new HashSet<Edge>();
            var triangles = new MultiValueDictionary<Edge, Triangle>(2);

            foreach (var tri in Delaunay)
            {
                var edgeA = tri.GetEdgeA();
                var edgeB = tri.GetEdgeB();
                var edgeC = tri.GetEdgeC();

                edges.Add(edgeA);
                edges.Add(edgeB);
                edges.Add(edgeC);

                triangles.Add(edgeA, tri);
                triangles.Add(edgeB, tri);
                triangles.Add(edgeC, tri);

                VoronoiPoints.Add(tri.Circumcenter);
            }

            foreach (var edge in edges)
            {
                var tris = triangles[edge]; // O(1)
                if (tris.Count == 2)
                {
                    var c1 = tris[0].Circumcenter;
                    var c2 = tris[1].Circumcenter;

                    if (c1.Equals(c2))
                        continue;

                    VoronoiEdges.Add(new Edge(c1, c2));
                }
            }

            // TODO: Infinity edges
        }
    }
}
