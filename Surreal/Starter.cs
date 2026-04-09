using System;

namespace Surreal
{
    class Starter
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Basic arithmetic ===");
            foreach (var x in new[] { new Surr(0), new Surr(1), new Surr(2), new Surr(-3) })
                Console.WriteLine($"{x}: neg={-x}, +1={x + 1}, -1={x - 1}, double={x + x}");

            Console.WriteLine();
            Console.WriteLine("=== Fractions ===");
            var half = Surr.Half;
            var quarter = Surr.Dyadic(1, 2);
            Console.WriteLine($"1/2 + 1/2 = {half + half}");
            Console.WriteLine($"1/2 * 1/2 = {half * half}");
            Console.WriteLine($"1/4 * 4 = {quarter * 4}");
            Console.WriteLine($"4 * 4 = {new Surr(4) * new Surr(4)}");
            Console.WriteLine($"3 * 3 * 3 = {new Surr(3) * new Surr(3) * new Surr(3)}");

            Console.WriteLine();
            Console.WriteLine("=== Transfinite: ω ===");
            var w = Surr.Omega;
            Console.WriteLine($"ω = {w}");
            Console.WriteLine($"ω > 0 : {w > 0}");
            Console.WriteLine($"ω > 100 : {w > 100}");
            Console.WriteLine($"ω > 1000000 : {w > 1000000}");
            Console.WriteLine($"ω == ω : {w == w}");
            Console.WriteLine($"ω >= ω : {w >= w}");
            Console.WriteLine($"ω > ω : {w > w}");

            // ω + 1 = {ω | }
            var w1 = new Surr(null, new[] { w }, null, null, "ω+1");
            Console.WriteLine($"ω+1 = {w1}");
            Console.WriteLine($"ω+1 > ω : {w1 > w}");
            Console.WriteLine($"ω > ω+1 : {w > w1}");
            Console.WriteLine($"ω+1 > 100 : {w1 > 100}");

            // ω - 1 = {0,1,2,... | ω}
            var wm1 = new Surr(NaturalNumbers.Instance, null, null, new[] { w }, "ω-1");
            Console.WriteLine($"ω-1 = {wm1}");
            Console.WriteLine($"ω-1 < ω : {wm1 < w}");
            Console.WriteLine($"ω-1 > 100 : {wm1 > 100}");

            Console.WriteLine();
            Console.WriteLine("=== Infinitesimal: 1/ω ===");
            var eps = Surr.InverseOmega;
            Console.WriteLine($"1/ω = {eps}");
            Console.WriteLine($"1/ω > 0 : {eps > 0}");
            Console.WriteLine($"1/ω < 1 : {eps < 1}");
            Console.WriteLine($"1/ω < 1/2 : {eps < half}");
            Console.WriteLine($"1/ω < 1/4 : {eps < quarter}");
        }
    }
}
