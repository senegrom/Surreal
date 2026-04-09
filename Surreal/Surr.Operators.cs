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

            var l1 = Safe(b.left).Select(y => a + y);
            var l2 = Safe(a.left).Select(x => b + x);
            var r1 = Safe(b.right).Select(y => a + y);
            var r2 = Safe(a.right).Select(x => b + x);
            return new Surr(l1.Concat(l2), r1.Concat(r2));
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
