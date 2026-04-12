using System;
using System.Collections.Generic;

namespace Surreal
{
    public sealed partial class Surr
    {
        /// <summary>Creates the surreal number n / 2^k (a dyadic fraction).</summary>
        public static Surr Dyadic(long n, int k)
        {
            if (k == 0) return new Surr(n);
            if (k < 0) throw new ArgumentException("k must be non-negative");
            if (n % 2 == 0) return Dyadic(n / 2, k - 1);
            return new Surr(new[] { Dyadic(n - 1, k) }, new[] { Dyadic(n + 1, k) });
        }

        /// <summary>
        /// Create a surreal number from rational p/q.
        /// If p/q is a dyadic rational, returns the standard Dyadic form.
        /// Otherwise, uses lazy dyadic approximation generators.
        /// </summary>
        public static Surr FromRational(long p, long q)
        {
            if (q <= 0) throw new ArgumentException("denominator must be positive");
            long g = Gcd(Math.Abs(p), q);
            p /= g; q /= g;
            if ((q & (q - 1)) == 0)
            {
                int exp = 0;
                long tmp = q;
                while (tmp > 1) { tmp >>= 1; exp++; }
                return Dyadic(p, exp);
            }
            string name = q == 1 ? $"{p}" : $"{p}/{q}";
            var gen = new LazyDyadicApprox(p, q);
            return new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
        }

        /// <summary>
        /// Create a surreal number for √n.
        /// Uses binary search with predicate "mid² &lt; n" to generate dyadic approximations.
        /// </summary>
        public static Surr FromSqrt(long n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            // Check for perfect squares
            long root = (long)Math.Sqrt(n);
            if (root * root == n) return new Surr(root);
            if ((root + 1) * (root + 1) == n) return new Surr(root + 1);

            // Predicate: is midNum/2^exp < √n?  ↔  midNum² < n * 4^exp
            var gen = new LazyDyadicApprox(
                (midNum, exp) => midNum * midNum < n * (1L << (2 * exp)),
                root,
                $"sqrt:{n}");

            string name = $"√{n}";
            return new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
        }

        /// <summary>
        /// Create a surreal from a general Dedekind cut predicate among dyadics.
        /// The predicate tests: is midNum/2^exp below the target value?
        /// </summary>
        public static Surr FromPredicate(Func<long, int, bool> isBelow, long floor, string name)
        {
            var gen = new LazyDyadicApprox(isBelow, floor, $"pred:{name}");
            return new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
        }

        /// <summary>
        /// π — defined by predicate: is mid &lt; π? Uses the Leibniz bound π &gt; 3 and π &lt; 4,
        /// refined by checking mid² &lt; known rational bounds of π².
        /// We use π² ≈ 9.8696 so mid² &lt; 9.8696 ↔ mid² * 10000 &lt; 98696.
        /// </summary>
        public static Surr Pi()
        {
            // π defined via Dedekind cut: is midNum/2^exp < π?
            // Uses System.Math.PI (double precision ≈ 15 significant digits)
            return FromPredicate(
                (midNum, exp) => midNum < Math.PI * (1L << exp),
                3,
                "π");
        }

        #region Well-known constants
        public static readonly Surr Half = Dyadic(1, 1);
        public static readonly Surr Zero = new Surr(0L);

        public static readonly Surr Omega = new(
            NaturalNumbers.Instance, null,
            null, null,
            "ω");

        public static readonly Surr InverseOmega = new(
            null, new List<Surr> { Zero },
            PositivePowersOfHalf.Instance, null,
            "1/ω");

        private static readonly OmegaPowersOfHalf _omegaPowersOfHalf = new();

        /// <summary>ω² = {0, ω, 2ω, 3ω, ... | }</summary>
        public static readonly Surr OmegaSquared = new(
            OmegaMultiples.Instance, null,
            null, null,
            "ω²");

        /// <summary>ω^ω = {1, ω, ω², ω³, ... | }</summary>
        public static readonly Surr OmegaToOmega = new(
            OmegaPowers.Instance, null,
            null, null,
            "ω^ω");

        /// <summary>ω/2 = {naturals | ω, ω-1, ω-2, ...}</summary>
        public static readonly Surr OmegaHalf = new(
            NaturalNumbers.Instance, null,
            OmegaMinusNaturals.Instance, null,
            "ω/2");

        /// <summary>
        /// √ω — the surreal whose square is ω. Greater than all finite integers, less than ω.
        /// Constructed as {naturals | ω, ω/2, ω/4, ω/8, ...} — proper right options via division.
        /// </summary>
        public static readonly Surr SqrtOmega = new(
            NaturalNumbers.Instance, null,
            _omegaPowersOfHalf, null,
            "√ω");
        #endregion
    }
}
