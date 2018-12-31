using System;
using System.Windows;

namespace VoronoiApp.Algo.Primitives
{
    public struct Edge : IEquatable<Edge>
    {
        public Point Start { get; }
        public Point End { get; }



        public Edge(Point start, Point end)
        {
            Start = start;
            End = end;
        }

        public bool Equals(Edge other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End) ||
                    Start.Equals(other.End) && End.Equals(other.Start);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Edge))
                return false;

            return Equals((Edge)obj);
        }

        public override int GetHashCode()
        {
            var a = Start;
            var b = End;

            if (a.X != b.X)
            {
                if (a.X > b.X)
                {
                    var t = a;
                    a = b;
                    b = t;
                }
            }
            else if (a.Y > b.Y)
            {
                var t = a;
                a = b;
                b = t;
            }

            var hash = (13 * 7) + a.GetHashCode();
            hash = (hash * 7) + b.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return $"({Start})-({End})";
        }
    }
}
