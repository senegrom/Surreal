using System;
using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    /// <summary>
    /// Represents a surreal number as a polynomial in ω:
    /// c_n·ω^n + ... + c_1·ω + c_0 + c_{-1}·ω^{-1} + ...
    /// Coefficients are real surreals (dyadic rationals or FromRational values).
    /// Exponents are integers (positive for transfinite, negative for infinitesimal).
    /// </summary>
    public sealed class OmegaPolynomial
    {
        /// <summary>Terms sorted by descending exponent. Each term is (exponent, coefficient).</summary>
        public readonly List<(int exp, Surr coeff)> Terms;

        public OmegaPolynomial(params (int exp, Surr coeff)[] terms)
        {
            Terms = terms.Where(t => t.coeff != Surr.Zero && !(t.coeff == 0))
                         .OrderByDescending(t => t.exp)
                         .ToList();
        }

        /// <summary>Build the surreal number represented by this polynomial.</summary>
        public Surr ToSurreal()
        {
            if (Terms.Count == 0) return Surr.Zero;

            Surr result = Surr.Zero;
            foreach (var (exp, coeff) in Terms)
            {
                var basis = exp switch
                {
                    0 => new Surr(1),
                    1 => Surr.Omega,
                    2 => Surr.OmegaSquared,
                    -1 => Surr.InverseOmega,
                    _ when exp > 0 => OmegaPowers.Instance.Get(exp),
                    _ => throw new NotImplementedException($"ω^{exp} for exp < -1 not yet supported")
                };

                var term = coeff * basis;
                result = result + term;
            }
            return result;
        }

        public override string ToString()
        {
            if (Terms.Count == 0) return "0";

            var parts = new List<string>();
            foreach (var (exp, coeff) in Terms)
            {
                string coeffStr = coeff.ToString();
                string basisStr = exp switch
                {
                    0 => "",
                    1 => "ω",
                    -1 => "ω⁻¹",
                    _ => $"ω^{exp}"
                };

                if (basisStr == "")
                    parts.Add(coeffStr);
                else if (coeffStr == "1")
                    parts.Add(basisStr);
                else if (coeffStr == "-1")
                    parts.Add($"-{basisStr}");
                else
                    parts.Add($"{coeffStr}·{basisStr}");
            }
            return string.Join(" + ", parts);
        }
    }

    public sealed partial class Surr
    {
        /// <summary>
        /// Create a surreal from an ω-polynomial: e.g., OmegaPoly((2, 3), (1, -1), (0, 5))
        /// represents 3ω² - ω + 5.
        /// </summary>
        public static Surr OmegaPoly(params (int exp, long coeff)[] terms)
        {
            var poly = new OmegaPolynomial(
                terms.Select(t => (t.exp, (Surr)new Surr(t.coeff))).ToArray());
            return poly.ToSurreal();
        }
    }
}
