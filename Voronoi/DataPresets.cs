using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VoronoiApp.Algo.Primitives;

namespace VoronoiApp
{
    public static class DataPresets
    {
        public static List<MovingPoint> LoadPreset(int num, double viewportWidth, double viewportHeight)
        {
            var midX = viewportWidth / 2;
            var midY = viewportHeight / 2;
            var objectRadius = 200;
            var points = new List<MovingPoint>();

            switch (num)
            {
                case 1:
                    // Random points
                    for (var i = 0; i < 50; i++)
                        points.Add(GenerateMovingPoint(viewportWidth, viewportHeight));
                    break;

                case 2:
                    // Square
                    points = new List<MovingPoint>
                    {
                        new MovingPoint(new Point(midX - objectRadius, midY - objectRadius), 100, new Vector(1, 1)),
                        new MovingPoint(new Point(midX + objectRadius, midY - objectRadius), 100, new Vector(1, 1)),
                        new MovingPoint(new Point(midX + objectRadius, midY + objectRadius), 100, new Vector(1, 1)),
                        new MovingPoint(new Point(midX - objectRadius, midY + objectRadius), 100, new Vector(1, 1))
                    };
                    break;

                case 3:
                    // Circle
                    for (var deg = 0; deg < 360; deg += 360 / 10)
                    {
                        var x = objectRadius * Math.Cos(deg * Math.PI / 180);
                        var y = objectRadius * Math.Sin(deg * Math.PI / 180);

                        points.Add(new MovingPoint(new Point(midX + x, midY + y)));

                        x = objectRadius * 1.2 * Math.Cos(deg * Math.PI / 180);
                        y = objectRadius * 1.2 * Math.Sin(deg * Math.PI / 180);

                        points.Add(new MovingPoint(new Point(midX + x, midY + y)));
                    }
                    break;

                case 4:
                    // Grid
                    var gridWidth = viewportWidth * 0.7;
                    var gridHeight = viewportHeight * 0.8;
                    var marginX = viewportWidth * 0.15;
                    var marginY = viewportHeight * 0.1;

                    for (var x = 0; x <= 15; x++)
                    {
                        for (var y = 0; y <= 15; y++)
                        {
                            points.Add(new MovingPoint(new Point(marginX + x / 15.0 * gridWidth, marginY + y / 15.0 * gridHeight)));
                        }
                    }
                    break;

                case 5:
                    // Star
                    points.Add(new MovingPoint(new Point(midX, midY)));

                    // Inner points
                    for (var deg = 0; deg < 360; deg += 360 / 10)
                    {
                        var x = objectRadius * 0.25 * Math.Cos(deg * Math.PI / 180);
                        var y = objectRadius * 0.25 * Math.Sin(deg * Math.PI / 180);

                        points.Add(new MovingPoint(new Point(midX + x, midY + y)));
                    }

                    // Outer points
                    for (var deg = 0; deg < 360; deg += 360 / 20)
                    {
                        var x = objectRadius * 1.5 * Math.Cos(deg * Math.PI / 180);
                        var y = objectRadius * 1.5 * Math.Sin(deg * Math.PI / 180);

                        points.Add(new MovingPoint(new Point(midX + x, midY + y)));
                    }
                    break;

                case 6:
                    // Text
                    var formattedText = new FormattedText(
                        "Kalina",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#MC Sweetie Hearts"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Arial")),
                        450,
                        Brushes.Black,
                        96);
                    
                    var geometry = formattedText.BuildGeometry(new Point(midX - formattedText.Width / 2 - 100, midY - formattedText.Height / 2));

                    foreach (var f in geometry.GetFlattenedPathGeometry().Figures)
                    {
                        foreach (var s in f.Segments)
                        {
                            if (s is PolyLineSegment)
                            {
                                foreach (var pt in ((PolyLineSegment)s).Points)
                                    points.Add(new MovingPoint(pt));
                            }
                        }
                    }
                    break;
            }

            return points;
        }

        public static MovingPoint GenerateMovingPoint(double viewportWidth, double viewportHeight)
        {
            return new MovingPoint(
                new Point(
                    App.Random.NextDouble() * viewportWidth,
                    App.Random.NextDouble() * viewportHeight
                ),
                App.Random.Next(0, 400),
                new Vector(
                    App.Random.NextDouble() * (App.Random.NextDouble() * 2 - 1),
                    App.Random.NextDouble() * (App.Random.NextDouble() * 2 - 1)
                )
            );
        }
    }
}
