using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public interface IInfiniteSet
    {
        bool HasElementGreaterOrEqual(Surr target);
        bool HasElementLessOrEqual(Surr target);
        string DisplayName { get; }
        Surr[] SampleElements(int count);
    }

    /// <summary>
    /// Wraps any IInfiniteSet and negates all its elements.
    /// If inner = {a,b,c,...} then NegatedSet = {-a,-b,-c,...}.
    /// "Exists -x >= target" ↔ "Exists x <= -target" in the inner set.
    /// </summary>
    public sealed class NegatedSet : IInfiniteSet
    {
        public readonly IInfiniteSet Inner;
        public NegatedSet(IInfiniteSet inner) { Inner = inner; }

        public string DisplayName => $"-({Inner.DisplayName})";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // "Exists -x ≥ target where x ∈ Inner" ↔ "Exists x ≤ -target"
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return Inner.HasElementLessOrEqual(Surr.Dyadic(-val.Value.Num, val.Value.Exp));
            // For non-evaluable target: conservative answer based on inner set properties
            // If inner has elements ≤ everything (like NaturalNumbers has 0), negated has 0 too
            return Inner.HasElementLessOrEqual(Surr.Zero); // conservative: negated set contains -0=0
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return Inner.HasElementGreaterOrEqual(Surr.Dyadic(-val.Value.Num, val.Value.Exp));
            return Inner.HasElementGreaterOrEqual(Surr.Zero);
        }

        public Surr[] SampleElements(int count)
        {
            var samples = Inner.SampleElements(count);
            var result = new List<Surr>();
            foreach (var s in samples)
            {
                var val = Surr.TryEvaluate(s);
                if (val.HasValue)
                    result.Add(Surr.Dyadic(-val.Value.Num, val.Value.Exp));
                // Skip non-evaluable samples to avoid recursion
            }
            return result.ToArray();
        }
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

    /// <summary>The set {ω, ω-1, ω-2, ...} — ω minus each natural number.</summary>
    public sealed class OmegaMinusNaturals : IInfiniteSet
    {
        public static readonly OmegaMinusNaturals Instance = new();
        public string DisplayName => "ω,ω-1,ω-2,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // ω is in the set. True iff target ≤ ω.
            return target <= Surr.Omega;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Check ω-n for n=0,1,2,... Returns true if any ω-n ≤ target.
            // For target between naturals and ω-k: ω-n > target for all finite n → false.
            // For target ≥ ω-k for some k: ω-k ≤ target → true.
            for (int n = 0; n <= 50; n++)
                if ((Surr.Omega - n) <= target) return true;
            return false;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++)
                result[i] = Surr.Omega - new Surr(i);
            return result;
        }
    }

    /// <summary>The set {ω, ω/2, ω/4, ω/8, ...} — ω divided by powers of 2.</summary>
    public sealed class OmegaPowersOfHalf : IInfiniteSet
    {
        private readonly List<Surr> _cache = new();

        public string DisplayName => "ω,ω/2,ω/4,...";

        public Surr Get(int k)
        {
            while (_cache.Count <= k)
            {
                if (_cache.Count == 0)
                    _cache.Add(Surr.Omega);
                else
                {
                    // ω/2^k = {naturals | ω/2^(k-1), ω/2^(k-1) - 1, ω/2^(k-1) - 2, ...}
                    // Simplified: {naturals | previous}
                    var prev = _cache[^1];
                    _cache.Add(new Surr(
                        NaturalNumbers.Instance, null,
                        OmegaMinusNaturals.Instance, new List<Surr> { prev },
                        $"ω/{1L << _cache.Count}"));
                }
            }
            return _cache[k];
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // ω is in the set. True iff target ≤ ω.
            return target <= Surr.Omega;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Elements decrease: ω > ω/2 > ω/4 > ... All > all naturals.
            // True iff target > some ω/2^k, i.e., target is "comparable to ω" level.
            for (int k = 0; k <= 10; k++)
                if (Get(k) <= target) return true;
            return false;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Get(i);
            return result;
        }
    }

    /// <summary>The set {0, ω, 2ω, 3ω, ...} — multiples of ω. Used as left options of ω².</summary>
    public sealed class OmegaMultiples : IInfiniteSet
    {
        public static readonly OmegaMultiples Instance = new();
        private readonly List<Surr> _cache = new();
        public string DisplayName => "0,ω,2ω,3ω,...";

        public Surr Get(int n)
        {
            while (_cache.Count <= n)
            {
                if (_cache.Count == 0)
                    _cache.Add(Surr.Zero);
                else
                    // n·ω = (n-1)·ω + ω, represented as {(n-1)·ω, naturals | }
                    // with NaturalNumbers since n·ω > all finite integers
                    _cache.Add(new Surr(
                        NaturalNumbers.Instance, new List<Surr> { _cache[^1] },
                        null, null,
                        $"{_cache.Count}ω"));
            }
            return _cache[n];
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Some n·ω ≥ target? True unless target is ω² or beyond.
            // Check: is target ≤ some small n·ω?
            for (int n = 0; n <= 10; n++)
                if (target <= Get(n)) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // 0 is in the set.
            return Surr.Zero <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Get(i);
            return result;
        }
    }

    /// <summary>The set {1, ω, ω², ω³, ...} — powers of ω. Used as left options of ω^ω.</summary>
    public sealed class OmegaPowers : IInfiniteSet
    {
        public static readonly OmegaPowers Instance = new();
        private readonly List<Surr> _cache = new();
        public string DisplayName => "1,ω,ω²,ω³,...";

        public Surr Get(int n)
        {
            while (_cache.Count <= n)
            {
                if (_cache.Count == 0)
                    _cache.Add(new Surr(1));
                else if (_cache.Count == 1)
                    _cache.Add(Surr.Omega);
                else
                    // ω^n has {all ω^(n-1) multiples | } as left, nothing right
                    _cache.Add(new Surr(
                        OmegaMultiples.Instance, new List<Surr> { _cache[^1] },
                        null, null,
                        $"ω^{_cache.Count}"));
            }
            return _cache[n];
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            for (int n = 0; n <= 5; n++)
                if (target <= Get(n)) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return new Surr(1) <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new Surr[count];
            for (int i = 0; i < count; i++) result[i] = Get(i);
            return result;
        }
    }

    /// <summary>
    /// Lazy generator for dyadic approximations via binary search.
    /// The generating RULE is a predicate that decides, for each dyadic midpoint,
    /// whether it is below the target value. Different predicates define different numbers:
    /// rationals (p/q), square roots (√n), or any Dedekind cut among dyadics.
    /// Integer arithmetic is used only during generation. All comparisons use surreal <=.
    /// </summary>
    public sealed class LazyDyadicApprox
    {
        /// <summary>String tag identifying the generating rule. Same tag = same value.</summary>
        public readonly string Tag;

        /// <summary>For rational generators: the numerator and denominator. Null otherwise.</summary>
        public readonly long? P, Q;

        /// <summary>Predicate: is midNum/2^exp below our target? Used during generation only.</summary>
        private readonly System.Func<long, int, bool> _isBelow;

        private long _loNum, _hiNum;
        private int _exp;

        public readonly List<Surr> Lower = new();
        public readonly List<Surr> Upper = new();

        /// <summary>General constructor: provide a predicate and floor value.</summary>
        public LazyDyadicApprox(System.Func<long, int, bool> isBelow, long floor, string tag)
        {
            Tag = tag;
            _isBelow = isBelow;
            _loNum = floor;
            _hiNum = floor + 1;
            _exp = 0;
            Lower.Add(Surr.GetInt(floor));
            Upper.Add(Surr.GetInt(floor + 1));
        }

        /// <summary>Convenience constructor for rational p/q.</summary>
        public LazyDyadicApprox(long p, long q) : this(
            (midNum, exp) => p * (1L << exp) > midNum * q,
            FloorRat(p, q),
            $"rat:{p}/{q}")
        { P = p; Q = q; }

        private static long FloorRat(long p, long q)
        {
            // Floor division: largest integer <= p/q
            long f = p / q;
            if (f * q > p) f--;
            return f;
        }

        /// <summary>Whether two generators converge to the same value.</summary>
        public bool SameRule(LazyDyadicApprox other) => Tag == other.Tag;

        public void EnsureDepth(int totalElements)
        {
            while (Lower.Count + Upper.Count < totalElements + 2)
                GenerateNext();
        }

        public void GenerateNext()
        {
            _exp++;
            _loNum *= 2;
            _hiNum *= 2;
            long midNum = _loNum + 1;

            var midSurr = Surr.Dyadic(midNum, _exp);

            if (_isBelow(midNum, _exp))
            {
                Lower.Add(midSurr);
                _loNum = midNum;
            }
            else
            {
                Upper.Add(midSurr);
                _hiNum = midNum;
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

                if (Upper[^1] <= other.Lower[^1]) return -1;
                if (other.Upper[^1] <= Lower[^1]) return 1;
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
