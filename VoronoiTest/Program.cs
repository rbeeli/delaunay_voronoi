using System;
using System.Diagnostics;
using System.Windows;
using VoronoiApp.Algo;

namespace VoronoiTest
{
    class Program
    {
        static Random Random = new Random();

        static void Main()
        {
            // Generate 1 M uniformly distributed points
            var points = new Point[1_000_000];
            for (var i = 0; i < points.Length; i++)
                points[i] = new Point(Random.Next(), Random.Next());

            var watch = new Stopwatch();

            for (var i = 0; i < 10; i++)
            {
                var voronoi = new VoronoiSweepCircle(points, false);

                // Delaunay
                {
                    watch.Restart();
                    voronoi.CalculateDelaunay();
                    watch.Stop();
                }

                var delaunaySec = watch.Elapsed.TotalSeconds;

                // Voronoi
                {
                    watch.Restart();
                    voronoi.CalculateVoronoi(1, 1);
                    watch.Stop();
                }

                var vornoiSec = watch.Elapsed.TotalSeconds;

                Console.WriteLine($"Sweep-Circle {points.Length:N0} points: Delaunay {delaunaySec:0.00} s, Voronoi {vornoiSec:0.00} s, Total {(delaunaySec + vornoiSec):0.00} s.");
            }

            Console.ReadLine();
        }
    }
}
