using System.Runtime.CompilerServices;
using System.Windows;

namespace VoronoiApp.Algo.Primitives
{
    public class Triangle
    {
        public static Triangle Zero = new Triangle();

        public Point A { get; }
        public Point B { get; }
        public Point C { get; }

        public Point Circumcenter { get; }



        public Triangle(Point a, Point b, Point c)
        {
            A = a;
            B = b;
            C = c;

            Circumcenter = GeoMath.Circumcenter(a, b, c);
        }

        public Triangle()
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Edge GetEdgeA()
        {
            return new Edge(A, B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Edge GetEdgeB()
        {
            return new Edge(B, C);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Edge GetEdgeC()
        {
            return new Edge(C, A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasVertex(Point p)
        {
            return A.Equals(p) || B.Equals(p) || C.Equals(p);
        }

        public override string ToString()
        {
            return $"{{{A}}}, {{{B}}}, {{{C}}}";
        }
    }
}
