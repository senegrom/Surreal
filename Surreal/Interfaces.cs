using System;
using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public interface IInfiniteSet
    {
        bool HasElementGreaterOrEqual(Surr target);
        bool HasElementLessOrEqual(Surr target);
        string DisplayName { get; }
    }

    public sealed class NaturalNumbers : IInfiniteSet
    {
        public static readonly NaturalNumbers Instance = new();
        public string DisplayName => "0,1,2,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Some n >= target iff target is not transfinite (not ω or beyond).
            // Works for dyadics, rationals like 1/3, and any finite real.
            // We test: is 0 >= target? is 1 >= target? Only need to check small n.
            // If target <= some small n, we're done. For efficiency, check up to a bound.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true; // any finite dyadic — some n works

            // Target is non-dyadic (e.g., 1/3) or transfinite (ω).
            // Check: is target <= 0? <= 1? ... <= 1000?
            // For 1/3: target <= 1 is true. For ω: target <= n is always false.
            for (int n = 0; n <= 100; n++)
                if (target <= n) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // 0 is in the set. True iff 0 <= target.
            return new Surr(0) <= target;
        }
    }

    public sealed class PositivePowersOfHalf : IInfiniteSet
    {
        public static readonly PositivePowersOfHalf Instance = new();
        public string DisplayName => "1,1/2,1/4,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Largest element is 1. True iff target <= 1.
            return target <= new Surr(1);
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Elements approach 0+. True iff target > 0 (some 1/2^n is small enough).
            // Check: is 1 <= target? 1/2 <= target? 1/4 <= target? ...
            for (int k = 0; k <= 50; k++)
                if (Surr.Dyadic(1, k) <= target) return true;
            return false;
        }
    }

    /// <summary>
    /// Lazy generator for dyadic approximations of a rational p/q.
    /// Generates lower and upper approximations on demand via binary search.
    /// Integer arithmetic is used only during generation (to determine which
    /// side each midpoint falls on). All comparison queries use surreal <=.
    /// </summary>
    public sealed class LazyDyadicApprox
    {
        private readonly long _p, _q;
        private long _loNum, _hiNum;
        private int _exp;
        private readonly int _maxDepth;

        public readonly List<Surr> Lower = new();
        public readonly List<Surr> Upper = new();

        public LazyDyadicApprox(long p, long q, int maxDepth = 15)
        {
            _p = p; _q = q; _maxDepth = maxDepth;

            long intPart = p >= 0 ? p / q : (p / q - 1);
            if (intPart * q > p) intPart--;

            _loNum = intPart;
            _hiNum = intPart + 1;
            _exp = 0;

            Lower.Add(Surr.GetInt(intPart));
            Upper.Add(Surr.GetInt(intPart + 1));
        }

        /// <summary>Generate approximations until we have at least 'count' on each side, or hit maxDepth.</summary>
        public void EnsureTotal(int count)
        {
            while ((Lower.Count < count || Upper.Count < count) && _exp < _maxDepth)
                GenerateNext();
        }

        private void GenerateNext()
        {
            if (_exp >= _maxDepth) return;
            _exp++;
            _loNum *= 2;
            _hiNum *= 2;
            long midNum = _loNum + 1;

            // p/q < midNum/2^exp ?  ↔  p * 2^exp < midNum * q
            bool pqLessThanMid = _p * (1L << _exp) < midNum * _q;
            var midSurr = Surr.Dyadic(midNum, _exp);

            if (pqLessThanMid)
            {
                Upper.Add(midSurr);
                _hiNum = midNum;
            }
            else
            {
                Lower.Add(midSurr);
                _loNum = midNum;
            }
        }

        public bool CanGenerateMore => _exp < _maxDepth;
    }

    /// <summary>Lower dyadic approximations of p/q (all elements < p/q, approaching from below).</summary>
    public sealed class DyadicApproxBelow : IInfiniteSet
    {
        private readonly LazyDyadicApprox _gen;
        public string DisplayName { get; }

        public DyadicApproxBelow(LazyDyadicApprox gen, string displayName)
        {
            _gen = gen;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Check already-generated approximations
            for (int i = 0; i < _gen.Lower.Count; i++)
                if (target <= _gen.Lower[i]) return true;

            // Generate more on demand
            while (_gen.CanGenerateMore)
            {
                int prevCount = _gen.Lower.Count;
                _gen.EnsureTotal(_gen.Lower.Count + 1);
                // Check any newly added lower approximation
                for (int i = prevCount; i < _gen.Lower.Count; i++)
                    if (target <= _gen.Lower[i]) return true;

                // Stopping: if target >= latest upper bound, no lower approx can reach it
                if (_gen.Upper.Count > 0 && _gen.Upper[^1] <= target)
                    return false;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // First element (floor) is always small. Check it.
            if (_gen.Lower.Count > 0 && _gen.Lower[0] <= target) return true;

            for (int i = 1; i < _gen.Lower.Count; i++)
                if (_gen.Lower[i] <= target) return true;

            while (_gen.CanGenerateMore)
            {
                int prevCount = _gen.Lower.Count;
                _gen.EnsureTotal(_gen.Lower.Count + 1);
                for (int i = prevCount; i < _gen.Lower.Count; i++)
                    if (_gen.Lower[i] <= target) return true;
            }
            return false;
        }
    }

    /// <summary>Upper dyadic approximations of p/q (all elements > p/q, approaching from above).</summary>
    public sealed class DyadicApproxAbove : IInfiniteSet
    {
        private readonly LazyDyadicApprox _gen;
        public string DisplayName { get; }

        public DyadicApproxAbove(LazyDyadicApprox gen, string displayName)
        {
            _gen = gen;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // First element (ceil) is always large. Check it.
            if (_gen.Upper.Count > 0 && target <= _gen.Upper[0]) return true;

            for (int i = 1; i < _gen.Upper.Count; i++)
                if (target <= _gen.Upper[i]) return true;

            while (_gen.CanGenerateMore)
            {
                int prevCount = _gen.Upper.Count;
                _gen.EnsureTotal(_gen.Upper.Count + 1);
                for (int i = prevCount; i < _gen.Upper.Count; i++)
                    if (target <= _gen.Upper[i]) return true;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            for (int i = 0; i < _gen.Upper.Count; i++)
                if (_gen.Upper[i] <= target) return true;

            while (_gen.CanGenerateMore)
            {
                int prevCount = _gen.Upper.Count;
                _gen.EnsureTotal(_gen.Upper.Count + 1);
                for (int i = prevCount; i < _gen.Upper.Count; i++)
                    if (_gen.Upper[i] <= target) return true;

                // Stopping: if latest lower bound >= target, no upper approx is <= target
                if (_gen.Lower.Count > 0 && target <= _gen.Lower[^1])
                    return false;
            }
            return false;
        }
    }
}
