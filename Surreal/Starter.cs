using System;

namespace Surreal
{
    class Starter
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Basic arithmetic ===");
            var values = new[] { new Surr(0), new Surr(1), new Surr(2), new Surr(-3) };
            foreach (var x in values)
                Console.WriteLine($"{x}: neg={-x}, +1={x + 1}, -1={x - 1}, double={x + x}, numeric={x.IsNumeric}");

            Console.WriteLine();
            Console.WriteLine("=== Dyadic fractions ===");
            var half = Surr.Half;
            var quarter = Surr.Dyadic(1, 2);
            var threeQuarters = Surr.Dyadic(3, 2);
            Console.WriteLine($"1/2 = {half}");
            Console.WriteLine($"1/4 = {quarter}");
            Console.WriteLine($"3/4 = {threeQuarters}");
            Console.WriteLine($"1/2 + 1/2 = {half + half}");
            Console.WriteLine($"1/4 + 3/4 = {quarter + threeQuarters}");
            Console.WriteLine($"1/2 - 1/4 = {half - quarter}");

            Console.WriteLine();
            Console.WriteLine("=== Multiplication ===");
            Console.WriteLine($"1 * 1 = {new Surr(1) * new Surr(1)}");
            Console.WriteLine($"(-1) * 3 = {new Surr(-1) * new Surr(3)}");
            Console.WriteLine($"2 * 3 = {new Surr(2) * new Surr(3)}");
            Console.WriteLine($"1/2 * 2 = {half * 2}");
            Console.WriteLine($"1/2 * 1/2 = {half * half}");
            Console.WriteLine($"(-1) * 1/2 = {new Surr(-1) * half}");

            Console.WriteLine();
            Console.WriteLine("=== Previously slow (now with auto-simplify) ===");
            Console.WriteLine($"1/4 * 4 = {quarter * 4}");
            Console.WriteLine($"3 * (-2) = {new Surr(3) * new Surr(-2)}");
            var x4 = new Surr(4);
            Console.WriteLine($"4 + 4 = {x4 + x4}");
            var r = x4 * x4;
            Console.WriteLine($"4 * 4 = {r} (simplified: {r.Simplify()})");
            Console.WriteLine($"3 * 3 * 3 = {new Surr(3) * new Surr(3) * new Surr(3)}");

            Console.WriteLine();
            Console.WriteLine("=== Simplify ===");
            var bloated = new Surr(new[] { new Surr(1), new Surr(2), new Surr(3) }, new[] { new Surr(7) });
            Console.WriteLine($"{{1,2,3|7}} = {bloated} (simplified: {bloated.Simplify()})");

            Console.WriteLine();
            Console.WriteLine("=== Comparisons ===");
            Console.WriteLine($"1/2 < 1 : {half < new Surr(1)}");
            Console.WriteLine($"1/4 < 1/2 : {quarter < half}");
            Console.WriteLine($"3/4 > 1/2 : {threeQuarters > half}");
        }
    }
}
