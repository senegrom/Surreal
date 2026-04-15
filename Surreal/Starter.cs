using System;

namespace Surreal
{
    class Starter
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Surreal Numbers Demo ===\n");

            // Basic arithmetic
            Console.WriteLine("--- Arithmetic ---");
            Console.WriteLine($"3 + 4 = {new Surr(3) + new Surr(4)}");
            Console.WriteLine($"1/2 * 1/2 = {Surr.Half * Surr.Half}");
            Console.WriteLine($"4 * 4 = {new Surr(4) * new Surr(4)}");
            Console.WriteLine($"1/4 * 4 = {Surr.Dyadic(1, 2) * 4}");
            Console.WriteLine($"6 / 3 = {new Surr(6) / new Surr(3)}");
            Console.WriteLine($"1 / 3 = {new Surr(1) / new Surr(3)}");

            // Irrationals and transcendentals
            Console.WriteLine("\n--- Irrationals ---");
            Console.WriteLine($"√2 = {Surr.FromSqrt(2)}");
            Console.WriteLine($"√2 * √2 = {Surr.FromSqrt(2) * Surr.FromSqrt(2)}");
            Console.WriteLine($"√2 * √3 = {Surr.FromSqrt(2) * Surr.FromSqrt(3)}");
            Console.WriteLine($"π = {Surr.Pi()}");
            Console.WriteLine($"e = {Surr.E()}");
            Console.WriteLine($"π > e : {Surr.Pi() > Surr.E()}");

            // Transfinite
            Console.WriteLine("\n--- Transfinite ---");
            Console.WriteLine($"ω > 1000000 : {Surr.Omega > 1000000}");
            Console.WriteLine($"√ω * √ω = {Surr.SqrtOmega * Surr.SqrtOmega}");
            Console.WriteLine($"ω * ω = {Surr.Omega * Surr.Omega}");
            Console.WriteLine($"ω^ε₀ = ε₀ : {Surr.Pow(Surr.Omega, Surr.EpsilonNaught) == Surr.EpsilonNaught}");

            // Infinitesimals
            Console.WriteLine("\n--- Infinitesimals ---");
            Console.WriteLine($"1/ω > 0 : {Surr.InverseOmega > 0}");
            Console.WriteLine($"1/Γ₀ > 0 : {Surr.InverseGamma0 > 0}");
            Console.WriteLine($"1/Γ₀ < 1/ω : {Surr.InverseGamma0 < Surr.InverseOmega}");

            // Games
            Console.WriteLine("\n--- Games ---");
            Console.WriteLine($"* fuzzy with 0 : {!(Surr.Star <= 0) && !(Surr.Star >= 0)}");
            Console.WriteLine($"* + * = 0 : {Surr.Star + Surr.Star == 0}");
            Console.WriteLine($"↑ > 0 : {Surr.Up > 0}");
            Console.WriteLine($"↓ < 0 : {Surr.Down < 0}");
            Console.WriteLine($"*2 ⊗ *3 = * : {Surr.NimMultiply(Surr.Nimber(2), Surr.Nimber(3)) == Surr.Star}");

            // FOIL identity
            Console.WriteLine("\n--- Algebraic Identity ---");
            var sw = Surr.SqrtOmega;
            var s2 = Surr.FromSqrt(2);
            Console.WriteLine($"(√ω-√2)(√ω+√2) + 2 = ω : {(sw - s2) * (sw + s2) + new Surr(2) == Surr.Omega}");

            // Structural
            Console.WriteLine("\n--- Structure ---");
            Console.WriteLine($"Birthday(1/2) = {Surr.Birthday(Surr.Half)}");
            Console.WriteLine($"Birthday(3/4) = {Surr.Birthday(Surr.Dyadic(3, 2))}");
            Console.WriteLine($"SignExpansion(3/4) = {Surr.SignExpansion(Surr.Dyadic(3, 2))}");

            // Full number line
            Console.WriteLine("\n--- Number Line ---");
            Console.WriteLine("0 < 1/Γ₀ < 1/ε₀ < 1/ω < √2 < e < π < 100");
            Console.WriteLine("  < log(ω) < √ω < ω/2 < ω < 2ω < ω² < ω^ω");
            Console.WriteLine("  < ε₀ < ε₁ < ζ₀ < Γ₀");
        }
    }
}
