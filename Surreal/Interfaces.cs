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
            // "exists -x ≥ target" ≡ "exists x ≤ -target" in Inner.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return Inner.HasElementLessOrEqual(Surr.Dyadic(-val.Value.Num, val.Value.Exp));
            // Non-evaluable target: negate via operator- and delegate.
            return Inner.HasElementLessOrEqual(-target);
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // "exists -x ≤ target" ≡ "exists x ≥ -target" in Inner.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return Inner.HasElementGreaterOrEqual(Surr.Dyadic(-val.Value.Num, val.Value.Exp));
            return Inner.HasElementGreaterOrEqual(-target);
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
                else
                    result.Add(-s); // Non-evaluable: negate structurally
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

    /// <summary>The set {base+0, base+1, base+2, ...} for a transfinite base.</summary>
    public sealed class TransfinitePlusNaturals : IInfiniteSet
    {
        private readonly Surr _base;
        public TransfinitePlusNaturals(Surr baseVal) { _base = baseVal; }
        public string DisplayName => $"{_base}+n";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue) return true; // base > any finite
            // For transfinite target: check if target ≤ base (base+0 = base)
            if (target <= _base) return true;
            // Check if target has symbolic terms suggesting base+k form
            if (target._symbolicTerms != null)
            {
                // If target is base+constant, our set has base+(constant+1) which is ≥
                foreach (var (f, _) in target._symbolicTerms)
                    if (Surr.TryEvaluate(f) is null && _base <= f) return true;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return _base <= target;
        }

        public Surr[] SampleElements(int count)
        {
            // Return just the base to avoid recursion through TransfiniteAdd
            return new[] { _base };
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
                {
                    var prev = _cache[^1];
                    // n·ω = {(n-1)·ω+k : k ∈ ℕ | } — includes shifted naturals above prev
                    _cache.Add(new Surr(
                        new TransfinitePlusNaturals(prev), new List<Surr> { prev },
                        null, null,
                        $"{_cache.Count}ω"));
                }
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

    /// <summary>The set {ε_0, ε_1, ε_2, …} — all finite-indexed epsilon numbers. Left set of ζ_0.</summary>
    public sealed class EpsilonSeq : IInfiniteSet
    {
        public static readonly EpsilonSeq Instance = new();
        public string DisplayName => "ε_0, ε_1, ε_2, ...";
        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Doubling iteration: k = 1, 2, 4, …, 512 covers up to ε_512 in log steps.
            if (target <= Surr.EpsilonNaught) return true;
            for (int k = 1; k <= 512; k *= 2)
                if (target <= Surr.Epsilon(k)) return true;
            return false;
        }
        public bool HasElementLessOrEqual(Surr target) => Surr.EpsilonNaught <= target;
        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Epsilon(i);
            return arr;
        }
    }

    /// <summary>The set {ζ_0, ζ_1, ζ_2, …} — all zeta numbers. Left set of Γ_0.</summary>
    public sealed class ZetaSeq : IInfiniteSet
    {
        public static readonly ZetaSeq Instance = new();
        public string DisplayName => "ζ_0, ζ_1, ζ_2, ...";
        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Doubling iteration covers up to ζ_128 in log steps.
            if (target <= Surr.Zeta0) return true;
            for (int k = 1; k <= 128; k *= 2)
                if (target <= Surr.Zeta(k)) return true;
            return false;
        }
        public bool HasElementLessOrEqual(Surr target) => Surr.Zeta0 <= target;
        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Zeta(i);
            return arr;
        }
    }

    /// <summary>Common stratified-ordinal sampling used by the α-parameterized below-sets. Yields representative β values
    /// from integers through LVO, in increasing order, so callers can filter by `β &lt; α` to get valid left options.</summary>
    internal static class OrdinalSamples
    {
        public static System.Collections.Generic.IEnumerable<Surr> Stratified()
        {
            // Finite integers
            for (int k = 1; k <= 5; k++) yield return Surr.GetInt(k);
            // ω and simple ω-powers
            yield return Surr.Omega;
            yield return Surr.OmegaSquared;
            yield return Surr.OmegaToOmega;
            // Epsilon tower (doubling indices)
            yield return Surr.EpsilonNaught;
            yield return Surr.Epsilon(1);
            yield return Surr.Epsilon(2);
            yield return Surr.Epsilon(4);
            yield return Surr.Epsilon(8);
            yield return Surr.Epsilon(16);
            // Zeta tower
            yield return Surr.Zeta0;
            yield return Surr.Zeta(1);
            yield return Surr.Zeta(4);
            // Gamma tower
            yield return Surr.Gamma0;
            yield return Surr.Gamma(1);
            yield return Surr.Gamma(4);
            // SVO
            yield return Surr.SmallVeblen();
        }
    }

    /// <summary>{φ(β, 0, 0) : β &lt; α, β drawn from a stratified ordinal sample}. Used as left set of Veblen(α, 0, 0) for transfinite α so Veblen is monotonic across the ordinal hierarchy, not just among ε-indexed values.</summary>
    public sealed class VeblenBelow : IInfiniteSet
    {
        private readonly Surr _alpha;
        public VeblenBelow(Surr alpha) { _alpha = alpha; }
        public string DisplayName => $"φ(β, 0, 0) for β < {_alpha}";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (!(beta < _alpha)) continue;
                if (target <= Surr.Veblen(beta, 0, 0)) return true;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return new Surr(1) < _alpha && Surr.SmallVeblen() <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new System.Collections.Generic.List<Surr>();
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (result.Count >= count) break;
                if (beta < _alpha) result.Add(Surr.Veblen(beta, 0, 0));
            }
            return result.ToArray();
        }
    }

    /// <summary>{ε_β : β &lt; α, β drawn from stratified sample}. Left set of Epsilon(α) for transfinite α.</summary>
    public sealed class EpsilonIndexedBelow : IInfiniteSet
    {
        private readonly Surr _alpha;
        public EpsilonIndexedBelow(Surr alpha) { _alpha = alpha; }
        public string DisplayName => $"ε_β for β < {_alpha}";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // All finite β are < any transfinite α; include ε_0 … ε_16 by doubling.
            if (target <= Surr.EpsilonNaught) return true;
            for (int k = 1; k <= 64; k *= 2)
                if (target <= Surr.Epsilon(k)) return true;
            // Transfinite β's strictly less than α
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (!(beta < _alpha)) continue;
                if (target <= Surr.Epsilon(beta)) return true;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target) => Surr.EpsilonNaught <= target;

        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Epsilon(i);
            return arr;
        }
    }

    /// <summary>{Γ_β : β &lt; α, β drawn from stratified sample}. Left set of Gamma(α) for transfinite α.</summary>
    public sealed class GammaIndexedBelow : IInfiniteSet
    {
        private readonly Surr _alpha;
        public GammaIndexedBelow(Surr alpha) { _alpha = alpha; }
        public string DisplayName => $"Γ_β for β < {_alpha}";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            if (target <= Surr.Gamma0) return true;
            for (int k = 1; k <= 32; k *= 2)
                if (target <= Surr.Gamma(k)) return true;
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (!(beta < _alpha)) continue;
                if (target <= Surr.Gamma(beta)) return true;
            }
            return false;
        }

        public bool HasElementLessOrEqual(Surr target) => Surr.Gamma0 <= target;

        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Gamma(i);
            return arr;
        }
    }

    /// <summary>{ψ(β) : β &lt; α, β drawn from stratified sample including ω_1}. Left set of Psi(α).
    /// Makes Psi monotonic in α — larger α yields a Psi with more previous ψ values in its left set.</summary>
    public sealed class PsiBelow : IInfiniteSet
    {
        private readonly Surr _alpha;
        public PsiBelow(Surr alpha) { _alpha = alpha; }
        public string DisplayName => $"ψ(β) for β < {_alpha}";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (!(beta < _alpha)) continue;
                if (target <= Surr.Psi(beta)) return true;
            }
            // Also include zero as a base case if 0 < _alpha
            if (Surr.Zero < _alpha && target <= Surr.Psi(Surr.Zero)) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return Surr.Zero < _alpha && Surr.Psi(Surr.Zero) <= target;
        }

        public Surr[] SampleElements(int count)
        {
            var result = new System.Collections.Generic.List<Surr>();
            if (Surr.Zero < _alpha) result.Add(Surr.Psi(Surr.Zero));
            foreach (var beta in OrdinalSamples.Stratified())
            {
                if (result.Count >= count) break;
                if (beta < _alpha) result.Add(Surr.Psi(beta));
            }
            return result.ToArray();
        }
    }

    /// <summary>The set {φ(k, 0, 0) : k = 1, 2, 3, …} plus φ(α, 0, 0) for various transfinite α.
    /// Used as left set of the Large Veblen Ordinal = φ(1, 0, 0, 0).</summary>
    public sealed class SmallVeblenSeq : IInfiniteSet
    {
        public static readonly SmallVeblenSeq Instance = new();
        public string DisplayName => "φ(α, 0, 0) for α = 1, 2, …, Γ_0, SVO, …";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            for (int k = 1; k <= 8; k++)
                if (target <= Surr.Veblen(k, 0, 0)) return true;
            if (target <= Surr.Veblen(Surr.Gamma0, 0, 0)) return true;
            if (target <= Surr.Veblen(Surr.SmallVeblen(), 0, 0)) return true;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target) => Surr.SmallVeblen() <= target;

        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Veblen(i + 1, 0, 0);
            return arr;
        }
    }

    /// <summary>The set of ALL countable ordinals — used as left set of ω_1 (the first uncountable ordinal).
    /// Uses the Surr._isCountable trait flag to decide membership: any target marked countable is considered present.</summary>
    public sealed class CountableOrdinals : IInfiniteSet
    {
        public static readonly CountableOrdinals Instance = new();
        public string DisplayName => "(all countable ordinals)";
        public bool HasElementGreaterOrEqual(Surr target) => target._isCountable;
        public bool HasElementLessOrEqual(Surr target) => Surr.Zero <= target;
        public Surr[] SampleElements(int count)
        {
            // Return a sample of representative countable ordinals.
            return new[] { Surr.Zero, Surr.Omega, Surr.EpsilonNaught, Surr.Gamma0 };
        }
    }

    /// <summary>The set {Γ_0, Γ_1, Γ_2, …}. Left set of the Small Veblen Ordinal.</summary>
    public sealed class GammaSeq : IInfiniteSet
    {
        public static readonly GammaSeq Instance = new();
        public string DisplayName => "Γ_0, Γ_1, Γ_2, ...";
        public bool HasElementGreaterOrEqual(Surr target)
        {
            if (target <= Surr.Gamma0) return true;
            for (int k = 1; k <= 64; k *= 2)
                if (target <= Surr.Gamma(k)) return true;
            return false;
        }
        public bool HasElementLessOrEqual(Surr target) => Surr.Gamma0 <= target;
        public Surr[] SampleElements(int count)
        {
            var arr = new Surr[count];
            for (int i = 0; i < count; i++) arr[i] = Surr.Gamma(i);
            return arr;
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

        /// <summary>For sqrt generators: n such that the target value is √n. Null otherwise.</summary>
        public readonly long? SqrtOf;

        /// <summary>For nth-root generators: (k, n) such that the target value is ⁿ√k. Null otherwise.</summary>
        public readonly (long K, int N)? NthRootOf;

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

        /// <summary>Sqrt constructor: target value is √n.</summary>
        public LazyDyadicApprox(System.Func<long, int, bool> isBelow, long floor, long sqrtOf)
            : this(isBelow, floor, $"sqrt:{sqrtOf}")
        { SqrtOf = sqrtOf; }

        /// <summary>Nth-root constructor: target value is ⁿ√k.</summary>
        public LazyDyadicApprox(System.Func<long, int, bool> isBelow, long floor, long k, int n)
            : this(isBelow, floor, $"nthroot:{n}:{k}")
        { NthRootOf = (k, n); }

        /// <summary>Convenience constructor for rational p/q.</summary>
        public LazyDyadicApprox(long p, long q) : this(
            (midNum, exp) => {
                // p·2^exp > midNum·q, with overflow guards. Predicate says "midNum·2^-exp < p/q".
                if (exp >= 62) return p > 0;  // precision exhausted — favor consistent answer
                long lhs = p << exp;
                if (p != 0 && (lhs >> exp) != p) return p > 0; // lhs overflow
                long absMid = System.Math.Abs(midNum);
                long absQ = System.Math.Abs(q);
                if (absMid != 0 && absQ != 0 && absMid > long.MaxValue / absQ) return p > 0; // midNum·q overflow
                return lhs > midNum * q;
            },
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

        /// <summary>Max bit-depth before GenerateNext is a no-op — keeps midNum² safely within long range
        /// (midNum < 2^30 ⇒ midNum² < 2^60). Far more precision than any realistic comparison needs.</summary>
        private const int MaxDepth = 30;
        /// <summary>True once GenerateNext has been capped by MaxDepth — callers can detect stalling.</summary>
        public bool AtMaxDepth => _exp >= MaxDepth;

        public void EnsureDepth(int totalElements)
        {
            while (Lower.Count + Upper.Count < totalElements + 2)
            {
                if (AtMaxDepth) return;
                GenerateNext();
            }
        }

        public void GenerateNext()
        {
            if (AtMaxDepth) return;
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
                // Both generators stalled at max depth without distinguishing — the values
                // agree to ~60 bits. Treat as equal; caller must have pre-checked SameRule.
                if (AtMaxDepth && other.AtMaxDepth) return 0;
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
            int take = System.Math.Min(count, Gen.Lower.Count);
            var arr = new Surr[take];
            for (int i = 0; i < take; i++) arr[i] = Gen.Lower[i];
            return arr;
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
            int take = System.Math.Min(count, Gen.Upper.Count);
            var arr = new Surr[take];
            for (int i = 0; i < take; i++) arr[i] = Gen.Upper[i];
            return arr;
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
