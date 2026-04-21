using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Surreal
{
    /// <summary>
    /// Polynomial in one variable with surreal coefficients: c₀ + c₁·x + c₂·x² + ...
    /// Immutable. Supports +, -, *, Evaluate(x), Derivative, and Pow for non-negative integer exponents.
    /// </summary>
    public sealed class SurrPoly
    {
        /// <summary>Coefficients in ascending order: Coeffs[i] is the coefficient of xⁱ.</summary>
        public readonly IReadOnlyList<Surr> Coeffs;

        /// <summary>Degree (highest power with non-zero coefficient). -1 for the zero polynomial.</summary>
        public int Degree
        {
            get
            {
                for (int i = Coeffs.Count - 1; i >= 0; i--)
                    if (!(Coeffs[i] == Surr.Zero)) return i;
                return -1;
            }
        }

        public bool IsZero => Degree < 0;

        public SurrPoly(params Surr[] coeffs) : this((IEnumerable<Surr>)coeffs) { }

        public SurrPoly(IEnumerable<Surr> coeffs)
        {
            var list = coeffs.ToList();
            // Trim trailing zeros to keep representation canonical-ish
            while (list.Count > 0 && list[^1] == Surr.Zero) list.RemoveAt(list.Count - 1);
            Coeffs = list;
        }

        /// <summary>The polynomial x (= 0 + 1·x).</summary>
        public static readonly SurrPoly X = new(Surr.Zero, new Surr(1));

        /// <summary>Constant polynomial c (= c + 0·x).</summary>
        public static SurrPoly Constant(Surr c) => new(c);

        /// <summary>Evaluate the polynomial at a surreal point using Horner's method.</summary>
        public Surr Evaluate(Surr x)
        {
            if (Coeffs.Count == 0) return Surr.Zero;
            var result = Coeffs[^1];
            for (int i = Coeffs.Count - 2; i >= 0; i--)
                result = result * x + Coeffs[i];
            return result;
        }

        public static SurrPoly operator +(SurrPoly a, SurrPoly b)
        {
            int n = Math.Max(a.Coeffs.Count, b.Coeffs.Count);
            var sum = new Surr[n];
            for (int i = 0; i < n; i++)
            {
                var ai = i < a.Coeffs.Count ? a.Coeffs[i] : Surr.Zero;
                var bi = i < b.Coeffs.Count ? b.Coeffs[i] : Surr.Zero;
                sum[i] = ai + bi;
            }
            return new SurrPoly(sum);
        }

        public static SurrPoly operator -(SurrPoly p)
        {
            var neg = new Surr[p.Coeffs.Count];
            for (int i = 0; i < p.Coeffs.Count; i++) neg[i] = -p.Coeffs[i];
            return new SurrPoly(neg);
        }

        public static SurrPoly operator -(SurrPoly a, SurrPoly b) => a + (-b);

        public static SurrPoly operator *(SurrPoly a, SurrPoly b)
        {
            if (a.IsZero || b.IsZero) return new SurrPoly();
            int n = a.Coeffs.Count + b.Coeffs.Count - 1;
            var prod = new Surr[n];
            for (int i = 0; i < n; i++) prod[i] = Surr.Zero;
            for (int i = 0; i < a.Coeffs.Count; i++)
                for (int j = 0; j < b.Coeffs.Count; j++)
                    prod[i + j] = prod[i + j] + a.Coeffs[i] * b.Coeffs[j];
            return new SurrPoly(prod);
        }

        public static SurrPoly operator *(Surr c, SurrPoly p) => new SurrPoly(new[] { c }) * p;
        public static SurrPoly operator *(SurrPoly p, Surr c) => c * p;

        /// <summary>p^n for non-negative integer n via repeated squaring.</summary>
        public SurrPoly Pow(int n)
        {
            if (n < 0) throw new ArgumentException("Negative exponent not supported for polynomials");
            if (n == 0) return new SurrPoly(new Surr(1));
            var result = new SurrPoly(new Surr(1));
            var basePoly = this;
            while (n > 0)
            {
                if ((n & 1) == 1) result *= basePoly;
                n >>= 1;
                if (n > 0) basePoly *= basePoly;
            }
            return result;
        }

        /// <summary>Formal derivative d/dx: Σ i·cᵢ·x^(i-1).</summary>
        public SurrPoly Derivative()
        {
            if (Coeffs.Count <= 1) return new SurrPoly();
            var d = new Surr[Coeffs.Count - 1];
            for (int i = 1; i < Coeffs.Count; i++)
                d[i - 1] = new Surr(i) * Coeffs[i];
            return new SurrPoly(d);
        }

        public override string ToString()
        {
            if (IsZero) return "0";
            var sb = new StringBuilder();
            for (int i = Coeffs.Count - 1; i >= 0; i--)
            {
                if (Coeffs[i] == Surr.Zero) continue;
                if (sb.Length > 0) sb.Append(" + ");
                if (i == 0) sb.Append(Coeffs[i]);
                else if (i == 1) sb.Append($"{Coeffs[i]}·x");
                else sb.Append($"{Coeffs[i]}·x^{i}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Solve ax² + bx + c = 0 for x using the quadratic formula.
        /// Returns (x₁, x₂) where x₁ ≤ x₂. Throws if the discriminant is negative (no real roots).
        /// </summary>
        public static (Surr, Surr) SolveQuadratic(Surr a, Surr b, Surr c)
        {
            if (a == Surr.Zero) throw new ArgumentException("a must be non-zero (not a quadratic)");
            var disc = b * b - new Surr(4) * a * c;
            if (disc < Surr.Zero) throw new ArgumentException("Negative discriminant; no real surreal roots");
            var sqrtDisc = Surr.Sqrt(disc);
            var twoA = new Surr(2) * a;
            var minus = (-b - sqrtDisc) / twoA;
            var plus = (-b + sqrtDisc) / twoA;
            return (minus, plus);
        }

        /// <summary>Solve this polynomial = 0 when it has degree 2. Shortcut for SolveQuadratic(Coeffs[2], Coeffs[1], Coeffs[0]).</summary>
        public (Surr, Surr) SolveQuadratic()
        {
            if (Degree != 2) throw new InvalidOperationException($"SolveQuadratic requires degree 2, got {Degree}");
            return SolveQuadratic(Coeffs[2], Coeffs[1], Coeffs[0]);
        }
    }
}
