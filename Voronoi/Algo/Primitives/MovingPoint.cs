using System.Windows;

namespace VoronoiApp.Algo.Primitives
{
    /// <summary>
    /// Cartesian point which has speed and direction of travel properties.
    /// </summary>
    public class MovingPoint
    {
        public Point Loc;
        public double Speed;
        public Vector Direction;


        public MovingPoint(Point location)
        {
            Loc = location;
        }

        public MovingPoint(Point location, double speed, Vector direction)
        {
            Loc = location;
            Speed = speed;
            Direction = direction;
        }

        public string Serialize()
        {
            return $"{Loc}|{Speed}|{Direction.X},{Direction.Y}";
        }

        public static MovingPoint Deserialize(string serialized)
        {
            var parts = serialized.Split('|');
            var loc = new Point(double.Parse(parts[0].Split(',')[0]), double.Parse(parts[0].Split(',')[1]));
            var speed = double.Parse(parts[1]);
            var direction = new Vector(double.Parse(parts[2].Split(',')[0]), double.Parse(parts[2].Split(',')[1]));

            return new MovingPoint(loc, speed, direction);
        }
    }
}
