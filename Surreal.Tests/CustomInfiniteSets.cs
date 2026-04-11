using System;
using System.Collections.Generic;
using Surreal;

namespace Surreal.Tests
{
    /// <summary>{2, 4, 6, 8, 10, ...} — all positive even integers.</summary>
    public sealed class EvenNaturals : IInfiniteSet
    {
        public static readonly EvenNaturals Instance = new();
        public string DisplayName => "2,4,6,8,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Some even n >= target? True for any finite target.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true;
            // Non-dyadic finite? Check small evens via surreal <=.
            for (int n = 2; n <= 200; n += 2)
                if (target <= n) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Smallest element is 2. True iff 2 <= target.
            return new Surr(2) <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Surr.GetInt(2 * (i + 1));
            return result;
        }
    }

    /// <summary>{k, k+1, k+2, ...} — naturals starting from k.</summary>
    public sealed class NaturalsFrom : IInfiniteSet
    {
        private readonly long _start;
        public NaturalsFrom(long start) { _start = start; }
        public string DisplayName => $"{_start},{_start + 1},{_start + 2},...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true;
            for (long n = _start; n <= _start + 100; n++)
                if (target <= n) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return new Surr(_start) <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Surr.GetInt(_start + i);
            return result;
        }
    }

    /// <summary>{1/2, 1/3, 1/5, 1/7, 1/11, ...} — reciprocals of primes.</summary>
    public sealed class InversePrimes : IInfiniteSet
    {
        public static readonly InversePrimes Instance = new();
        public string DisplayName => "1/2,1/3,1/5,1/7,...";

        private static readonly int[] Primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Largest element is 1/2. True iff target <= 1/2.
            return target <= Surr.Half;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Elements {1/2, 1/3, 1/5, ...} approach 0+. Since primes are unbounded,
            // 1/p gets arbitrarily small. True iff target is a positive real number.
            // False for infinitesimals (smaller than all positive reals) and non-positives.

            // Positive dyadic → true (some large prime gives 1/p ≤ target)
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Dyad(0, 0)) > 0;

            // Positive rational (has generator) → true
            var gen = GeneratorHelper.GetGenerator(target);
            if (gen != null)
                return gen.P > 0; // positive rational → primes are unbounded

            // Unknown (infinitesimal or transfinite) — check concretely
            foreach (var p in Primes)
            {
                var invP = p == 2 ? Surr.Half : Surr.FromRational(1, p);
                if (invP <= target) return true;
            }
            return false;
        }

        public Surr[] SampleElements(int count)
        {
            var n = Math.Min(count, Primes.Length);
            var result = new Surr[n];
            for (int i = 0; i < n; i++)
                result[i] = Primes[i] == 2 ? Surr.Half : Surr.FromRational(1, Primes[i]);
            return result;
        }
    }
}
