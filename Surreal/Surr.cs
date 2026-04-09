using System;
using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public sealed partial class Surr
    {
        private readonly IReadOnlyCollection<Surr> left, right;
        private readonly IInfiniteSet leftInf, rightInf;
        private readonly string _displayName;

        private Dyad? _cachedValue;
        private bool _evalAttempted;

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

        internal static IEnumerable<Surr> Safe(IReadOnlyCollection<Surr> side)
            => side ?? Array.Empty<Surr>();

        #region Evaluation and simplification
        /// <summary>Try to evaluate as a dyadic rational. Returns null for transfinite/non-dyadic surreals.</summary>
        internal static Dyad? TryEvaluate(Surr s)
        {
            if (s._cachedValue.HasValue) return s._cachedValue;
            if (s._evalAttempted) return null;
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

            s._cachedValue = Dyad.SimplestBetween(lo, hi);
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

        #region ToString
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

        internal static string PrintSide(IReadOnlyCollection<Surr> side)
        {
            if (side.Count == 0) return " ";
            return string.Join(", ", side.Select(x => x.ToString()).ToArray()) + " ";
        }
        #endregion

        #region Equals and GetHashCode
        public override bool Equals(object obj)
        {
            if (obj is Surr other) return this == other;
            return false;
        }

        public override int GetHashCode() => 0;
        #endregion
    }
}
