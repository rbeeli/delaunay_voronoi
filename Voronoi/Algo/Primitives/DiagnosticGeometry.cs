using System.Windows;

namespace VoronoiApp.Algo.Primitives
{
    public enum DiagGeometryType
    {
        Vertex,
        Line,
        Circle
    }

    public enum DiagColor
    {
        Red,
        Yellow
    }

    public class DiagnosticGeometry
    {
        public DiagGeometryType Type { get; set; }
        public DiagColor Color { get; set; }
        public Point[] Vertices { get; }



        public DiagnosticGeometry(DiagGeometryType type, DiagColor color, params Point[] vertices)
        {
            Type = type;
            Color = color;
            Vertices = vertices;
        }
    }
}
