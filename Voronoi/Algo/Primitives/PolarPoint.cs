using System.Windows;

namespace VoronoiApp.Algo.Primitives
{
    /// <summary>
    /// Represents a cartesian point in a polar coordinate system.
    /// </summary>
    public struct PolarPoint
    {
        public Point CartesianPoint { get; }
        public double Radius2 { get; }
        public double Angle { get; }



        public PolarPoint(Point cartesianPoint, double radius2, double angle)
        {
            CartesianPoint = cartesianPoint;
            Radius2 = radius2;
            Angle = angle;
        }

        public override string ToString()
        {
            return $"[radius² {Radius2:0.0}, angle {Angle:0.0000} ({CartesianPoint.X:0.000}, {CartesianPoint.Y:0.000})]";
        }
    }
}
