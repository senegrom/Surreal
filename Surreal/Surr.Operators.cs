using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public sealed partial class Surr
    {
        #region Comparison operators
        public static bool operator <=(Surr a, Surr b)
        {
            var aVal = TryEvaluate(a);
            var bVal = TryEvaluate(b);
            if (aVal.HasValue && bVal.HasValue)
                return aVal.Value.CompareTo(bVal.Value) <= 0;

            bool leftHasGe = (a.leftInf != null && a.leftInf.HasElementGreaterOrEqual(b))
                          || Safe(a.left).Any(x => b <= x);
            if (leftHasGe) return false;

            bool rightHasLe = (b.rightInf != null && b.rightInf.HasElementLessOrEqual(a))
                           || Safe(b.right).Any(x => x <= a);
            return !rightHasLe;
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
            return new Surr(
                Safe(a.right).Select(x => -x).ToList(),
                Safe(a.left).Select(x => -x).ToList(), raw: true).Simplify();
        }

        private static readonly Dictionary<(Dyad, Dyad), Surr> AddCache = new();

        public static Surr operator +(Surr a, Surr b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;

            // Fast path: both finite dyadics
            if (a.IsFinite && b.IsFinite)
            {
                var key = (Evaluate(a), Evaluate(b));
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

            // Rational path: if both have known rational/dyadic values, sum the rationals.
            // This constructs the result via FromRational (lazy generators, surreal comparison).
            // The integer arithmetic here is construction-time only (same as FromRational's generator).
            var sumRational = TryRationalSum(a, b);
            if (sumRational is not null) return sumRational;

            // Transfinite path: construct surreal with shifted infinite sets
            return TransfiniteAdd(a, b);
        }

        private static Surr TryRationalSum(Surr a, Surr b)
        {
            // Extract (p, q) for each: dyadics as (Num * 2^0, 2^Exp) and generators as (P, Q)
            if (!TryGetRationalPQ(a, out long ap, out long aq)) return null;
            if (!TryGetRationalPQ(b, out long bp, out long bq)) return null;
            // sum = ap/aq + bp/bq = (ap*bq + bp*aq) / (aq*bq)
            return FromRational(ap * bq + bp * aq, aq * bq);
        }

        private static bool TryGetRationalPQ(Surr s, out long p, out long q)
        {
            var val = TryEvaluate(s);
            if (val.HasValue) { p = val.Value.Num; q = 1L << val.Value.Exp; return true; }
            var gen = GeneratorHelper.GetGenerator(s);
            if (gen != null) { p = gen.P; q = gen.Q; return true; }
            p = q = 0; return false;
        }

        private static Surr TransfiniteAdd(Surr a, Surr b)
        {
            // Build left/right from finite parts + shifted infinite sets
            var finiteLeft = Safe(b.left).Select(y => a + y)
                .Concat(Safe(a.left).Select(x => b + x)).ToList();
            var finiteRight = Safe(b.right).Select(y => a + y)
                .Concat(Safe(a.right).Select(x => b + x)).ToList();

            // Shifted infinite sets: if a has leftInf, each element x gets b added → new infinite set
            IInfiniteSet newLeftInf = null;
            IInfiniteSet newRightInf = null;

            if (a.leftInf is NaturalNumbers && TryGetRationalPQ(b, out long bp, out long bq))
                newLeftInf = new ShiftedNaturals(bp, bq);
            else if (b.leftInf is NaturalNumbers && TryGetRationalPQ(a, out long ap2, out long aq2))
                newLeftInf = new ShiftedNaturals(ap2, aq2);

            // For right: if a.rightInf or b.rightInf exists, shift similarly
            // (ω has no rightInf, so this mainly helps with other transfinite constructions)

            return new Surr(newLeftInf, finiteLeft, newRightInf, finiteRight);
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

            var aL2 = Safe(a.left); var aR2 = Safe(a.right);
            var bL2 = Safe(b.left); var bR2 = Safe(b.right);
            var lo = aL2.SelectMany(al => bL2.Select(bl => al * b + a * bl - al * bl))
                .Concat(aR2.SelectMany(ar => bR2.Select(br => ar * b + a * br - ar * br)));
            var ro = aL2.SelectMany(al => bR2.Select(br => al * b + a * br - al * br))
                .Concat(aR2.SelectMany(ar => bL2.Select(bl => ar * b + a * bl - ar * bl)));
            return new Surr(lo, ro);
        }

        public static Surr operator *(Surr a, long b) => a * new Surr(b);
        public static Surr operator *(long a, Surr b) => new Surr(a) * b;
        #endregion
    }
}
