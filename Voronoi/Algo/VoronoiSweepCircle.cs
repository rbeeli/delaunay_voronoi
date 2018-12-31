using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp.Algo
{
    public class VoronoiSweepCircle : Voronoi
    {
        class PointNode
        {
            public PolarPoint Point { get; }
            public int t;
            public PointNode Previous;
            public PointNode Next;
            public bool Removed;


            public PointNode(PolarPoint p)
            {
                Point = p;
            }
        }


        PolarPoint[] triangles;
        int trianglesPointIndex;
        int[] halfedges;

        PolarPoint[] polarPoints;
        PointNode[] hash;
        int hashSize;

        Point center;

        PointNode hull;




        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="points">Point set to build Voronoi/Delaunay from.</param>
        /// <param name="buildDiagnostics">Indcates if diagnostics shapes/geometry for debugging is built.</param>
        public VoronoiSweepCircle(Point[] points, bool buildDiagnostics = false)
            : base(points, buildDiagnostics)
        { }

        /// <summary>
        /// Calculates Delaunay triangulation using the more efficient Sweep-Circle algorithm.
        /// Considered one of the fastest algorithms in empirical tests.
        /// Expected runtime is O(n log n).
        /// </summary>
        /// <remarks>
        /// http://cglab.ca/~biniaz/papers/Sweep%20Circle.pdf
        /// </remarks>
        public override void CalculateDelaunay()
        {
            if (!CheckDelaunayConditions())
                return;

            // Calculate origin point and seed triangle
            (Point origin, Triangle seed) = CalculateOriginSeedTriangle(out int i0, out int i1, out int i2);
            center = origin;

            if (i0 == -1)
            {
                SetDelaunayResult(new List<Triangle>());
                return; // No Delaunay triangulation exists
            }

            var len = Points.Length;
            polarPoints = new PolarPoint[len];

            var seed1 = new PolarPoint();
            var seed2 = new PolarPoint();
            var seed3 = new PolarPoint();
            for (var i = 0; i < len; i++)
            {
                var p = Points[i];
                var pp = new PolarPoint(
                    p,
                    GeoMath.EuclideanDistance2(center, p),
                    GeoMath.PolarPseudoAngle(center, p)
                );

                if (p.Equals(seed.A))
                    seed1 = pp;
                else if (p.Equals(seed.B))
                    seed2 = pp;
                else if (p.Equals(seed.C))
                    seed3 = pp;

                polarPoints[i] = pp;
            }

            // Sort polar points by radius in ascending order
            QuickSortPoints(polarPoints, 0, len - 1);

            // Hashtable for edges of convex hull
            hashSize = (int)Math.Sqrt(len);
            hash = new PointNode[hashSize + 2];

            // Circular doubly-linked list which will hold the convex hull
            var e = hull = InsertNode(seed1);
            HashEdge(e);
            e.t = 0;
            e = InsertNode(seed2, e);
            HashEdge(e);
            e.t = 1;
            e = InsertNode(seed3, e);
            HashEdge(e);
            e.t = 2;

            var maxTriangles = 2 * len - 5;
            triangles = new PolarPoint[maxTriangles * 3];
            halfedges = new int[maxTriangles * 3];

            AddTriangle(seed1, seed2, seed3, -1, -1, -1);

            var prev = new PolarPoint(new Point(-1, -1), -1, -1);

            for (var i = 0; i < len; i++)
            {
                var pp = polarPoints[i];

                if (seed.HasVertex(pp.CartesianPoint))
                    continue; // Skip seed triangle points

                if (prev.CartesianPoint.Equals(pp.CartesianPoint))
                    continue; // Ignore duplicate point

                // Find a visible edge on the convex hull using edge hash
                var startKey = HashKey(pp);
                var key = startKey;
                PointNode start;
                do
                {
                    start = hash[key];
                    key = (key + 1) % hashSize;
                }
                while ((start == null || start.Removed) && key != startKey);

                e = start;
                while (GeoMath.TriangleAreaDeterminant(pp.CartesianPoint, e.Point.CartesianPoint, e.Next.Point.CartesianPoint) <= 0)
                {
                    e = e.Next;
#if DEBUG
                    if (e == start)
                        throw new Exception("Processing error. Input points invalid or seed triangle wrongly oriented.");
#endif
                }

                var walkBack = e == start;

                // Add first triangle
                var t = AddTriangle(e.Point, pp, e.Next.Point, -1, -1, e.t);

                e.t = t; // Keep track of boundary triangles on hull
                e = InsertNode(pp, e);

                // Recursively flip triangles from the point until they satisfy the Delaunay condition
                e.t = Legalize(t + 2);
                if (e.Previous.Previous.t == halfedges[t + 1])
                    e.Previous.Previous.t = t + 2;

                // Walk forward through the hull, adding more triangles and flipping recursively
                var q = e.Next;
                while (GeoMath.TriangleAreaDeterminant(pp.CartesianPoint, q.Point.CartesianPoint, q.Next.Point.CartesianPoint) > 0)
                {
                    t = AddTriangle(q.Point, pp, q.Next.Point, q.Previous.t, -1, q.t);
                    q.Previous.t = Legalize(t + 2); // Flipping
                    hull = RemoveNode(q);
                    q = q.Next;
                }

                if (walkBack)
                {
                    // Walk backward from the other side, adding more triangles and flipping recursively
                    q = e.Previous;
                    while (GeoMath.TriangleAreaDeterminant(pp.CartesianPoint, q.Previous.Point.CartesianPoint, q.Point.CartesianPoint) > 0)
                    {
                        t = AddTriangle(q.Previous.Point, pp, q.Point, -1, q.t, q.Previous.t);
                        Legalize(t + 2); // Flipping
                        q.Previous.t = t;
                        hull = RemoveNode(q);
                        q = q.Previous;
                    }
                }

                // Save the two new edges in the hashtable
                HashEdge(e);
                HashEdge(e.Previous);

                prev = pp;
            }

            // Create Delaunay triangles from points
            var delaunay = new List<Triangle>(trianglesPointIndex / 3);
            for (var i = 0; i < trianglesPointIndex;)
            {
                delaunay.Add(new Triangle(
                    triangles[i++].CartesianPoint,
                    triangles[i++].CartesianPoint,
                    triangles[i++].CartesianPoint
                ));
            }

            SetDelaunayResult(delaunay);
        }

        /// <summary>
        /// Index given <see cref="PointNode"/> into hashtable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashEdge(PointNode e)
        {
            hash[HashKey(e.Point)] = e;
        }

        /// <summary>
        /// Calculate hash key of a <see cref="PolarPoint"/> by its angle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HashKey(PolarPoint p)
        {
            var dy = p.CartesianPoint.Y - center.Y;
            return (int)((2 + (dy < 0 ? -p.Angle : p.Angle)) / 4 * hashSize);
        }

        /// <summary>
        /// Legalizes triangles around given edge.
        /// </summary>
        private int Legalize(int a)
        {
            var b = halfedges[a];

            var a0 = a - a % 3;
            var b0 = b - b % 3;

            var al = a0 + (a + 1) % 3;
            var ar = a0 + (a + 2) % 3;
            var bl = b0 + (b + 2) % 3;

            var p0 = triangles[ar];
            var pr = triangles[a];
            var pl = triangles[al];
            var p1 = triangles[bl];

            var illegal = GeoMath.IsPointInCircumcircle(p0.CartesianPoint, pr.CartesianPoint, pl.CartesianPoint, p1.CartesianPoint);
            if (illegal && b != -1)
            {
                triangles[a] = p1;
                triangles[b] = p0;

                Link(a, halfedges[bl]);
                Link(b, halfedges[ar]);
                Link(ar, bl);

                var br = b0 + (b + 1) % 3;

                Legalize(a);

                return Legalize(br);
            }

            return ar;
        }

        /// <summary>
        /// Links two edges.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Link(int a, int b)
        {
            halfedges[a] = b;

            if (b != -1)
                halfedges[b] = a;
        }

        /// <summary>
        /// Add a new triangle given vertex indices and adjacent half-edge ids.
        /// </summary>
        private int AddTriangle(PolarPoint i0, PolarPoint i1, PolarPoint i2, int a, int b, int c)
        {
            var t = trianglesPointIndex;

            triangles[t] = i0;
            triangles[t + 1] = i1;
            triangles[t + 2] = i2;

            Link(t, a);
            Link(t + 1, b);
            Link(t + 2, c);

            trianglesPointIndex += 3;

            return t;
        }

        /// <summary>
        /// Insert new node into double-linked list
        /// </summary>
        private PointNode InsertNode(PolarPoint p, PointNode prev = null)
        {
            var node = new PointNode(p);

            if (prev == null)
            {
                node.Previous = node;
                node.Next = node;
            }
            else
            {
                node.Next = prev.Next;
                node.Previous = prev;
                prev.Next.Previous = node;
                prev.Next = node;
            }

            return node;
        }

        /// <summary>
        /// Remove node from double-linked list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PointNode RemoveNode(PointNode node)
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
            node.Removed = true;
            return node.Previous;
        }

        /// <summary>
        /// Manual implementation of QuickSort for ordering <see cref="PolarPoint"/> after <see cref="PolarPoint.Radius2"/>.
        /// </summary>
        private void QuickSortPoints(PolarPoint[] points, int left, int right)
        {
            int i = left, j = right;

            var pivot = points[(left + right) / 2].Radius2;

            while (i <= j)
            {
                while (points[i].Radius2 < pivot)
                    i++;

                while (points[j].Radius2 > pivot)
                    j--;

                if (i <= j)
                {
                    // Swap
                    var tmp = points[i];
                    points[i] = points[j];
                    points[j] = tmp;

                    i++;
                    j--;
                }
            }

            // Recursive calls
            if (left < j)
                QuickSortPoints(points, left, j);

            // Recursive calls
            if (i < right)
                QuickSortPoints(points, i, right);
        }

        protected (Point, Triangle) CalculateOriginSeedTriangle(out int idx1, out int idx2, out int idx3)
        {
            idx1 = -1;
            idx2 = -1;
            idx3 = -1;

            // Calculate origin point
            (Point min, Point max) = CalculateBoundaryBox();

            var origin = new Point((min.X + max.X) / 2, (min.Y + max.Y) / 2);

            // Find first seed point closest to origin
            {
                var minDist = double.MaxValue;
                for (var i = 0; i < Points.Length; i++)
                {
                    var dist = GeoMath.EuclideanDistance2(Points[i], origin);
                    if (dist < minDist)
                    {
                        idx1 = i;
                        minDist = dist;
                    }
                }
            }

            // Find point closest to seed point
            {
                var minDist = double.MaxValue;
                for (var i = 0; i < Points.Length; i++)
                {
                    if (i == idx1) continue;
                    var dist = GeoMath.EuclideanDistance2(Points[i], Points[idx1]);
                    if (dist < minDist)
                    {
                        idx2 = i;
                        minDist = dist;
                    }
                }
            }

            // Find point which creates the smallest circumcircle
            {
                var minCircRadius2 = double.MaxValue;
                for (var i = 0; i < Points.Length; i++)
                {
                    if (i == idx1 || i == idx2) continue;
                    var circRadius2 = GeoMath.Circumradius2(Points[idx1], Points[idx2], Points[i]);
                    if (circRadius2 < minCircRadius2)
                    {
                        idx3 = i;
                        minCircRadius2 = circRadius2;
                    }
                }

                if (idx3 == -1)
                {
                    idx1 = -1;
                    idx2 = -1;
                    return (new Point(), Triangle.Zero); // No Delaunay triangulation exists
                }
            }

            // Make sure seed triangle points are oriented counter-clockwise
            if (GeoMath.TriangleAreaDeterminant(Points[idx1], Points[idx2], Points[idx3]) > 0)
            {
                var tmp = idx2;
                idx2 = idx3;
                idx3 = tmp;
            }

            // Update origin to represent midpoint of seed triangle
            origin = GeoMath.CalculateMidpoint(Points[idx1], Points[idx2], Points[idx3]);

            var seed = new Triangle(Points[idx1], Points[idx2], Points[idx3]);

            if (BuildDiagonstics)
            {
                // Add boundary box and seed point/triangle to diagnostics
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Line, DiagColor.Red, seed.GetEdgeA().Start, seed.GetEdgeA().End));
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Line, DiagColor.Red, seed.GetEdgeB().Start, seed.GetEdgeB().End));
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Line, DiagColor.Red, seed.GetEdgeC().Start, seed.GetEdgeC().End));
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Line, DiagColor.Red, min, new Point(min.X, max.Y), new Point(max.X, max.Y), new Point(max.X, max.Y), new Point(max.X, min.Y), min));
                DiagnosticGeometry.Add(new DiagnosticGeometry(DiagGeometryType.Vertex, DiagColor.Red, origin));
            }

            return (origin, seed);
        }


        public override void CalculateVoronoi(double viewportWidth, double viewportHeight)
        {
            if (Delaunay == null)
                throw new InvalidOperationException("Delaunay triangulation needs to be calculated first.");

            VoronoiEdges = new List<Edge>(Delaunay.Count * 2);
            VoronoiPoints = new List<Point>(Delaunay.Count);

            if (Delaunay.Count < 1)
                return; // Nothing to do

            // Add circumenters of all Delaunay triangles
            for (var i = 0; i < Delaunay.Count; i++)
                VoronoiPoints.Add(Delaunay[i].Circumcenter);

            // Connect all neighbouring Delaunay triangle circumcenters
            for (var i = 0; i < halfedges.Length; i++)
            {
                var j = halfedges[i];
                if (j < i || j < 0)
                    continue;

                var edge = new Edge(VoronoiPoints[i / 3], VoronoiPoints[j / 3]);
                VoronoiEdges.Add(edge);
            }

            // Calculate infinity edges of convex hull
            var node = hull;
            var head = node;
            do
            {
                var p1 = node.Point.CartesianPoint;
                var p2 = node.Next.Point.CartesianPoint;
                var c = VoronoiPoints[node.t / 3];

                // Delta vector
                var dx = (p1.X + p2.X) / 2 - c.X;
                var dy = (p1.Y + p2.Y) / 2 - c.Y;
                var dxAbs = dx < 0 ? -dx : dx;
                var dyAbs = dy < 0 ? -dy : dy;

                // Set direction
                var det = GeoMath.TriangleAreaDeterminant(p1, p2, c) > 0 ? -1 : 1;
                dx *= det;
                dy *= det;

                // Stretch vector to boundary rectangle (viewport)
                if (dxAbs > dyAbs)
                {
                    // Normalize delta vector
                    dx = dx < 0 ? -1 : 1;
                    dy /= dxAbs;

                    if (dx < 0)
                    {
                        dx *= c.X;
                        dy *= c.X;
                    }
                    else
                    {
                        dx *= viewportWidth - c.X;
                        dy *= viewportWidth - c.X;
                    }
                }
                else
                {
                    // Normalize delta vector
                    dx /= dyAbs;
                    dy = dy < 0 ? -1 : 1;

                    if (dy < 0)
                    {
                        dx *= c.Y;
                        dy *= c.Y;
                    }
                    else
                    {
                        dx *= viewportHeight - c.Y;
                        dy *= viewportHeight - c.Y;
                    }
                }

                VoronoiEdges.Add(new Edge(c, new Point(c.X + dx, c.Y + dy)));
            }
            while ((node = node.Next) != head);
        }
    }
}
