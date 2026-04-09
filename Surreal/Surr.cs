using System;
using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public sealed class Surr
    {
        #region Dyadic rational arithmetic (for simplification)
        internal readonly struct Dyad : IComparable<Dyad>, IEquatable<Dyad>
        {
            public readonly long Num;
            public readonly int Exp; // value = Num / 2^Exp

            public Dyad(long num, int exp)
            {
                while (exp > 0 && num != 0 && num % 2 == 0) { num /= 2; exp--; }
                Num = num; Exp = exp;
            }

            public bool Equals(Dyad other) => Num == other.Num && Exp == other.Exp;
            public override bool Equals(object obj) => obj is Dyad d && Equals(d);
            public override int GetHashCode() => HashCode.Combine(Num, Exp);

            public int CompareTo(Dyad other)
            {
                int mx = Math.Max(Exp, other.Exp);
                long a = Num << (mx - Exp), b = other.Num << (mx - other.Exp);
                return a.CompareTo(b);
            }

            public static bool operator <(Dyad a, Dyad b) => a.CompareTo(b) < 0;
            public static bool operator >(Dyad a, Dyad b) => a.CompareTo(b) > 0;

            // Smallest integer strictly greater than this value
            public long CeilExclusive()
            {
                if (Exp == 0) return Num + 1;
                long den = 1L << Exp;
                return Num >= 0 ? (Num / den) + 1 : -((-Num - 1) / den);
            }

            // Largest integer strictly less than this value
            public long FloorExclusive()
            {
                if (Exp == 0) return Num - 1;
                long den = 1L << Exp;
                return Num >= 0 ? Num / den : -((-Num + (den - 1)) / den);
            }

            // Floor of this value
            public long Floor()
            {
                if (Exp == 0) return Num;
                long den = 1L << Exp;
                return Num >= 0 ? Num / den : -((-Num + (den - 1)) / den);
            }

            public static Dyad operator +(Dyad a, Dyad b)
            {
                int mx = Math.Max(a.Exp, b.Exp);
                long aN = a.Num << (mx - a.Exp);
                long bN = b.Num << (mx - b.Exp);
                return new Dyad(aN + bN, mx);
            }

            public static Dyad operator -(Dyad a) => new Dyad(-a.Num, a.Exp);

            public static Dyad operator *(Dyad a, Dyad b)
                => new Dyad(a.Num * b.Num, a.Exp + b.Exp);
        }

        private static Dyad SimplestBetween(Dyad? lo, Dyad? hi)
        {
            var zero = new Dyad(0, 0);
            if (lo == null && hi == null) return zero;
            if (lo == null) return hi.Value > zero ? zero : new Dyad(hi.Value.FloorExclusive(), 0);
            if (hi == null) return lo.Value < zero ? zero : new Dyad(lo.Value.CeilExclusive(), 0);
            return SimplestBounded(lo.Value, hi.Value);
        }

        private static Dyad SimplestBounded(Dyad lo, Dyad hi)
        {
            var zero = new Dyad(0, 0);
            if (lo < zero && hi > zero) return zero;

            long loInt = lo.CeilExclusive();
            long hiInt = hi.FloorExclusive();
            if (loInt <= hiInt)
            {
                if (loInt <= 0 && hiInt >= 0) return zero;
                return loInt > 0 ? new Dyad(loInt, 0) : new Dyad(hiInt, 0);
            }

            // No integer in (lo, hi) — recurse via halving
            long n = lo.Floor();
            int mx = Math.Max(lo.Exp, hi.Exp);
            long loN = lo.Num << (mx - lo.Exp);
            long hiN = hi.Num << (mx - hi.Exp);
            long nScaled = n << mx;
            var newLo = new Dyad(2 * (loN - nScaled), mx);
            var newHi = new Dyad(2 * (hiN - nScaled), mx);

            var mid = SimplestBounded(newLo, newHi);
            return new Dyad(n * (1L << (mid.Exp + 1)) + mid.Num, mid.Exp + 1);
        }

        private Dyad? _cachedValue;
        private bool _evalAttempted;

        /// <summary>Try to evaluate as a dyadic rational. Returns null for transfinite surreals.</summary>
        internal static Dyad? TryEvaluate(Surr s)
        {
            if (s._cachedValue.HasValue) return s._cachedValue;
            if (s._evalAttempted) return null; // already tried, was transfinite
            s._evalAttempted = true;

            if (s.leftInf != null || s.rightInf != null) return null;
            if (s.IsZero) { s._cachedValue = new Dyad(0, 0); return s._cachedValue; }

            var leftVals = new List<Dyad>();
            foreach (var x in Safe(s.left))
            {
                var v = TryEvaluate(x);
                if (v == null) return null;
                leftVals.Add(v.Value);
            }
            var rightVals = new List<Dyad>();
            foreach (var x in Safe(s.right))
            {
                var v = TryEvaluate(x);
                if (v == null) return null;
                rightVals.Add(v.Value);
            }

            Dyad? lo = leftVals.Count > 0 ? leftVals.Max() : null;
            Dyad? hi = rightVals.Count > 0 ? rightVals.Min() : null;

            s._cachedValue = SimplestBetween(lo, hi);
            return s._cachedValue;
        }

        private static Dyad Evaluate(Surr s)
            => TryEvaluate(s) ?? throw new InvalidOperationException("Cannot evaluate transfinite surreal as dyadic rational");

        public Surr Simplify()
        {
            var d = TryEvaluate(this);
            return d.HasValue ? Dyadic(d.Value.Num, d.Value.Exp) : this;
        }
        #endregion

        private readonly IReadOnlyCollection<Surr> left, right;
        private readonly IInfiniteSet leftInf, rightInf;
        private readonly string _displayName;

        private Surr(IEnumerable<Surr> left, IEnumerable<Surr> right) :
            this(left.ToList(), right.ToList())
        { }

        // Raw constructor: skips dedup/ordering. Use only when result will be Simplified.
        private Surr(List<Surr> left, List<Surr> right, bool raw)
        {
            this.left = left;
            this.right = right;
        }

        /// <summary>Construct a surreal with infinite and/or finite left/right sets.</summary>
        public Surr(IInfiniteSet leftInf, IReadOnlyCollection<Surr> left,
                    IInfiniteSet rightInf, IReadOnlyCollection<Surr> right,
                    string displayName = null)
        {
            this.leftInf = leftInf;
            this.rightInf = rightInf;
            this.left = left ?? new List<Surr>();
            this.right = right ?? new List<Surr>();
            _displayName = displayName;
        }

        public Surr(IReadOnlyCollection<Surr> left, IReadOnlyCollection<Surr> right)
        {
            var tempLeft = new List<Surr>();
            foreach (var j in left)
            {
                if (!tempLeft.Any(x => x == j) && tempLeft.All(i => j >= i))
                    tempLeft.Add(j);
            }
            this.left = tempLeft;

            var tempRight = new List<Surr>();
            foreach (var j in right)
            {
                if (!tempRight.Any(x => x == j) && tempRight.All(i => j <= i))
                    tempRight.Add(j);
            }
            this.right = tempRight;
        }

        private static readonly Dictionary<long, Surr> IntCache = new();

        public Surr(long n)
        {
            var s = GetInt(n);
            left = s.left; right = s.right;
            _cachedValue = s._cachedValue; _evalAttempted = true;
        }

        internal static Surr GetInt(long n)
        {
            if (IntCache.TryGetValue(n, out var cached)) return cached;

            // Build iteratively from 0 toward n
            if (!IntCache.ContainsKey(0))
            {
                var zero = new Surr(new List<Surr>(), new List<Surr>(), raw: true);
                zero._cachedValue = new Dyad(0, 0); zero._evalAttempted = true;
                IntCache[0] = zero;
            }

            if (n > 0)
            {
                for (long i = 1; i <= n; i++)
                {
                    if (IntCache.ContainsKey(i)) continue;
                    var s = new Surr(new List<Surr> { IntCache[i - 1] }, new List<Surr>(), raw: true);
                    s._cachedValue = new Dyad(i, 0); s._evalAttempted = true;
                    IntCache[i] = s;
                }
            }
            else
            {
                for (long i = -1; i >= n; i--)
                {
                    if (IntCache.ContainsKey(i)) continue;
                    var s = new Surr(new List<Surr>(), new List<Surr> { IntCache[i + 1] }, raw: true);
                    s._cachedValue = new Dyad(i, 0); s._evalAttempted = true;
                    IntCache[i] = s;
                }
            }

            return IntCache[n];
        }

        public bool IsNumeric => !Safe(left).Any(x => Safe(right).Any(y => y <= x));

        private bool IsZero => left is { Count: 0 } && right is { Count: 0 };

        private bool IsFinite => leftInf == null && rightInf == null;

        /// <summary>Creates the surreal number n / 2^k (a dyadic fraction).</summary>
        public static Surr Dyadic(long n, int k)
        {
            if (k == 0)
                return new Surr(n);
            if (k < 0)
                throw new System.ArgumentException("k must be non-negative");
            // n/2^k = [(n-1)/2^k | (n+1)/2^k] when n is odd
            // n/2^k = (n/2) / 2^(k-1) when n is even
            if (n % 2 == 0)
                return Dyadic(n / 2, k - 1);
            return new Surr(new[] { Dyadic(n - 1, k) }, new[] { Dyadic(n + 1, k) });
        }

        public static readonly Surr Half = Dyadic(1, 1);
        public static readonly Surr Zero = new Surr(0L);

        /// <summary>ω = {0, 1, 2, 3, ... | }</summary>
        public static readonly Surr Omega = new(
            NaturalNumbers.Instance, null,
            null, null,
            "ω");

        /// <summary>1/ω = {0 | 1, 1/2, 1/4, 1/8, ...} — a positive infinitesimal</summary>
        public static readonly Surr InverseOmega = new(
            null, new List<Surr> { Zero },
            PositivePowersOfHalf.Instance, null,
            "1/ω");

        /// <summary>
        /// Create a surreal number from rational p/q.
        /// If p/q is a dyadic rational, returns the standard Dyadic form.
        /// Otherwise, pre-generates dyadic approximations from both sides.
        /// </summary>
        public static Surr FromRational(long p, long q, int depth = 10)
        {
            if (q <= 0) throw new ArgumentException("denominator must be positive");
            long g = Gcd(Math.Abs(p), q);
            p /= g; q /= g;
            // Check if it's a dyadic rational (q is a power of 2)
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

        #region ToString
        public const int DefaultFrom = -8;
        public const int DefaultLength = 17;
        public static readonly Surr[] Defaults =
            Enumerable.Range(DefaultFrom, DefaultLength).Select(n => new Surr(n)).ToArray();

        // Well-known dyadic fractions for display
        private static readonly (Surr value, string name)[] KnownFractions = {
            (Dyadic(1, 1), "1/2"), (Dyadic(-1, 1), "-1/2"),
            (Dyadic(1, 2), "1/4"), (Dyadic(-1, 2), "-1/4"),
            (Dyadic(3, 2), "3/4"), (Dyadic(-3, 2), "-3/4"),
            (Dyadic(1, 3), "1/8"), (Dyadic(-1, 3), "-1/8"),
        };

        internal static string PrintSide(IReadOnlyCollection<Surr> side)
        {
            if (side.Count == 0)
                return " ";
            return string.Join(", ", side.Select(x => x.ToString()).ToArray())+" ";
        }

        public override string ToString()
        {
            if (_displayName != null) return _displayName;

            var val = TryEvaluate(this);
            if (val.HasValue)
            {
                var d = val.Value;
                if (d.Exp == 0) return $"{d.Num}";
                if (d.Exp > 0) return $"{d.Num}/{1L << d.Exp}";
            }

            string lStr = leftInf != null ? leftInf.DisplayName : PrintSide(left).TrimEnd();
            string rStr = rightInf != null ? rightInf.DisplayName : PrintSide(right).TrimEnd();
            return $"{{ {lStr} | {rStr} }}";
        }
        #endregion

        #region equals and hashcode
        public override bool Equals(object obj)
        {
            if (obj is Surr other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            // Surreal equality is algebraic (not structural), so equal surreals
            // can have different left/right sets. We use a coarse hash that
            // won't distinguish well but stays consistent with Equals.
            return 0;
        }

        #endregion

        #region operators with long
        public static bool operator <=(Surr a, long b)
            => (a <= new Surr(b));

        public static bool operator >=(Surr a, long b)
            => (a >= new Surr(b));

        public static bool operator <=(long a, Surr b)
            => (new Surr(a) <= b);

        public static bool operator >=(long a, Surr b)
            => (new Surr(a) >= b);

        public static bool operator <(Surr a, long b)
            => (a < new Surr(b));

        public static bool operator >(Surr a, long b)
            => (a > new Surr(b));

        public static bool operator <(long a, Surr b)
            => (new Surr(a) < b);

        public static bool operator >(long a, Surr b)
            => (new Surr(a) > b);

        public static explicit operator Surr(long n)
            => new Surr(n);

        public static bool operator ==(Surr a, long b)
            => (a == new Surr(b));

        public static bool operator ==(long a, Surr b)
            => (new Surr(a) == b);

        public static bool operator !=(Surr a, long b)
            => (a != new Surr(b));

        public static bool operator !=(long a, Surr b)
            => (new Surr(a) != b);

        public static Surr operator +(long a, Surr b)
            => (new Surr(a) + b);

        public static Surr operator +(Surr a, long b)
            => (a + new Surr(b));
        #endregion

        #region basic operators
        private static IEnumerable<Surr> Safe(IReadOnlyCollection<Surr> side)
            => side ?? Array.Empty<Surr>();

        public static bool operator <=(Surr a, Surr b)
        {
            // Fast path: both fully finite numeric surreals
            var aVal = TryEvaluate(a);
            var bVal = TryEvaluate(b);
            if (aVal.HasValue && bVal.HasValue)
                return aVal.Value.CompareTo(bVal.Value) <= 0;

            // a <= b iff !(exists x in a.left: b <= x) && !(exists y in b.right: y <= a)
            bool leftHasGe = (a.leftInf != null && a.leftInf.HasElementGreaterOrEqual(b))
                          || Safe(a.left).Any(x => b <= x);

            if (leftHasGe) return false;

            bool rightHasLe = (b.rightInf != null && b.rightInf.HasElementLessOrEqual(a))
                           || Safe(b.right).Any(x => x <= a);

            return !rightHasLe;
        }

        public static bool operator >=(Surr a, Surr b)
            => b <= a;

        public static bool operator ==(Surr a, Surr b)
            => (a <= b) && (b <= a);

        public static bool operator !=(Surr a, Surr b)
            => !(a == b);

        public static bool operator <(Surr a, Surr b)
            => (a <= b) && !(b <= a);

        public static bool operator >(Surr a, Surr b)
            => (b <= a) && !(a <= b);

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
