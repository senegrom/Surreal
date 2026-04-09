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
        #endregion
    }
}
