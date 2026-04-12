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

        private static long Gcd(long a, long b)
        {
            while (b != 0) { (a, b) = (b, a % b); }
            return a;
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

        /// <summary>
        /// √ω — the surreal whose square is ω. Greater than all finite integers, less than ω.
        /// Constructed as {naturals | ω} with algebraic tag "sqrt:omega".
        /// Multiplication rule: √ω * √ω = ω, √ω * √n = √(nω).
        /// </summary>
        public static readonly Surr SqrtOmega = new(
            NaturalNumbers.Instance, null,
            null, new List<Surr> { Omega },
            "√ω");
        #endregion
    }
}
