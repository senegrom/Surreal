using System;

namespace Surreal
{
    class Starter
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Basic arithmetic ===");
            Console.WriteLine($"1/2 * 1/2 = {Surr.Half * Surr.Half}");
            Console.WriteLine($"1/4 * 4 = {Surr.Dyadic(1, 2) * 4}");
            Console.WriteLine($"4 * 4 = {new Surr(4) * new Surr(4)}");
            Console.WriteLine($"3 * 3 * 3 = {new Surr(3) * new Surr(3) * new Surr(3)}");

            Console.WriteLine();
            Console.WriteLine("=== Transfinite ===");
            var w = Surr.Omega;
            Console.WriteLine($"ω > 1000000 : {w > 1000000}");
            Console.WriteLine($"ω == ω : {w == w}");
            Console.WriteLine($"1/ω > 0 : {Surr.InverseOmega > 0}");
            Console.WriteLine($"1/ω < 1/4 : {Surr.InverseOmega < Surr.Dyadic(1, 2)}");
            Console.WriteLine($"ω > 1/3 : {w > Surr.FromRational(1, 3)}");
            Console.WriteLine($"ω > 1/ω : {w > Surr.InverseOmega}");

            Console.WriteLine();
            Console.WriteLine("=== Rational: 1/3 ===");
            var third = Surr.FromRational(1, 3);
            Console.WriteLine($"1/3 = {third}");
            Console.WriteLine($"1/3 > 0 : {third > 0}");
            Console.WriteLine($"1/3 < 1 : {third < 1}");
            Console.WriteLine($"1/3 < 1/2 : {third < Surr.Half}");
            Console.WriteLine($"1/3 > 1/4 : {third > Surr.Dyadic(1, 2)}");
            Console.WriteLine($"1/3 == 1/3 : {third == Surr.FromRational(1, 3)}");

            var twoThirds = Surr.FromRational(2, 3);
            Console.WriteLine($"2/3 = {twoThirds}");
            Console.WriteLine($"1/3 < 2/3 : {third < twoThirds}");
            Console.WriteLine($"2/3 < 1 : {twoThirds < 1}");

            Console.WriteLine();
            Console.WriteLine("=== Other rationals ===");
            var fifth = Surr.FromRational(1, 5);
            Console.WriteLine($"1/5 = {fifth}");
            Console.WriteLine($"1/5 < 1/3 : {fifth < third}");
            Console.WriteLine($"1/5 > 0 : {fifth > 0}");

            var threeSevenths = Surr.FromRational(3, 7);
            Console.WriteLine($"3/7 = {threeSevenths}");
            Console.WriteLine($"3/7 > 1/3 : {threeSevenths > third}");
            Console.WriteLine($"3/7 < 1/2 : {threeSevenths < Surr.Half}");

            Console.WriteLine();
            Console.WriteLine("=== Mixed: rationals + transfinite ===");
            var eps = Surr.InverseOmega;
            Console.WriteLine($"1/5 > 1/ω : {fifth > eps}");
            Console.WriteLine($"1 + 1/5 > 1 + 1/ω : {(new Surr(1) + fifth) > (new Surr(1) + eps)}");
            Console.WriteLine($"ω + 1/5 > ω : {(w + fifth) > w}");
        }
    }
}
