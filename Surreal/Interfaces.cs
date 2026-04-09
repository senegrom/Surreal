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
        public string DisplayName => "0,1,2,3,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Some natural n >= target iff target is finite (not transfinite)
            return Surr.TryEvaluate(target).HasValue;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // 0 is in the set, so true iff target >= 0
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(0, 0)) >= 0;
            return true; // 0 <= any transfinite
        }
    }

    public sealed class PositivePowersOfHalf : IInfiniteSet
    {
        public static readonly PositivePowersOfHalf Instance = new();
        public string DisplayName => "1,1/2,1/4,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(1, 0)) <= 0;
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(0, 0)) > 0;
            return true;
        }
    }

    /// <summary>
    /// Pre-generated list of dyadic approximations approaching a value from below.
    /// Comparison queries use surreal <= on the stored dyadics (no rational arithmetic at runtime).
    /// </summary>
    public sealed class DyadicApproxBelow : IInfiniteSet
    {
        private readonly List<Surr> _approxs;
        public string DisplayName { get; }

        public DyadicApproxBelow(List<Surr> approxs, string displayName)
        {
            _approxs = approxs;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // "Exists element in set >= target?" — check all approximations via surreal <=
            return _approxs.Any(a => target <= a);
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return _approxs.Any(a => a <= target);
        }
    }

    /// <summary>
    /// Pre-generated list of dyadic approximations approaching a value from above.
    /// Comparison queries use surreal <= on the stored dyadics (no rational arithmetic at runtime).
    /// </summary>
    public sealed class DyadicApproxAbove : IInfiniteSet
    {
        private readonly List<Surr> _approxs;
        public string DisplayName { get; }

        public DyadicApproxAbove(List<Surr> approxs, string displayName)
        {
            _approxs = approxs;
            DisplayName = displayName;
        }

        public bool HasElementGreaterOrEqual(Surr target)
        {
            return _approxs.Any(a => target <= a);
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            return _approxs.Any(a => a <= target);
        }
    }
}
