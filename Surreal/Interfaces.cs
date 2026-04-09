namespace Surreal
{
    /// <summary>
    /// Represents an infinite set of surreal numbers used as left or right options.
    /// Implementations must answer comparison queries symbolically.
    /// </summary>
    public interface IInfiniteSet
    {
        /// <summary>Does this set contain any element x such that x >= target?</summary>
        bool HasElementGreaterOrEqual(Surr target);

        /// <summary>Does this set contain any element x such that x <= target?</summary>
        bool HasElementLessOrEqual(Surr target);

        string DisplayName { get; }
    }

    /// <summary>The set {0, 1, 2, 3, ...} of all non-negative integers.</summary>
    public sealed class NaturalNumbers : IInfiniteSet
    {
        public static readonly NaturalNumbers Instance = new();

        public string DisplayName => "0,1,2,3,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // "Exists n ∈ ℕ such that n >= target?"
            // True if target is a finite number (some large enough n will exceed it).
            // False if target is transfinite (no finite n reaches ω or beyond).
            var val = Surr.TryEvaluate(target);
            return val.HasValue;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // "Exists n ∈ ℕ such that n <= target?"
            // True if target >= 0 (since 0 ∈ ℕ).
            // Also true if target is transfinite (0 <= ω).
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(0, 0)) >= 0;
            // Target is transfinite — 0 is in our set and 0 <= any transfinite
            return true;
        }
    }

    /// <summary>The set {1, 1/2, 1/4, 1/8, ...} of positive powers of 1/2.</summary>
    public sealed class PositivePowersOfHalf : IInfiniteSet
    {
        public static readonly PositivePowersOfHalf Instance = new();

        public string DisplayName => "1,1/2,1/4,1/8,...";

        public bool HasElementGreaterOrEqual(Surr target)
        {
            // Largest element is 1. So true iff target <= 1.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(1, 0)) <= 0;
            // Target is transfinite — no 1/2^n reaches it
            return false;
        }

        public bool HasElementLessOrEqual(Surr target)
        {
            // Elements approach 0 from above. True iff target > 0 (some small enough 1/2^n <= target).
            // Also true for any transfinite target.
            var val = Surr.TryEvaluate(target);
            if (val.HasValue)
                return val.Value.CompareTo(new Surr.Dyad(0, 0)) > 0;
            return true;
        }
    }
}
