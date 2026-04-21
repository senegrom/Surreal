using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Surreal
{
    public sealed partial class Surr
    {
        #region Comparison operators

        private const int CacheCapacity = 2_000_000;

        /// <summary>Memoization cache for <= comparisons, keyed by reference identity.</summary>
        private static readonly Dictionary<(Surr, Surr), bool> LeqCache = new(SurrPairComparer.Instance);

        private sealed class SurrPairComparer : IEqualityComparer<(Surr, Surr)>
        {
            public static readonly SurrPairComparer Instance = new();
            public bool Equals((Surr, Surr) x, (Surr, Surr) y)
                => ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);
            public int GetHashCode((Surr, Surr) obj)
                => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Item1), RuntimeHelpers.GetHashCode(obj.Item2));
        }

        public static bool operator <=(Surr a, Surr b)
        {
            if (ReferenceEquals(a, b)) return true;

            var key = (a, b);
            if (LeqCache.TryGetValue(key, out var cached)) return cached;

            // Conway's recursive definition:
            // a <= b iff !(exists x in a.left: b <= x) && !(exists y in b.right: y <= a)
            bool leftHasGe = (a.leftInf != null && a.leftInf.HasElementGreaterOrEqual(b))
                          || Safe(a.left).Any(x => b <= x);

            bool result;
            if (leftHasGe)
                result = false;
            else
            {
                bool rightHasLe = (b.rightInf != null && b.rightInf.HasElementLessOrEqual(a))
                              || Safe(b.right).Any(x => x <= a);
                result = !rightHasLe;
            }

            if (LeqCache.Count >= CacheCapacity) LeqCache.Clear();
            LeqCache[key] = result;
            return result;
        }

        public static bool operator >=(Surr a, Surr b) => b <= a;
        public static bool operator ==(Surr a, Surr b) => (a <= b) && (b <= a);
        public static bool operator !=(Surr a, Surr b) => !(a == b);
        public static bool operator <(Surr a, Surr b) => (a <= b) && !(b <= a);
        public static bool operator >(Surr a, Surr b) => (b <= a) && !(a <= b);
        #endregion

        #region Long overloads
        public static bool operator <=(Surr a, long b) => a <= new Surr(b);
        public static bool operator >=(Surr a, long b) => a >= new Surr(b);
        public static bool operator <=(long a, Surr b) => new Surr(a) <= b;
        public static bool operator >=(long a, Surr b) => new Surr(a) >= b;
        public static bool operator <(Surr a, long b) => a < new Surr(b);
        public static bool operator >(Surr a, long b) => a > new Surr(b);
        public static bool operator <(long a, Surr b) => new Surr(a) < b;
        public static bool operator >(long a, Surr b) => new Surr(a) > b;
        public static bool operator ==(Surr a, long b) => a == new Surr(b);
        public static bool operator ==(long a, Surr b) => new Surr(a) == b;
        public static bool operator !=(Surr a, long b) => a != new Surr(b);
        public static bool operator !=(long a, Surr b) => new Surr(a) != b;
        public static explicit operator Surr(long n) => new Surr(n);
        #endregion

        #region Arithmetic operators
        public static Surr operator -(Surr a)
        {
            if (a.IsZero) return a;
            // Double negation: -(-x) = x via symbolic tag
            if (a._symbolicTerms is { Count: 1 } negList && negList[0].negate)
                return negList[0].factor;
            if (a.IsFinite)
                return new Surr(
                    Safe(a.right).Select(x => -x).ToList(),
                    Safe(a.left).Select(x => -x).ToList(), raw: true).Simplify();

            // For non-finite: negate structure.
            // Use NegatedSet only for "simple" infinite sets (NaturalNumbers, OmegaMinusNaturals, etc.)
            // For generator-based sets (DyadicApprox), swap left/right without wrapping to avoid
            // infinite recursion in SampleElements.
            // For generator-based surreals (rationals, sqrt): negate via the factory
            var gen = GeneratorHelper.GetGenerator(a);
            if (gen?.P != null)
                return FromRational(-gen.P.Value, gen.Q.Value);
            // Note: -√n does NOT go through FromPredicate — it preserves symbolic terms
            // for FOIL expansion. The DyadicApprox sets are dropped but symbolic tracking still works.

            // Wrap infinite sets in NegatedSet for proper negation.
            // DyadicApprox sets: raw swap (NegatedSet causes recursion via SampleElements).
            IInfiniteSet negLeftInf = null, negRightInf = null;
            if (a.rightInf != null)
                negLeftInf = a.rightInf is NegatedSet nr ? nr.Inner
                    : (a.rightInf is DyadicApproxBelow or DyadicApproxAbove) ? a.rightInf  // raw swap
                    : new NegatedSet(a.rightInf);
            if (a.leftInf != null)
                negRightInf = a.leftInf is NegatedSet nl ? nl.Inner
                    : (a.leftInf is DyadicApproxBelow or DyadicApproxAbove) ? a.leftInf    // raw swap
                    : new NegatedSet(a.leftInf);
            var neg = new Surr(
                negLeftInf, Safe(a.right).Select(x => -x).ToList(),
                negRightInf, Safe(a.left).Select(x => -x).ToList());
            if (a._symbolicTerms != null)
            {
                neg._symbolicTerms = new List<(Surr, bool)>();
                foreach (var (f, n) in a._symbolicTerms)
                    neg._symbolicTerms.Add((f, !n));
            }
            else
            {
                neg._symbolicTerms = new List<(Surr, bool)> { (a, true) };
            }
            if (a._displayName != null)
                neg._displayName = $"-({a._displayName})";
            // Negation preserves countability: -(countable) is countable, -(uncountable) is uncountable.
            neg._isCountable = a._isCountable;
            return neg;
        }

        private static readonly Dictionary<(Dyad, Dyad), Surr> AddCache = new();

        public static Surr operator +(Surr a, Surr b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;

            // Fast path: both finite numeric dyadics
            if (a.IsFinite && b.IsFinite)
            {
                var av = TryEvaluate(a); var bv = TryEvaluate(b);
                if (av is null || bv is null) goto nonFinite; // game or contains transfinite
                var key = (av.Value, bv.Value);
                if (AddCache.TryGetValue(key, out var cached)) return cached;

                var left1 = Safe(b.left).Select(y => a + y);
                var left2 = Safe(a.left).Select(x => b + x);
                var right1 = Safe(b.right).Select(y => a + y);
                var right2 = Safe(a.right).Select(x => b + x);

                var result = new Surr(
                    left1.Concat(left2).ToList(),
                    right1.Concat(right2).ToList(), raw: true).Simplify();
                if (AddCache.Count >= CacheCapacity) AddCache.Clear();
                AddCache[key] = result;
                return result;
            }

            nonFinite:
            // Rational path: if both have known rational/dyadic values, sum the rationals.
            var sumRational = TryRationalSum(a, b);
            if (sumRational is not null) return sumRational;

            // Symbolic path: decompose into transfinite + finite parts, recombine finite parts.
            var symbolic = TrySymbolicSum(a, b);
            if (symbolic is not null) return symbolic;

            // For finite games (no infinite sets on either side): use full Conway formula
            // without SafeAdd limits. Games need all cross-terms for correct comparison.
            if (a.IsFinite && b.IsFinite)
            {
                var left1 = Safe(b.left).Select(y => a + y);
                var left2 = Safe(a.left).Select(x => b + x);
                var right1 = Safe(b.right).Select(y => a + y);
                var right2 = Safe(a.right).Select(x => b + x);
                return new Surr(left1.Concat(left2).ToList(), right1.Concat(right2).ToList(), raw: true);
            }

            // Transfinite path: construct surreal with shifted infinite sets
            return TransfiniteAdd(a, b);
        }

        /// <summary>Add two surreals; return null if it would trigger deep TransfiniteAdd recursion (safe for cross-terms).</summary>
        [System.ThreadStatic] private static int _safeAddDepth;

        private static Surr SafeAdd(Surr a, Surr b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            // Both evaluable dyadics: fast path, no recursion risk
            var av = TryEvaluate(a);
            var bv = TryEvaluate(b);
            if (av.HasValue && bv.HasValue) return a + b;
            // Rational sum: no recursion risk
            var rat = TryRationalSum(a, b);
            if (rat is not null) return rat;
            // Limit recursion depth for anything else (games, transfinite sums)
            if (_safeAddDepth >= 4) return null;
            _safeAddDepth++;
            try { return a + b; }
            finally { _safeAddDepth--; }
        }

        private static Surr TryRationalSum(Surr a, Surr b)
        {
            // Extract (p, q) for each: dyadics as (Num * 2^0, 2^Exp) and generators as (P, Q)
            if (!TryGetRationalPQ(a, out long ap, out long aq)) return null;
            if (!TryGetRationalPQ(b, out long bp, out long bq)) return null;
            // sum = ap/aq + bp/bq = (ap*bq + bp*aq) / (aq*bq)
            return FromRational(ap * bq + bp * aq, aq * bq);
        }

        /// <summary>Decompose via symbolic terms, sum finite parts, rebuild — e.g., (ω-25)+25 → ω.</summary>
        private static Surr TrySymbolicSum(Surr a, Surr b)
        {
            // Only try if at least one operand has symbolic terms from a previous TransfiniteAdd
            if (a._symbolicTerms is null && b._symbolicTerms is null) return null;

            var terms = new List<(Surr factor, bool negate)>();
            AddSymbolicTerms(terms, a, false);
            AddSymbolicTerms(terms, b, false);

            // Separate into evaluable (finite) and non-evaluable (transfinite) terms
            var transfinite = new List<(Surr factor, bool negate)>();
            long finiteNum = 0, finiteDen = 1;

            foreach (var (f, neg) in terms)
            {
                var val = TryEvaluate(f);
                if (val.HasValue)
                {
                    long fDen = 1L << val.Value.Exp;
                    long commonDen = finiteDen * fDen;
                    finiteNum = finiteNum * fDen + (neg ? -1 : 1) * val.Value.Num * finiteDen;
                    finiteDen = commonDen;
                    long g = Gcd(System.Math.Abs(finiteNum), finiteDen);
                    if (g > 0) { finiteNum /= g; finiteDen /= g; }
                }
                else
                {
                    transfinite.Add((f, neg));
                }
            }

            // Must have at least one transfinite and have reduced some finite terms
            if (transfinite.Count == 0 || transfinite.Count == terms.Count) return null;

            // Rebuild without calling operator+ (to avoid recursion).
            // If just one transfinite term and no finite remainder, return it directly.
            if (transfinite.Count == 1 && finiteNum == 0)
            {
                var (f, neg) = transfinite[0];
                return neg ? -f : f;
            }

            // One transfinite term + finite remainder: use TransfiniteAdd directly
            if (transfinite.Count == 1)
            {
                var (f, neg) = transfinite[0];
                var finiteVal = (finiteDen & (finiteDen - 1)) == 0
                    ? Dyadic(finiteNum, BitOperations.TrailingZeroCount((ulong)finiteDen))
                    : FromRational(finiteNum, finiteDen);
                var tf = neg ? -f : f;
                return TransfiniteAdd(tf, finiteVal);
            }

            // Multiple transfinite terms: fall back (can't simplify without operator+)
            return null;
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0) { (a, b) = (b, a % b); }
            return a;
        }

        private static bool TryGetRationalPQ(Surr s, out long p, out long q)
        {
            var val = TryEvaluate(s);
            if (val.HasValue) { p = val.Value.Num; q = 1L << val.Value.Exp; return true; }
            var gen = GeneratorHelper.GetGenerator(s);
            if (gen?.P != null && gen?.Q != null) { p = gen.P.Value; q = gen.Q.Value; return true; }
            p = q = 0; return false;
        }

        private static readonly Dictionary<(Surr, Surr), Surr> _transAddCache = new(SurrPairComparer.Instance);
        private static Surr TransfiniteAdd(Surr a, Surr b)
        {
            var cacheKey = (a, b);
            if (_transAddCache.TryGetValue(cacheKey, out var cachedSum)) return cachedSum;
            var finiteLeft = new List<Surr>();
            var finiteRight = new List<Surr>();
            IInfiniteSet newLeftInf = null;
            IInfiniteSet newRightInf = null;

            // Finite cross-terms. Use SafeAdd to avoid deep recursion: if a+bL would
            // re-enter TransfiniteAdd with a large finite chain, SafeAdd returns null.
            foreach (var bL in Safe(b.left)) { var s = SafeAdd(a, bL); if (s is not null) finiteLeft.Add(s); }
            foreach (var aL in Safe(a.left)) { var s = SafeAdd(aL, b); if (s is not null) finiteLeft.Add(s); }
            foreach (var bR in Safe(b.right)) { var s = SafeAdd(a, bR); if (s is not null) finiteRight.Add(s); }
            foreach (var aR in Safe(a.right)) { var s = SafeAdd(aR, b); if (s is not null) finiteRight.Add(s); }

            // Infinite × value cross-terms: sample concrete elements, add other operand
            if (b.leftInf != null)
                foreach (var el in b.leftInf.SampleElements(3)) finiteLeft.Add(a + el);
            if (a.leftInf != null)
                foreach (var el in a.leftInf.SampleElements(3)) finiteLeft.Add(el + b);
            if (b.rightInf != null)
                foreach (var el in b.rightInf.SampleElements(3)) finiteRight.Add(a + el);
            if (a.rightInf != null)
                foreach (var el in a.rightInf.SampleElements(3)) finiteRight.Add(el + b);

            // When adding transfinite + positive: include the transfinite itself as a left option.
            // This represents the chain: a + b has left option a + 0 = a (reachable since 0
            // is eventually a left option of any positive surreal by taking left options repeatedly).
            var bEval = TryEvaluate(b);
            var aEval = TryEvaluate(a);
            if (!a.IsFinite && bEval.HasValue && bEval.Value.Num > 0 && bEval.Value.Exp == 0)
                finiteLeft.Add(a); // a + 0 = a, and 0 is reachable from positive b
            if (!b.IsFinite && aEval.HasValue && aEval.Value.Num > 0 && aEval.Value.Exp == 0)
                finiteLeft.Add(b);

            // Shifted infinite sets for NaturalNumbers
            if (a.leftInf is NaturalNumbers && TryGetRationalPQ(b, out var bp, out var bq))
                newLeftInf = new ShiftedNaturals(bp, bq);
            else if (b.leftInf is NaturalNumbers && TryGetRationalPQ(a, out var ap, out var aq))
                newLeftInf = new ShiftedNaturals(ap, aq);

            var result = new Surr(newLeftInf, finiteLeft, newRightInf, finiteRight);

            // Tag with symbolic terms for algebraic expansion in multiplication
            result._symbolicTerms = new List<(Surr, bool)>();
            AddSymbolicTerms(result._symbolicTerms, a, false);
            AddSymbolicTerms(result._symbolicTerms, b, false);

            if (a._displayName != null && b._displayName != null)
                result._displayName = $"{a._displayName}+{b._displayName}";

            // Propagate countability: result is countable iff both operands are.
            result._isCountable = a._isCountable && b._isCountable;

            if (_transAddCache.Count < CacheCapacity) _transAddCache[cacheKey] = result;
            return result;
        }

        private static void AddSymbolicTerms(List<(Surr factor, bool negate)> terms, Surr s, bool negate)
        {
            if (s._symbolicTerms != null)
            {
                foreach (var (f, n) in s._symbolicTerms)
                    terms.Add((f, n ^ negate));
            }
            else
            {
                terms.Add((s, negate));
            }
        }

        public static Surr operator -(Surr a, Surr b) => a + (-b);
        public static Surr operator -(Surr a, long b) => a + (-new Surr(b));
        public static Surr operator -(long a, Surr b) => new Surr(a) + (-b);
        public static Surr operator +(long a, Surr b) => new Surr(a) + b;
        public static Surr operator +(Surr a, long b) => a + new Surr(b);

        private static readonly Dictionary<(Dyad, Dyad), Surr> MulCache = new();

        public static Surr operator *(Surr a, Surr b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            // Identity: 1 * x = x
            var av1 = TryEvaluate(a);
            if (av1.HasValue && av1.Value.Num == 1 && av1.Value.Exp == 0) return b;
            var bv1 = TryEvaluate(b);
            if (bv1.HasValue && bv1.Value.Num == 1 && bv1.Value.Exp == 0) return a;

            // Negation unwrap: if one operand is tagged as -factor, compute -(factor * other).
            // Integers' negation doesn't set _symbolicTerms, so this doesn't fire for int*int.
            if (a._symbolicTerms is { Count: 1 } at && at[0].negate)
                return -(at[0].factor * b);
            if (b._symbolicTerms is { Count: 1 } bt && bt[0].negate)
                return -(a * bt[0].factor);

            // Symbolic FOIL: check before IsFinite since TransfiniteAdd results may appear finite
            var foil = TrySymbolicProduct(a, b);
            if (foil is not null) return foil;

            if (a.IsFinite && b.IsFinite)
            {
                // Only cache via Dyad keys when both operands are evaluable.
                var av = TryEvaluate(a); var bv = TryEvaluate(b);
                if (av.HasValue && bv.HasValue)
                {
                    var key = (av.Value, bv.Value);
                    if (MulCache.TryGetValue(key, out var cached)) return cached;

                    var aL = Safe(a.left); var aR = Safe(a.right);
                    var bL = Safe(b.left); var bR = Safe(b.right);

                    var leftOpts =
                        aL.SelectMany(al => bL.Select(bl => al * b + a * bl - al * bl))
                        .Concat(aR.SelectMany(ar => bR.Select(br => ar * b + a * br - ar * br)));
                    var rightOpts =
                        aL.SelectMany(al => bR.Select(br => al * b + a * br - al * br))
                        .Concat(aR.SelectMany(ar => bL.Select(bl => ar * b + a * bl - ar * bl)));

                    var result = new Surr(leftOpts.ToList(), rightOpts.ToList(), raw: true).Simplify();
                    if (MulCache.Count >= CacheCapacity) MulCache.Clear();
                    MulCache[key] = result;
                    return result;
                }
                // Non-evaluable finite (e.g., -ε₀): fall through to TryKnownProduct / fallback.
            }

            // Algebraic product path: use generator tags
            var known = TryKnownProduct(a, b);
            if (known is not null) return known;

            // Fallback for unknown non-finite products
            var aL2 = Safe(a.left); var aR2 = Safe(a.right);
            var bL2 = Safe(b.left); var bR2 = Safe(b.right);
            var lo = aL2.SelectMany(al => bL2.Select(bl => al * b + a * bl - al * bl))
                .Concat(aR2.SelectMany(ar => bR2.Select(br => ar * b + a * br - ar * br)));
            var ro = aL2.SelectMany(al => bR2.Select(br => al * b + a * br - al * br))
                .Concat(aR2.SelectMany(ar => bL2.Select(bl => ar * b + a * bl - ar * bl)));
            var prod = new Surr(lo, ro);
            prod._isCountable = a._isCountable && b._isCountable;
            return prod;
        }

        public static Surr operator /(Surr a, Surr b)
        {
            if (b.IsZero) throw new System.DivideByZeroException("Division by surreal zero");
            if (a.IsZero) return Zero;

            // Known quotient patterns (fast paths for common cases)
            var known = TryKnownQuotient(a, b);
            if (known is not null) return known;

            // Conjugate path: if b is a 2-term sum where both terms have a known-square tag
            // (sqrt generator with SqrtOf or _sqrtOf), multiply numerator and denominator by
            // the conjugate so the denominator becomes a rational number.
            var conjQuot = TryConjugateQuotient(a, b);
            if (conjQuot is not null) return conjQuot;

            // General: a / b = a · (1/b).
            return a * Inverse(b);
        }

        /// <summary>
        /// If b = f₁ ± f₂ where each fᵢ has a known square, rationalize: a/b = a·conj(b) / (b·conj(b)).
        /// Two paths:
        ///   (1) both f_i squares are evaluable integers — compute denom = f1² − f2² and scale.
        ///   (2) b · conj(b) equals a structurally — then a/b = conj(b) directly (covers the
        ///       (A² − B²)/(A − B) = A + B identity even when B² is transfinite).
        /// Returns null if the pattern doesn't match.
        /// </summary>
        private static Surr TryConjugateQuotient(Surr a, Surr b)
        {
            if (b._symbolicTerms is not { Count: 2 } ys) return null;
            var (f1, n1) = ys[0];
            var (f2, n2) = ys[1];
            if (!TryGetSquare(f1, out var f1Sq) || !TryGetSquare(f2, out var f2Sq)) return null;
            // Build conjugate: flip second term's negate flag.
            var t1 = n1 ? -f1 : f1;
            var t2 = !n2 ? -f2 : f2;
            var conj = t1 + t2;

            // Path 1: both squares are evaluable integers — rationalize via the integer denominator.
            var f1SqVal = TryEvaluate(f1Sq);
            var f2SqVal = TryEvaluate(f2Sq);
            if (f1SqVal.HasValue && f2SqVal.HasValue
                && f1SqVal.Value.Exp == 0 && f2SqVal.Value.Exp == 0)
            {
                long denom = f1SqVal.Value.Num - f2SqVal.Value.Num;
                if (denom != 0)
                {
                    var aVal = TryEvaluate(a);
                    if (aVal.HasValue && aVal.Value.Exp == 0 && aVal.Value.Num % denom == 0)
                        return new Surr(aVal.Value.Num / denom) * conj;
                    return (a * conj) / new Surr(denom);
                }
            }

            // Path 2: transfinite squares — check if a == f1² − f2² (i.e., a equals what b · conj would FOIL to).
            // If so, a/b = conj. Covers the (A² − B²)/(A − B) = A + B identity when B² is transfinite.
            try
            {
                var bConjValue = f1Sq + (-f2Sq);
                if (a == bConjValue) return conj;
            }
            catch (System.NotImplementedException) { /* can't even construct b·conj — skip */ }
            return null;
        }

        /// <summary>Extract the known square of x (via generator.SqrtOf or _sqrtOf tag). Returns false if no square is tracked.</summary>
        private static bool TryGetSquare(Surr x, out Surr square)
        {
            var g = GeneratorHelper.GetGenerator(x);
            if (g?.SqrtOf is long n) { square = GetInt(n); return true; }
            if (x._sqrtOf is not null) { square = x._sqrtOf; return true; }
            square = null;
            return false;
        }

        public static Surr operator /(Surr a, long b) => a / new Surr(b);
        public static Surr operator /(long a, Surr b) => new Surr(a) / b;

        /// <summary>
        /// Multiplicative inverse 1/y for non-zero y. Handles evaluable dyadics, rational/sqrt/nth-root
        /// generators, and named transfinite constants (ω, ε₀, Γ₀). Throws for truly unsupported cases
        /// like y = ω+1 (inverses requiring Conway's infinite iteration).
        /// </summary>
        private static readonly Dictionary<Surr, Surr> _inverseCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public static Surr Inverse(Surr y)
        {
            if (y.IsZero) throw new System.DivideByZeroException("Inverse of zero");
            if (_inverseCache.TryGetValue(y, out var cached)) return cached;
            // Unwrap symbolic negation first (avoids Conway comparison on y<0, which can give wrong
            // answers for structurally-swapped negatives like -√3).
            if (y._symbolicTerms is { Count: 1 } neg && neg[0].negate)
            {
                var inv = -Inverse(neg[0].factor);
                _inverseCache[y] = inv;
                return inv;
            }
            if (y < 0)
            {
                var inv = -Inverse(-y);
                _inverseCache[y] = inv;
                return inv;
            }
            // y > 0 from here.

            // Evaluable dyadic y: 1/y = den/num.
            var yVal = TryEvaluate(y);
            if (yVal.HasValue)
            {
                if (yVal.Value.Num == 1 && yVal.Value.Exp == 0) return GetInt(1);
                long num = yVal.Value.Num;
                long den = 1L << yVal.Value.Exp;
                var inv = FromRational(den, num);
                _inverseCache[y] = inv;
                return inv;
            }

            // Generator-based: rational p/q, sqrt n, nth-root (k, n).
            var yGen = GeneratorHelper.GetGenerator(y);
            if (yGen?.P != null && yGen?.Q != null)
            {
                var inv = FromRational(yGen.Q.Value, yGen.P.Value);
                _inverseCache[y] = inv;
                return inv;
            }
            if (yGen?.SqrtOf is long sn)
            {
                // 1/√n via Dedekind cut, tagged _sqrtOf = 1/n so (1/√n)² = 1/n.
                var inv = FromPredicate(
                    (midNum, exp) =>
                    {
                        if (2 * exp >= 63) return true;
                        return midNum * midNum * sn < (1L << (2 * exp));
                    },
                    0,
                    $"1/√{sn}");
                inv._sqrtOf = FromRational(1, sn);
                _inverseCache[y] = inv;
                return inv;
            }
            if (yGen?.NthRootOf is (long k, int n))
            {
                // 1/ⁿ√k via Dedekind cut, tagged _nthRoot equivalent for n² identity.
                var inv = FromPredicate(
                    (midNum, exp) =>
                    {
                        int shift = n * exp;
                        if (shift >= 63) return true;
                        long power = 1;
                        for (int i = 0; i < n; i++)
                        {
                            if (midNum != 0 && System.Math.Abs(power) > long.MaxValue / System.Math.Abs(midNum)) return true;
                            power *= midNum;
                        }
                        return power * k < (1L << shift);
                    },
                    0,
                    $"1/{n}√{k}");
                _inverseCache[y] = inv;
                return inv;
            }

            // Named transfinite constants
            if (ReferenceEquals(y, Omega)) return InverseOmega;
            if (ReferenceEquals(y, EpsilonNaught)) return InverseEpsilon0;
            if (ReferenceEquals(y, Gamma0)) return InverseGamma0;
            if (ReferenceEquals(y, SqrtOmega)) return FromSqrt(1) / SqrtOmega; // unreachable; handled above

            // Sqrt-tagged transfinite (e.g. √Γ_0): 1/√x via Sqrt of Inverse(x).
            if (y._sqrtOf is Surr sqrtOfVal)
                return Sqrt(Inverse(sqrtOfVal));

            throw new System.NotImplementedException(
                $"Inverse not yet implemented for {y} (requires Conway's iterative inversion)");
        }

        private static Surr TryKnownQuotient(Surr a, Surr b)
        {
            // Reflexive: x / x = 1 for any non-zero x
            if (ReferenceEquals(a, b)) return GetInt(1);

            // Normalize sign: -x / y = -(x/y); x / -y = -(x/y). Unwrap one level of symbolic negation.
            if (a._symbolicTerms is { Count: 1 } at && at[0].negate)
            {
                var inner = TryKnownQuotient(at[0].factor, b);
                if (inner is not null) return -inner;
            }
            if (b._symbolicTerms is { Count: 1 } bt && bt[0].negate)
            {
                var inner = TryKnownQuotient(a, bt[0].factor);
                if (inner is not null) return -inner;
            }

            var aVal = TryEvaluate(a);
            var bVal = TryEvaluate(b);
            var aGen = GeneratorHelper.GetGenerator(a);
            var bGen = GeneratorHelper.GetGenerator(b);

            // dyadic / dyadic → rational
            if (aVal.HasValue && bVal.HasValue)
            {
                long num = aVal.Value.Num * (1L << bVal.Value.Exp);
                long den = (1L << aVal.Value.Exp) * bVal.Value.Num;
                if (den < 0) { num = -num; den = -den; }
                return FromRational(num, den);
            }

            // rational / dyadic or dyadic / rational
            if (aGen?.P != null && bVal.HasValue)
            {
                long num = aGen.P.Value * (1L << bVal.Value.Exp);
                long den = aGen.Q.Value * bVal.Value.Num;
                if (den < 0) { num = -num; den = -den; }
                return FromRational(num, den);
            }
            if (aVal.HasValue && bGen?.P != null)
            {
                long num = aVal.Value.Num * bGen.Q.Value;
                long den = (1L << aVal.Value.Exp) * bGen.P.Value;
                if (den < 0) { num = -num; den = -den; }
                return FromRational(num, den);
            }

            // rational / rational
            if (aGen?.P != null && bGen?.P != null)
            {
                long num = aGen.P.Value * bGen.Q.Value;
                long den = aGen.Q.Value * bGen.P.Value;
                if (den < 0) { num = -num; den = -den; }
                return FromRational(num, den);
            }

            // √n / √m = √(n/m) if n/m is integer, else rationalize: √n/√m = √(n·m)/m
            if (aGen?.SqrtOf is long sn && bGen?.SqrtOf is long sm)
            {
                if (sn % sm == 0) return FromSqrt(sn / sm);
                return FromSqrt(sn * sm) / new Surr(sm);
            }

            // √n / integer k = √(n/k²) if k² | n, else √(n·k²)/(k²) via rationalization
            if (aGen?.SqrtOf is long sqN
                && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num != 0)
            {
                long k = bVal.Value.Num;
                long kSq = k * k;
                if (sqN % kSq == 0)
                {
                    var root = FromSqrt(sqN / kSq);
                    return k < 0 ? -root : root;
                }
            }

            // integer k / √n = √(k²/n) if n | k², else k·√n/n via tagged FromPredicate.
            if (bGen?.SqrtOf is long sqM
                && aVal.HasValue && aVal.Value.Exp == 0 && aVal.Value.Num != 0)
            {
                long k = aVal.Value.Num;
                if ((k * k) % sqM == 0)
                {
                    var root = FromSqrt(k * k / sqM);
                    return k < 0 ? -root : root;
                }
                // k/√n: build a tagged surreal so that (k/√n)² = k²/n and (k/√n)·√n = k.
                long absK = System.Math.Abs(k);
                var res = FromPredicate(
                    (midNum, exp) =>
                    {
                        if (2 * exp >= 63) return true;
                        return midNum * midNum * sqM < absK * absK * (1L << (2 * exp));
                    },
                    0,
                    $"{k}/√{sqM}");
                res._sqrtOf = FromRational(absK * absK, sqM);
                return k < 0 ? -res : res;
            }

            // √(m·ω) / k = √((m/k²)·ω) when k² | m — e.g., √(4ω)/2 = √ω
            if (a._sqrtOf is not null && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num != 0)
            {
                for (int m = 2; m <= 10; m++)
                {
                    if (!ReferenceEquals(a._sqrtOf, OmegaMultiples.Instance.Get(m))) continue;
                    long k = bVal.Value.Num;
                    long absK = System.Math.Abs(k);
                    long kSq = absK * absK;
                    if (m % kSq == 0)
                    {
                        int newM = m / (int)kSq;
                        var root = newM == 1 ? SqrtOmega : MakeSqrtNOmega(newM);
                        return k < 0 ? -root : root;
                    }
                }
            }

            // ω / integer (signed): ω/(-n) = -(ω/n)
            if (ReferenceEquals(a, Omega) && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num != 0)
            {
                long n = bVal.Value.Num;
                if (n < 0) return -(a / new Surr(-n));
                if (n == 1) return a;
                if (n == 2) return OmegaHalf;
                // General ω/n: {naturals | ω/(n-1), ω/(n-1)-1, ...}
                return new Surr(
                    NaturalNumbers.Instance, null,
                    OmegaMinusNaturals.Instance, new System.Collections.Generic.List<Surr> { a / new Surr(n - 1) },
                    $"ω/{n}");
            }

            return null;
        }

        private static Surr TryKnownProduct(Surr a, Surr b)
        {
            // Sqrt(x) * Sqrt(x) = x via _sqrtOf tag
            if (a._sqrtOf is not null && b._sqrtOf is not null && ReferenceEquals(a._sqrtOf, b._sqrtOf))
                return a._sqrtOf;

            var aGen = GeneratorHelper.GetGenerator(a);
            var bGen = GeneratorHelper.GetGenerator(b);
            var aVal = TryEvaluate(a);
            var bVal = TryEvaluate(b);

            // (1/√n) · √n = 1: a tagged with _sqrtOf = 1/n meets b's sqrt generator.
            if (a._sqrtOf is not null && bGen?.SqrtOf is long bSqrtN && IsReciprocalOf(a._sqrtOf, bSqrtN))
                return GetInt(1);
            if (b._sqrtOf is not null && aGen?.SqrtOf is long aSqrtN && IsReciprocalOf(b._sqrtOf, aSqrtN))
                return GetInt(1);

            // dyadic * rational generator
            if (aVal.HasValue && bGen?.P != null && bGen?.Q != null)
            {
                long num = aVal.Value.Num * bGen.P.Value;
                long den = (1L << aVal.Value.Exp) * bGen.Q.Value;
                return FromRational(num, den);
            }
            if (bVal.HasValue && aGen?.P != null && aGen?.Q != null)
            {
                long num = bVal.Value.Num * aGen.P.Value;
                long den = (1L << bVal.Value.Exp) * aGen.Q.Value;
                return FromRational(num, den);
            }

            // dyadic * sqrt: k * √n = √(k²n)
            if (aVal.HasValue && bGen?.SqrtOf is long bSn)
            {
                long k = aVal.Value.Num;
                long kDen = 1L << aVal.Value.Exp;
                if (aVal.Value.Exp == 0 && k > 0)
                    return FromSqrt(k * k * bSn);
                return FromPredicate(
                    (midNum, exp) => midNum * midNum * kDen * kDen < bSn * k * k * (1L << (2 * exp)),
                    (long)(k * System.Math.Sqrt(bSn) / kDen),
                    $"{aVal.Value}·√{bSn}");
            }
            if (bVal.HasValue && aGen?.SqrtOf is long aSn)
            {
                long k = bVal.Value.Num;
                long kDen = 1L << bVal.Value.Exp;
                if (bVal.Value.Exp == 0 && k > 0)
                    return FromSqrt(k * k * aSn);
                return FromPredicate(
                    (midNum, exp) => midNum * midNum * kDen * kDen < aSn * k * k * (1L << (2 * exp)),
                    (long)(k * System.Math.Sqrt(aSn) / kDen),
                    $"{bVal.Value}·√{aSn}");
            }

            // Check if either is √ω by reference identity (robust across construction paths)
            bool aIsSqrtOmega = ReferenceEquals(a, SqrtOmega);
            bool bIsSqrtOmega = ReferenceEquals(b, SqrtOmega);

            // √ω * √ω = ω
            if (aIsSqrtOmega && bIsSqrtOmega) return Omega;

            // √ω * √n = √(n·ω) — represented as tagged transfinite surreal
            if (aIsSqrtOmega && bGen?.SqrtOf is long bSqW)
                return MakeSqrtNOmega(bSqW);
            if (bIsSqrtOmega && aGen?.SqrtOf is long aSqW)
                return MakeSqrtNOmega(aSqW);

            // √ω * integer k = k√ω — tagged transfinite
            if (aIsSqrtOmega && bVal.HasValue && bVal.Value.Exp == 0)
                return MakeKSqrtOmega(bVal.Value.Num);
            if (bIsSqrtOmega && aVal.HasValue && aVal.Value.Exp == 0)
                return MakeKSqrtOmega(aVal.Value.Num);

            // Both evaluable dyadics: product is a dyadic
            if (aVal.HasValue && bVal.HasValue)
                return Dyadic(aVal.Value.Num * bVal.Value.Num, aVal.Value.Exp + bVal.Value.Exp);

            // ω * ω = ω²
            bool aIsOmega = ReferenceEquals(a, Omega);
            bool bIsOmega = ReferenceEquals(b, Omega);
            bool aIsOmegaSq = ReferenceEquals(a, OmegaSquared);
            bool bIsOmegaSq = ReferenceEquals(b, OmegaSquared);
            if (aIsOmega && bIsOmega) return OmegaSquared;
            if (aIsOmega && bIsOmegaSq) return OmegaPowers.Instance.Get(3);
            if (bIsOmega && aIsOmegaSq) return OmegaPowers.Instance.Get(3);
            if (aIsOmegaSq && bIsOmegaSq) return OmegaPowers.Instance.Get(4);

            // ω * integer = n·ω
            if (aIsOmega && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num > 0)
                return OmegaMultiples.Instance.Get((int)bVal.Value.Num);
            if (bIsOmega && aVal.HasValue && aVal.Value.Exp == 0 && aVal.Value.Num > 0)
                return OmegaMultiples.Instance.Get((int)aVal.Value.Num);

            // generator * generator (both must exist)
            if (aGen is null || bGen is null) return null;

            // sqrt * sqrt: √n * √m = √(nm)
            if (aGen.SqrtOf is long n0 && bGen.SqrtOf is long m0)
                return FromSqrt(n0 * m0);

            // ⁿ√k · ⁿ√m = ⁿ√(k·m), when both generators are nth-roots with matching n
            if (aGen.NthRootOf is (long k1, int n1) && bGen.NthRootOf is (long k2, int n2) && n1 == n2)
                return NthRoot(k1 * k2, n1);

            // rational * rational: (p1/q1) * (p2/q2)
            if (aGen.P.HasValue && bGen.P.HasValue)
                return FromRational(aGen.P.Value * bGen.P.Value, aGen.Q.Value * bGen.Q.Value);

            return null;
        }

        /// <summary>Does x structurally represent 1/n for the given integer n?</summary>
        private static bool IsReciprocalOf(Surr x, long n)
        {
            var v = TryEvaluate(x);
            if (v.HasValue) return v.Value.Num == 1 && (1L << v.Value.Exp) == n;
            var g = GeneratorHelper.GetGenerator(x);
            return g?.P == 1 && g?.Q == n;
        }

        /// <summary>Conservative equality: ReferenceEquals or matching dyadic values. No expensive operator==.</summary>
        private static bool AreEqual(Surr a, Surr b)
        {
            if (ReferenceEquals(a, b)) return true;
            var av = TryEvaluate(a); var bv = TryEvaluate(b);
            return av.HasValue && bv.HasValue && av.Value.Num == bv.Value.Num && av.Value.Exp == bv.Value.Exp;
        }

        /// <summary>Conservative negation check: dyadic opposites, or one's _symbolicTerms = [(other, true)].</summary>
        private static bool AreNegatives(Surr a, Surr b)
        {
            var av = TryEvaluate(a); var bv = TryEvaluate(b);
            if (av.HasValue && bv.HasValue)
                return av.Value.Num == -bv.Value.Num && av.Value.Exp == bv.Value.Exp;
            // a = -b: operator- on b (no _symbolicTerms) produces a with [(b, true)].
            if (a._symbolicTerms is { Count: 1 } at
                && at[0].negate && ReferenceEquals(at[0].factor, b))
                return true;
            if (b._symbolicTerms is { Count: 1 } bt
                && bt[0].negate && ReferenceEquals(bt[0].factor, a))
                return true;
            // Both have symbolic terms, pairwise inverted
            if (a._symbolicTerms is not null && b._symbolicTerms is not null
                && a._symbolicTerms.Count == b._symbolicTerms.Count)
            {
                for (int k = 0; k < a._symbolicTerms.Count; k++)
                {
                    var (af, an) = a._symbolicTerms[k];
                    var (bf, bn) = b._symbolicTerms[k];
                    if (!ReferenceEquals(af, bf) || an == bn) return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// FOIL expansion: if both operands have symbolic terms, multiply each pair
        /// and sum the results. Cancels matching +/- pairs before summing.
        /// </summary>
        private static Surr TrySymbolicProduct(Surr a, Surr b)
        {
            var aTerms = a._symbolicTerms;
            var bTerms = b._symbolicTerms;
            if (aTerms is null || bTerms is null) return null;

            // Compute all cross-products
            var products = new List<(Surr value, bool negate)>();
            foreach (var (af, an) in aTerms)
            {
                foreach (var (bf, bn) in bTerms)
                {
                    var product = TryKnownProduct(af, bf);
                    if (product is null) return null;
                    products.Add((product, an ^ bn));
                }
            }

            // Cancel pairs that sum to zero: (sign_i · v_i) + (sign_j · v_j) == 0.
            //   Same sign: v_i and v_j are negations of each other.
            //   Opposite sign: v_i and v_j are equal.
            for (int i = 0; i < products.Count; i++)
            {
                for (int j = i + 1; j < products.Count; j++)
                {
                    bool cancels = products[i].negate == products[j].negate
                        ? AreNegatives(products[i].value, products[j].value)
                        : AreEqual(products[i].value, products[j].value);

                    if (cancels)
                    {
                        products.RemoveAt(j);
                        products.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            // Sum remaining products: separate transfinite and finite, combine directly
            Surr transfiniteSum = Zero;
            long finiteNum = 0, finiteDen = 1;

            foreach (var (value, negate) in products)
            {
                var val = TryEvaluate(value);
                if (val.HasValue)
                {
                    long fDen = 1L << val.Value.Exp;
                    long sign = negate ? -1 : 1;
                    finiteNum = finiteNum * fDen + sign * val.Value.Num * finiteDen;
                    finiteDen *= fDen;
                    long g = Gcd(System.Math.Abs(finiteNum), finiteDen);
                    if (g > 0) { finiteNum /= g; finiteDen /= g; }
                }
                else
                {
                    transfiniteSum = negate ? transfiniteSum - value : transfiniteSum + value;
                }
            }

            // Combine: transfinite + finite
            if (finiteNum == 0) return transfiniteSum;
            if (transfiniteSum.IsZero)
                return (finiteDen & (finiteDen - 1)) == 0
                    ? Dyadic(finiteNum, BitOperations.TrailingZeroCount((ulong)finiteDen))
                    : FromRational(finiteNum, finiteDen);

            var finiteVal = (finiteDen & (finiteDen - 1)) == 0
                ? Dyadic(finiteNum, BitOperations.TrailingZeroCount((ulong)finiteDen))
                : FromRational(finiteNum, finiteDen);
            return transfiniteSum + finiteVal;
        }

        private static readonly Dictionary<long, Surr> _sqrtNOmegaCache = new();
        private static readonly Dictionary<long, Surr> _kSqrtOmegaCache = new();

        /// <summary>√(n·ω) — cached so ReferenceEquals works for cancellation. Tagged with _sqrtOf.</summary>
        private static Surr MakeSqrtNOmega(long n)
        {
            if (_sqrtNOmegaCache.TryGetValue(n, out var cached)) return cached;
            var s = new Surr(NaturalNumbers.Instance, null, null, new List<Surr> { Omega }, $"√({n}ω)");
            if (n >= 2 && n <= 10) s._sqrtOf = OmegaMultiples.Instance.Get((int)n);
            _sqrtNOmegaCache[n] = s;
            return s;
        }

        /// <summary>k·√ω — cached so ReferenceEquals works for cancellation. Tagged with _sqrtOf = k²·ω so (k√ω)² = k²ω.</summary>
        private static Surr MakeKSqrtOmega(long k)
        {
            if (k == 0) return Zero;
            if (k == 1) return SqrtOmega;
            if (k == -1) return -SqrtOmega;
            if (_kSqrtOmegaCache.TryGetValue(k, out var cached)) return cached;
            Surr s;
            if (k > 0)
            {
                s = new Surr(NaturalNumbers.Instance, null, null, new List<Surr> { Omega }, $"{k}√ω");
                // (k√ω)² = k²·ω. For small k the square falls in OmegaMultiples.
                long kSq = k * k;
                if (kSq >= 2 && kSq <= 10) s._sqrtOf = OmegaMultiples.Instance.Get((int)kSq);
                else if (kSq == 1) s._sqrtOf = Omega;
            }
            else
            {
                s = -MakeKSqrtOmega(-k);
            }
            _kSqrtOmegaCache[k] = s;
            return s;
        }

        public static Surr operator *(Surr a, long b) => a * new Surr(b);
        public static Surr operator *(long a, Surr b) => new Surr(a) * b;

        /// <summary>
        /// Surreal exponentiation. Handles known algebraic cases:
        /// integer^integer, ω^n (omega powers), ω^ε₀ = ε₀.
        /// General case not yet implemented.
        /// </summary>
        public static Surr Pow(Surr baseVal, Surr exponent)
        {
            // x^0 = 1, 0^x = 0, 1^x = 1
            if (exponent.IsZero) return new Surr(1);
            if (baseVal.IsZero) return Zero;
            var bvCheck = TryEvaluate(baseVal);
            if (bvCheck.HasValue && bvCheck.Value.Num == 1 && bvCheck.Value.Exp == 0) return new Surr(1);

            var bv = TryEvaluate(baseVal);
            var ev = TryEvaluate(exponent);

            // Integer ^ integer
            if (bv.HasValue && ev.HasValue && bv.Value.Exp == 0 && ev.Value.Exp == 0 && ev.Value.Num >= 0)
            {
                long result = 1;
                for (long i = 0; i < ev.Value.Num; i++) result *= bv.Value.Num;
                return new Surr(result);
            }

            bool baseIsOmega = ReferenceEquals(baseVal, Omega);

            // ω ^ n = OmegaPowers
            if (baseIsOmega && ev.HasValue && ev.Value.Exp == 0 && ev.Value.Num > 0)
                return OmegaPowers.Instance.Get((int)ev.Value.Num);

            // ω ^ ω = ω^ω
            if (baseIsOmega && ReferenceEquals(exponent, Omega))
                return OmegaToOmega;

            // ω ^ ε_n = ε_n (defining property of epsilon numbers).
            // Epsilon numbers are produced by Epsilon(n); display name starts with "ε".
            if (baseIsOmega && exponent._displayName != null
                && exponent._displayName.StartsWith("ε"))
                return exponent;

            // ω ^ ζ₀ = ζ₀, ω ^ Γ₀ = Γ₀ (fixed points of higher hierarchies)
            if (baseIsOmega && (ReferenceEquals(exponent, Zeta0) || ReferenceEquals(exponent, Gamma0)))
                return exponent;

            // n ^ ω for finite n ≥ 2: sup{n^k : k ∈ ℕ} = ω
            if (bv.HasValue && bv.Value.Exp == 0 && bv.Value.Num >= 2 && ReferenceEquals(exponent, Omega))
                return Omega;

            // (ω^ω)^ω = ω^(ω²) via ordinal: (α^β)^γ = α^(β·γ) and ω·ω = ω²
            if (ReferenceEquals(baseVal, OmegaToOmega) && ReferenceEquals(exponent, Omega))
                return Pow(Omega, OmegaSquared);

            // ω ^ ω²
            if (baseIsOmega && ReferenceEquals(exponent, OmegaSquared))
                return OmegaPowers.Instance.Get(3);

            throw new System.NotImplementedException(
                $"General exponentiation not yet implemented for {baseVal} ^ {exponent}");
        }
        #endregion
    }
}
