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
            if (a.IsFinite)
                return new Surr(
                    Safe(a.right).Select(x => -x).ToList(),
                    Safe(a.left).Select(x => -x).ToList(), raw: true).Simplify();

            // For non-finite: construct negation and propagate symbolic terms
            var neg = new Surr(
                a.rightInf, Safe(a.right).Select(x => -x).ToList(),
                a.leftInf, Safe(a.left).Select(x => -x).ToList());
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

        /// <summary>
        /// Add two surreals, but return null instead of entering TransfiniteAdd
        /// if the result would trigger deep recursion. Used for cross-terms.
        /// </summary>
        /// <summary>
        /// Add two surreals, returning null if it would trigger deep TransfiniteAdd recursion.
        /// Safe for use in cross-term computation within TransfiniteAdd.
        /// </summary>
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

        /// <summary>
        /// Decompose both operands into transfinite + finite parts via symbolic terms,
        /// sum the finite parts, and rebuild. E.g., (ω - 25) + 25 → ω + 0 = ω.
        /// </summary>
        /// <summary>
        /// Decompose both operands into transfinite + finite parts via symbolic terms,
        /// sum the finite parts, and rebuild. E.g., (ω - 25) + 25 → ω + 0 = ω.
        /// Only activates when at least one operand HAS symbolic terms (from TransfiniteAdd).
        /// </summary>
        [System.ThreadStatic] private static bool _inSymbolicSum;

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

        private static Surr TransfiniteAdd(Surr a, Surr b)
        {
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

            // Symbolic FOIL: check before IsFinite since TransfiniteAdd results may appear finite
            var foil = TrySymbolicProduct(a, b);
            if (foil is not null) return foil;

            if (a.IsFinite && b.IsFinite)
            {
                var key = (Evaluate(a), Evaluate(b));
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
                MulCache[key] = result;
                return result;
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
            return new Surr(lo, ro);
        }

        public static Surr operator /(Surr a, Surr b)
        {
            if (b.IsZero) throw new System.DivideByZeroException("Division by surreal zero");
            if (a.IsZero) return Zero;

            // Known quotient patterns
            var known = TryKnownQuotient(a, b);
            if (known is not null) return known;

            // Fallback: not yet implemented for general case
            throw new System.NotImplementedException(
                $"General surreal division not yet implemented for {a} / {b}");
        }

        public static Surr operator /(Surr a, long b) => a / new Surr(b);
        public static Surr operator /(long a, Surr b) => new Surr(a) / b;

        private static Surr TryKnownQuotient(Surr a, Surr b)
        {
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

            // √n / √m = √(n/m) if n/m is integer, else (√n/√m) = √n · (1/√m)
            if (aGen != null && bGen != null
                && aGen.Tag.StartsWith("sqrt:") && bGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(aGen.Tag[5..]);
                long m = long.Parse(bGen.Tag[5..]);
                if (n % m == 0) return FromSqrt(n / m);
                // √n / √m = √(n·m) / m  — rationalize denominator
                return FromSqrt(n * m) / new Surr(m);
            }

            // ω / positive integer = OmegaMultiples slot (conceptual, ω/n < ω)
            if (a._displayName == "ω" && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num > 0)
            {
                long n = bVal.Value.Num;
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
            var aGen = GeneratorHelper.GetGenerator(a);
            var bGen = GeneratorHelper.GetGenerator(b);
            var aVal = TryEvaluate(a);
            var bVal = TryEvaluate(b);

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
            if (aVal.HasValue && bGen != null && bGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(bGen.Tag[5..]);
                long k = aVal.Value.Num;
                long kDen = 1L << aVal.Value.Exp;
                // (k/kDen) * √n = √(k²n / kDen²) — only clean for integer k
                if (aVal.Value.Exp == 0 && k > 0)
                    return FromSqrt(k * k * n);
                // For dyadic k: k*√n is not a simple sqrt. Use FromPredicate.
                // mid < k*√n ↔ mid/k < √n ↔ (mid*kDen)² < n * (k * 2^exp)²
                return FromPredicate(
                    (midNum, exp) => midNum * midNum * kDen * kDen < n * k * k * (1L << (2 * exp)),
                    (long)(k * System.Math.Sqrt(n) / kDen),
                    $"{aVal.Value}·√{n}");
            }
            if (bVal.HasValue && aGen != null && aGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(aGen.Tag[5..]);
                long k = bVal.Value.Num;
                long kDen = 1L << bVal.Value.Exp;
                if (bVal.Value.Exp == 0 && k > 0)
                    return FromSqrt(k * k * n);
                return FromPredicate(
                    (midNum, exp) => midNum * midNum * kDen * kDen < n * k * k * (1L << (2 * exp)),
                    (long)(k * System.Math.Sqrt(n) / kDen),
                    $"{bVal.Value}·√{n}");
            }

            // Check if either is √ω (display name based, since it has no generator)
            bool aIsSqrtOmega = a._displayName == "√ω";
            bool bIsSqrtOmega = b._displayName == "√ω";

            // √ω * √ω = ω
            if (aIsSqrtOmega && bIsSqrtOmega) return Omega;

            // √ω * √n = √(n·ω) — represented as tagged transfinite surreal
            if (aIsSqrtOmega && bGen != null && bGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(bGen.Tag[5..]);
                return MakeSqrtNOmega(n);
            }
            if (bIsSqrtOmega && aGen != null && aGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(aGen.Tag[5..]);
                return MakeSqrtNOmega(n);
            }

            // √ω * integer k = k√ω — tagged transfinite
            if (aIsSqrtOmega && bVal.HasValue && bVal.Value.Exp == 0)
                return MakeKSqrtOmega(bVal.Value.Num);
            if (bIsSqrtOmega && aVal.HasValue && aVal.Value.Exp == 0)
                return MakeKSqrtOmega(aVal.Value.Num);

            // Both evaluable dyadics: product is a dyadic
            if (aVal.HasValue && bVal.HasValue)
                return Dyadic(aVal.Value.Num * bVal.Value.Num, aVal.Value.Exp + bVal.Value.Exp);

            // ω * ω = ω²
            bool aIsOmega = a._displayName == "ω";
            bool bIsOmega = b._displayName == "ω";
            if (aIsOmega && bIsOmega) return OmegaSquared;
            if (aIsOmega && b._displayName == "ω²") return OmegaPowers.Instance.Get(3);
            if (bIsOmega && a._displayName == "ω²") return OmegaPowers.Instance.Get(3);
            if (a._displayName == "ω²" && b._displayName == "ω²") return OmegaPowers.Instance.Get(4);

            // ω * integer = n·ω
            if (aIsOmega && bVal.HasValue && bVal.Value.Exp == 0 && bVal.Value.Num > 0)
                return OmegaMultiples.Instance.Get((int)bVal.Value.Num);
            if (bIsOmega && aVal.HasValue && aVal.Value.Exp == 0 && aVal.Value.Num > 0)
                return OmegaMultiples.Instance.Get((int)aVal.Value.Num);

            // generator * generator (both must exist)
            if (aGen is null || bGen is null) return null;

            // sqrt * sqrt: √n * √m = √(nm)
            if (aGen.Tag.StartsWith("sqrt:") && bGen.Tag.StartsWith("sqrt:"))
            {
                long n = long.Parse(aGen.Tag[5..]);
                long m = long.Parse(bGen.Tag[5..]);
                return FromSqrt(n * m);
            }

            // rational * rational: (p1/q1) * (p2/q2)
            if (aGen.P.HasValue && bGen.P.HasValue)
                return FromRational(aGen.P.Value * bGen.P.Value, aGen.Q.Value * bGen.Q.Value);

            return null;
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

            // Cancel pairs that sum to zero: same negate with opposite values,
            // or different negate with same values.
            for (int i = 0; i < products.Count; i++)
            {
                for (int j = i + 1; j < products.Count; j++)
                {
                    bool cancels = false;
                    // Same negate, opposite values: +X and +(-X) → cancels
                    if (products[i].negate == products[j].negate)
                    {
                        var vi = products[i].value;
                        var vj = products[j].value;
                        if (vi._displayName != null && vj._displayName != null
                            && (vi._displayName == $"-({vj._displayName})"
                                || vj._displayName == $"-({vi._displayName})"
                                || (vi._displayName.StartsWith("-") && vj._displayName == vi._displayName[1..])
                                || (vj._displayName.StartsWith("-") && vi._displayName == vj._displayName[1..])))
                            cancels = true;
                        // Also check via TryEvaluate: values are negatives of each other
                        var avi = TryEvaluate(vi); var avj = TryEvaluate(vj);
                        if (avi.HasValue && avj.HasValue && avi.Value.Num == -avj.Value.Num && avi.Value.Exp == avj.Value.Exp)
                            cancels = true;
                    }
                    // Different negate, same values
                    else if (products[i].value._displayName != null
                        && products[i].value._displayName == products[j].value._displayName)
                        cancels = true;

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

        /// <summary>√(n·ω) — transfinite surreal, tagged for algebraic manipulation.</summary>
        private static Surr MakeSqrtNOmega(long n)
        {
            // √(nω) * √(nω) = nω. Greater than all finite reals, less than ω.
            return new Surr(NaturalNumbers.Instance, null, null, new List<Surr> { Omega }, $"√({n}ω)");
        }

        /// <summary>k·√ω — transfinite surreal.</summary>
        private static Surr MakeKSqrtOmega(long k)
        {
            if (k == 0) return Zero;
            if (k == 1) return SqrtOmega;
            return new Surr(NaturalNumbers.Instance, null, null, new List<Surr> { Omega }, $"{k}√ω");
        }

        public static Surr operator *(Surr a, long b) => a * new Surr(b);
        public static Surr operator *(long a, Surr b) => new Surr(a) * b;
        #endregion
    }
}
