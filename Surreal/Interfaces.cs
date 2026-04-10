using System.Collections.Generic;

namespace Surreal
{
    public interface IInfiniteSet
    {
        bool HasElementGreaterOrEqual(Surr target);
        bool HasElementLessOrEqual(Surr target);
        string DisplayName { get; }
        /// <summary>Return a few concrete (dyadic) elements from this set for cross-term computation.</summary>
        Surr[] SampleElements(int count);
    }

    public sealed class NaturalNumbers : IInfiniteSet
    {
        public static readonly NaturalNumbers Instance = new();
        public string DisplayName => "0,1,2,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true;
            // Non-dyadic or transfinite. Check small integers via surreal <=.
            for (int n = 0; n <= 100; n++)
                if (target <= n) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return Surr.Zero <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Surr.GetInt(i);
            return result;
        }
    }

    public sealed class PositivePowersOfHalf : IInfiniteSet
    {
        public static readonly PositivePowersOfHalf Instance = new();
        public string DisplayName => "1,1/2,1/4,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            return target <= new Surr(1);
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            for (int k = 0; k <= 50; k++)
                if (Surr.Dyadic(1, k) <= target) return true;
            return false;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[System.Math.Min(count, 20)];
            for (int i = 0; i < result.Length; i++) result[i] = Surr.Dyadic(1, i);
            return result;
        }
    }

    /// <summary>The set {0+p/q, 1+p/q, 2+p/q, ...} for a rational offset p/q.</summary>
    public sealed class ShiftedNaturals : IInfiniteSet
    {
        private readonly long _p, _q;
        public string DisplayName => $"0+{_p}/{_q},1+{_p}/{_q},...";

        public ShiftedNaturals(long p, long q) { _p = p; _q = q; }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // {n + p/q} for n=0,1,2,... Some n+p/q >= target iff target is finite.
            // For finite target: some large n works.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true;
            var gen = GeneratorHelper.GetGenerator(target);
            if (gen != null) return true; // target is a finite rational
            // Transfinite: check small values via surreal <=
            for (int n = 0; n <= 10; n++)
                if (target <= new Surr(n) + Surr.FromRational(_p, _q)) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return Surr.FromRational(_p, _q) <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++)
                result[i] = Surr.GetInt(i) + Surr.FromRational(_p, _q);
            return result;
        }
    }

    /// <summary>
    /// Lazy generator for dyadic approximations of a rational p/q via binary search.
    /// The generating RULE is the binary search process itself. The (P, Q) parameters
    /// identify the rule — two generators with the same (P, Q) converge to the same value.
    /// Integer arithmetic is used only during generation. All comparisons use surreal <=.
    /// </summary>
    public sealed class LazyDyadicApprox
    {
        /// <summary>The generating rule identity (reduced fraction).</summary>
        public readonly long P, Q;

        private long _loNum, _hiNum;
        private int _exp;

        public readonly List<Surr> Lower = new();
        public readonly List<Surr> Upper = new();

        public LazyDyadicApprox(long p, long q)
        {
            P = p; Q = q;

            long intPart = p >= 0 ? p / q : (p / q - 1);
            if (intPart * q > p) intPart--;

            _loNum = intPart;
            _hiNum = intPart + 1;
            _exp = 0;

            Lower.Add(Surr.GetInt(intPart));
            Upper.Add(Surr.GetInt(intPart + 1));
        }

        /// <summary>Whether two generators have the same rule (converge to the same value).</summary>
        public bool SameRule(LazyDyadicApprox other) => P == other.P && Q == other.Q;

        public void EnsureDepth(int totalElements)
        {
            while (Lower.Count + Upper.Count < totalElements + 2) // +2 for initial bounds
                GenerateNext();
        }

        public void GenerateNext()
        {
            _exp++;
            _loNum *= 2;
            _hiNum *= 2;
            long midNum = _loNum + 1;

            bool pqLessThanMid = P * (1L << _exp) < midNum * Q;
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

        /// <summary>
        /// Compare two generators' target values by interleaving approximations.
        /// Returns negative if this &lt; other, positive if this &gt; other.
        /// Terminates for distinct values. Must check SameRule first for equal values.
        /// </summary>
        public int InterleaveCompare(LazyDyadicApprox other)
        {
            while (true)
            {
                GenerateNext();
                other.GenerateNext();

                // Our upper bound vs their lower bound (surreal <= on dyadics → fast)
                if (Upper[^1] <= other.Lower[^1]) return -1; // we < them
                if (other.Upper[^1] <= Lower[^1]) return 1;  // we > them
            }
        }
    }

    /// <summary>
    /// Helper to extract a LazyDyadicApprox generator from a surreal, if it has one.
    /// </summary>
    internal static class GeneratorHelper
    {
        internal static LazyDyadicApprox GetGenerator(Surr target)
        {
            // A surreal from FromRational has DyadicApproxBelow as leftInf
            return (target.LeftInf as DyadicApproxBelow)?.Gen
                ?? (target.RightInf as DyadicApproxAbove)?.Gen;
        }
    }

    /// <summary>Lower dyadic approximations (all elements &lt; p/q, approaching from below).</summary>
    public sealed class DyadicApproxBelow : IInfiniteSet
    {
        internal readonly LazyDyadicApprox Gen;
        public string DisplayName { get; }

        public DyadicApproxBelow(LazyDyadicApprox gen, string displayName)
        {
            Gen = gen;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // "Exists d < ourValue where d >= target?"

            // Case 1: target has same generating rule → no (all d < ourValue, target IS ourValue)
            var targetGen = GeneratorHelper.GetGenerator(target);
            if (targetGen != null)
            {
                if (Gen.SameRule(targetGen)) return false;
                // Case 2: different rule → true iff ourValue > targetValue
                return Gen.InterleaveCompare(targetGen) > 0;
            }

            // Case 3: target is dyadic or other — lazy generate and check via surreal <=
            for (int i = 0; i < Gen.Lower.Count; i++)
                if (target <= Gen.Lower[i]) return true;

            while (true)
            {
                int prev = Gen.Lower.Count;
                Gen.GenerateNext();
                for (int i = prev; i < Gen.Lower.Count; i++)
                    if (target <= Gen.Lower[i]) return true;
                // Stopping: upper bound dropped to or below target
                if (Gen.Upper[^1] <= target) return false;
            }
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return Gen.Lower[0] <= target;
        }

        public Surr[] SampleElements(int count)
        {
            Gen.EnsureDepth(count);
            return Gen.Lower.ToArray();
        }
    }

    /// <summary>Upper dyadic approximations (all elements &gt; p/q, approaching from above).</summary>
    public sealed class DyadicApproxAbove : IInfiniteSet
    {
        internal readonly LazyDyadicApprox Gen;
        public string DisplayName { get; }

        public DyadicApproxAbove(LazyDyadicApprox gen, string displayName)
        {
            Gen = gen;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // "Exists d > ourValue where d >= target?"
            // The first element (ceil) is large. Almost always true.
            return target <= Gen.Upper[0];
        }

        public Surr[] SampleElements(int count)
        {
            Gen.EnsureDepth(count);
            return Gen.Upper.ToArray();
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // "Exists d > ourValue where d <= target?"

            // Case 1: same rule → no (all d > ourValue, target IS ourValue)
            var targetGen = GeneratorHelper.GetGenerator(target);
            if (targetGen != null)
            {
                if (Gen.SameRule(targetGen)) return false;
                // Case 2: different rule → true iff ourValue < targetValue
                return Gen.InterleaveCompare(targetGen) < 0;
            }

            // Case 3: lazy generate and check via surreal <=
            for (int i = 0; i < Gen.Upper.Count; i++)
                if (Gen.Upper[i] <= target) return true;

            while (true)
            {
                int prev = Gen.Upper.Count;
                Gen.GenerateNext();
                for (int i = prev; i < Gen.Upper.Count; i++)
                    if (Gen.Upper[i] <= target) return true;
                // Stopping: lower bound rose to or above target
                if (target <= Gen.Lower[^1]) return false;
            }
        }
    }
}
