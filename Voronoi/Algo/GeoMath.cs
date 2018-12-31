using System.Runtime.CompilerServices;
using System.Windows;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp.Algo
{
    public static class GeoMath
    {
        /// <summary>
        /// Calculates the squared Eucledian distance between to points.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EuclideanDistance2(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Indicates if the circumcircle of the given triangle contains the given point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInCircumcircle(Triangle tri, Point p)
        {
            return EuclideanDistance2(tri.Circumcenter, p) <= EuclideanDistance2(tri.Circumcenter, tri.A);
        }

        /// <summary>
        /// Indicates if the circumcircle of the given triangle points contains the given point.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPointInCircumcircle(Point a, Point b, Point c, Point p)
        {
            var ax = a.X - p.X;
            var ay = a.Y - p.Y;
            var bx = b.X - p.X;
            var by = b.Y - p.Y;
            var cx = c.X - p.X;
            var cy = c.Y - p.Y;

            var ap = ax * ax + ay * ay;
            var bp = bx * bx + by * by;
            var cp = cx * cx + cy * cy;

            return ax * (by * cp - bp * cy) -
                   ay * (bx * cp - bp * cx) +
                   ap * (bx * cy - by * cx) < 0;
        }

        /// <summary>
        /// Computes squared radius of circumcircle for the three given points.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Circumradius2(Point a, Point b, Point c)
        {
            var circumcenter = Circumcenter(a, b, c);
            return EuclideanDistance2(circumcenter, a);
        }

        /// <summary>
        /// Computes the circumcenter of the triangle.
        /// </summary>
        /// <remarks>
        /// https://ch.mathworks.com/matlabcentral/fileexchange/7844-geom2d?focused=8114427&tab=function
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point Circumcenter(Point a, Point b, Point c)
        {
            // pre - compute some terms
            var ah = a.X * a.X + a.Y * a.Y; // sum(a.^ 2);
            var bh = b.X * b.X + b.Y * b.Y; // sum(b.^ 2);
            var ch = c.X * c.X + c.Y * c.Y; // sum(c.^ 2);

            var dab1 = a.X - b.X;
            var dab2 = a.Y - b.Y;
            var dbc1 = b.X - c.X;
            var dbc2 = b.Y - c.Y;
            var dca1 = c.X - a.X;
            var dca2 = c.Y - a.Y;

            // common denominator
            var D = .5 / (a.X * dbc2 + b.X * dca2 + c.X * dab2);

            // center coordinates
            var x = (ah * dbc2 + bh * dca2 + ch * dab2) * D;
            var y = -(ah * dbc1 + bh * dca1 + ch * dab1) * D;

            return new Point(x, y);
        }

        /// <summary>
        /// Determinant of triangle area.
        /// 
        /// Smaller 0     Clockwise
        /// Greater 0     Counter-clockwise
        /// Equal   0     Colinear
        /// </summary>
        /// <remarks>
        /// https://e-maxx-eng.appspot.com/geometry/oriented-triangle-area.html
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TriangleAreaDeterminant(Point a, Point b, Point c)
        {
            // twice the triangle's area - signed
            return (b.X - a.X) * (c.Y - b.Y) - (c.X - b.X) * (b.Y - a.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsColinear(Point a, Point b, Point c)
        {
            return TriangleAreaDeterminant(a, b, c) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(Point a, Point b, Point c)
        {
            return TriangleAreaDeterminant(a, b, c) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(Triangle tri)
        {
            return TriangleAreaDeterminant(tri.A, tri.B, tri.C) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCounterClockwise(Point a, Point b, Point c)
        {
            return TriangleAreaDeterminant(a, b, c) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCounterClockwise(Triangle tri)
        {
            return TriangleAreaDeterminant(tri.A, tri.B, tri.C) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double PolarPseudoAngle(Point center, Point p)
        {
            var dx = p.X - center.X;
            var dy = p.Y - center.Y;

            // Uses pseudo-angle, a measure that monotonically increases
            // with the actual angle, but without doing costly trigonometric calculations
            return 1 - dx / ((dx < 0 ? -dx : dx) + (dy < 0 ? -dy : dy));
        }

        /// <summary>
        /// Calculates the midpoint of the given triangle points.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point CalculateMidpoint(Point a, Point b, Point c)
        {
            return new Point((a.X + b.X + c.X) / 3, (a.Y + b.Y + c.Y) / 3);
        }
    }
}
