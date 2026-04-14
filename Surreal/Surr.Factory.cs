using System;
using System.Collections.Generic;
using System.Linq;

namespace Surreal
{
    public sealed partial class Surr
    {
        /// <summary>Creates the surreal number n / 2^k (a dyadic fraction).</summary>
        public static Surr Dyadic(long n, int k)
        {
            if (k == 0) return new Surr(n);
            if (k < 0) throw new ArgumentException("k must be non-negative");
            if (n % 2 == 0) return Dyadic(n / 2, k - 1);
            return new Surr(new[] { Dyadic(n - 1, k) }, new[] { Dyadic(n + 1, k) });
        }

        /// <summary>
        /// Create a surreal number from rational p/q.
        /// If p/q is a dyadic rational, returns the standard Dyadic form.
        /// Otherwise, uses lazy dyadic approximation generators.
        /// </summary>
        public static Surr FromRational(long p, long q)
        {
            if (q <= 0) throw new ArgumentException("denominator must be positive");
            long g = Gcd(Math.Abs(p), q);
            p /= g; q /= g;
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

        /// <summary>
        /// Create a surreal number for √n.
        /// Uses binary search with predicate "mid² &lt; n" to generate dyadic approximations.
        /// </summary>
        public static Surr FromSqrt(long n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            // Check for perfect squares
            long root = (long)Math.Sqrt(n);
            if (root * root == n) return new Surr(root);
            if ((root + 1) * (root + 1) == n) return new Surr(root + 1);

            // Predicate: is midNum/2^exp < √n?  ↔  midNum² < n * 4^exp
            var gen = new LazyDyadicApprox(
                (midNum, exp) => midNum * midNum < n * (1L << (2 * exp)),
                root,
                $"sqrt:{n}");

            string name = $"√{n}";
            return new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
        }

        /// <summary>
        /// Create a surreal from a general Dedekind cut predicate among dyadics.
        /// The predicate tests: is midNum/2^exp below the target value?
        /// </summary>
        public static Surr FromPredicate(Func<long, int, bool> isBelow, long floor, string name)
        {
            var gen = new LazyDyadicApprox(isBelow, floor, $"pred:{name}");
            return new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
        }

        /// <summary>
        /// π — defined by predicate: is mid &lt; π? Uses the Leibniz bound π &gt; 3 and π &lt; 4,
        /// refined by checking mid² &lt; known rational bounds of π².
        /// We use π² ≈ 9.8696 so mid² &lt; 9.8696 ↔ mid² * 10000 &lt; 98696.
        /// </summary>
        public static Surr Pi()
        {
            // π defined via Dedekind cut: is midNum/2^exp < π?
            // Uses System.Math.PI (double precision ≈ 15 significant digits)
            return FromPredicate(
                (midNum, exp) => midNum < Math.PI * (1L << exp),
                3,
                "π");
        }

        /// <summary>e (Euler's number) ≈ 2.71828... via Dedekind cut.</summary>
        public static Surr E() => FromPredicate(
            (midNum, exp) => midNum < Math.E * (1L << exp), 2, "e");

        /// <summary>
        /// Birthday of a surreal: the ordinal "day" it was first born.
        /// birthday(s) = 1 + max(birthday(x) for x in s.left ∪ s.right).
        /// Returns -1 for surreals with infinite sets (born on day ω or later).
        /// </summary>
        public static int Birthday(Surr s)
        {
            if (s.leftInf != null || s.rightInf != null) return -1;
            if (s.IsZero) return 0;

            int maxChild = -1;
            foreach (var x in Safe(s.left))
            {
                int b = Birthday(x);
                if (b < 0) return -1;
                if (b > maxChild) maxChild = b;
            }
            foreach (var x in Safe(s.right))
            {
                int b = Birthday(x);
                if (b < 0) return -1;
                if (b > maxChild) maxChild = b;
            }
            return maxChild + 1;
        }

        /// <summary>
        /// Sign expansion: the +/- sequence encoding the surreal's position in the binary tree.
        /// Integers: n copies of + (positive) or - (negative). 0: empty.
        /// Dyadics: integer signs then fractional binary digits.
        /// </summary>
        /// <summary>
        /// Sign expansion: trace the path through the surreal binary tree.
        /// At each step, + means "go right" (toward larger), - means "go left" (toward smaller).
        /// Integers: n copies of +/-. Dyadics: integer signs + fractional binary path.
        /// </summary>
        public static string SignExpansion(Surr s)
        {
            var val = TryEvaluate(s);
            if (!val.HasValue) return null;
            var d = val.Value;
            if (d.Num == 0 && d.Exp == 0) return "";
            var signs = new System.Text.StringBuilder();
            if (d.Exp == 0)
            {
                // Integer: |n| copies of + or -
                signs.Append(d.Num > 0 ? '+' : '-', (int)Math.Abs(d.Num));
                return signs.ToString();
            }
            // Dyadic n/2^k: trace the surreal tree path.
            // The surreal tree goes: 0 → ±1 → ±2 or ±1/2 → ...
            // Phase 1: integer signs. For positive target between n-1 and n: n copies of +.
            // Phase 2: binary search between two adjacent integers.
            long den = 1L << d.Exp;
            long target = d.Num; // value = target / den
            long floor = target >= 0 ? target / den : -((-target + den - 1) / den);
            long lo, hi;
            if (target > 0)
            {
                signs.Append('+', (int)(floor + 1));     // go to ceil
                lo = floor * den; hi = (floor + 1) * den; // bracket [floor, floor+1]
            }
            else if (target < 0)
            {
                signs.Append('-', (int)(-floor));        // go to floor (which is negative)
                lo = floor * den; hi = (floor + 1) * den;
            }
            else return signs.ToString(); // shouldn't reach here (target=0 handled above)

            // Phase 2: binary search [lo, hi]. Current position starts at the
            // integer (hi end for positive, lo end for negative).
            // Each step: the midpoint becomes the new node. If target is below
            // the current node → '-' (go left), else → '+' (go right).
            long cur = target > 0 ? hi : lo;
            while (cur != target)
            {
                long mid = (lo + hi) / 2;
                if (target < cur)
                {
                    signs.Append('-');
                    hi = cur; cur = mid;
                }
                else
                {
                    signs.Append('+');
                    lo = cur; cur = mid;
                }
            }
            return signs.ToString();
        }

        #region Well-known constants
        public static readonly Surr Half = Dyadic(1, 1);
        public static readonly Surr Zero = new Surr(0L);

        public static readonly Surr Omega = new(
            NaturalNumbers.Instance, null,
            null, null,
            "ω");

        public static readonly Surr InverseOmega = new(
            null, new List<Surr> { Zero },
            PositivePowersOfHalf.Instance, null,
            "1/ω");

        private static readonly OmegaPowersOfHalf _omegaPowersOfHalf = new();

        /// <summary>ω² = {0, ω, 2ω, 3ω, ... | }</summary>
        public static readonly Surr OmegaSquared = new(
            OmegaMultiples.Instance, null,
            null, null,
            "ω²");

        /// <summary>ω^ω = {1, ω, ω², ω³, ... | }</summary>
        public static readonly Surr OmegaToOmega = new(
            OmegaPowers.Instance, null,
            null, null,
            "ω^ω");

        /// <summary>ω/2 = {naturals | ω, ω-1, ω-2, ...}</summary>
        public static readonly Surr OmegaHalf = new(
            NaturalNumbers.Instance, null,
            OmegaMinusNaturals.Instance, null,
            "ω/2");

        /// <summary>ε₀ (epsilon-naught) — the first fixed point of x → ω^x. Greater than all ω^n.</summary>
        public static readonly Surr EpsilonNaught = new(
            null, new List<Surr> {
                Omega,
                OmegaSquared,
                OmegaToOmega,
                OmegaPowers.Instance.Get(3),
                OmegaPowers.Instance.Get(4),
                OmegaPowers.Instance.Get(5)
            },
            null, null,
            "ε₀");

        /// <summary>
        /// √ω — the surreal whose square is ω. Greater than all finite integers, less than ω.
        /// Constructed as {naturals | ω, ω/2, ω/4, ω/8, ...} — proper right options via division.
        /// </summary>
        public static readonly Surr SqrtOmega = new(
            NaturalNumbers.Instance, null,
            _omegaPowersOfHalf, null,
            "√ω");
        #endregion

        #region Games (non-numeric surreals)

        /// <summary>* (star) = {0|0} — fuzzy game, incomparable with 0. First player wins.</summary>
        public static readonly Surr Star = new(new[] { Zero }, new[] { Zero });

        /// <summary>↑ (up) = {0|*} — positive infinitesimal game.</summary>
        public static readonly Surr Up = new(new[] { Zero }, new[] { Star });

        /// <summary>↓ (down) = {*|0} — negative infinitesimal game.</summary>
        public static readonly Surr Down = new(new[] { Star }, new[] { Zero });

        /// <summary>Nimber *n — the Sprague-Grundy value n. *0=0, *1=*, *2={0,*|0,*}, etc.</summary>
        public static Surr Nimber(int n)
        {
            if (n == 0) return Zero;
            var options = new Surr[n];
            for (int i = 0; i < n; i++) options[i] = Nimber(i);
            return new Surr(options, options);
        }

        /// <summary>
        /// Nim product of two nimbers: *a ⊗ *b = *(NimProduct(a,b)).
        /// Uses the recursive definition:
        ///   a ⊗ b = mex({(a ⊗ b') ⊕ (a' ⊗ b) ⊕ (a' ⊗ b') : 0 ≤ a' &lt; a, 0 ≤ b' &lt; b})
        /// where ⊕ is XOR (nim-addition).
        /// </summary>
        public static int NimProduct(int a, int b)
        {
            if (a <= 1 || b <= 1) return a * b;
            if (_nimProdCache.TryGetValue((a, b), out var cached)) return cached;

            var reachable = new HashSet<int>();
            for (int ap = 0; ap < a; ap++)
                for (int bp = 0; bp < b; bp++)
                    reachable.Add(NimProduct(a, bp) ^ NimProduct(ap, b) ^ NimProduct(ap, bp));

            int mex = 0;
            while (reachable.Contains(mex)) mex++;

            _nimProdCache[(a, b)] = mex;
            _nimProdCache[(b, a)] = mex; // commutative
            return mex;
        }

        private static readonly Dictionary<(int, int), int> _nimProdCache = new();

        /// <summary>Multiply two nimbers as surreals: Nimber(a) ⊗ Nimber(b) = Nimber(NimProduct(a,b)).</summary>
        /// <summary>
        /// Temperature of a game: how much it matters who moves first.
        /// For a number: temperature = 0. For {a|b} with a,b numeric: temperature = (a-b)/2.
        /// Returns null if the game is too complex to evaluate.
        /// </summary>
        public static Surr Temperature(Surr g)
        {
            if (g.IsFinite)
            {
                var val = TryEvaluate(g);
                if (val.HasValue) return Zero; // numbers have temperature 0

                // Game with finite options: find max left - min right
                var leftVals = Safe(g.left).Select(x => TryEvaluate(x)).Where(v => v.HasValue).ToList();
                var rightVals = Safe(g.right).Select(x => TryEvaluate(x)).Where(v => v.HasValue).ToList();

                if (leftVals.Count > 0 && rightVals.Count > 0)
                {
                    // For simple hot games: temp = (maxLeft - minRight) / 2
                    // This assumes options are numbers
                    var maxLeft = leftVals.Max();
                    var minRight = rightVals.Min();
                    if (maxLeft.Value.CompareTo(minRight.Value) > 0)
                    {
                        var diff = maxLeft.Value + (-minRight.Value);
                        return Dyadic(diff.Num, diff.Exp + 1); // divide by 2
                    }
                }
                return Zero; // cold game or *, nimbers
            }
            return null; // can't compute for infinite games
        }

        /// <summary>
        /// Mean value of a game: the "average" outcome regardless of who moves first.
        /// For a number: mean = itself. For {a|b} with a,b numeric: mean = (a+b)/2.
        /// </summary>
        public static Surr Mean(Surr g)
        {
            if (g.IsFinite)
            {
                var val = TryEvaluate(g);
                if (val.HasValue) return g; // numbers are their own mean

                var leftVals = Safe(g.left).Select(x => TryEvaluate(x)).Where(v => v.HasValue).ToList();
                var rightVals = Safe(g.right).Select(x => TryEvaluate(x)).Where(v => v.HasValue).ToList();

                if (leftVals.Count > 0 && rightVals.Count > 0)
                {
                    var maxLeft = leftVals.Max();
                    var minRight = rightVals.Min();
                    var sum = maxLeft.Value + minRight.Value;
                    return Dyadic(sum.Num, sum.Exp + 1); // divide by 2
                }
                return Zero;
            }
            return null;
        }

        public static Surr NimMultiply(Surr a, Surr b)
        {
            // Extract nimber values — find n such that a == Nimber(n)
            int na = -1, nb = -1;
            for (int i = 0; i <= 16; i++)
            {
                if (na < 0 && a == Nimber(i)) na = i;
                if (nb < 0 && b == Nimber(i)) nb = i;
                if (na >= 0 && nb >= 0) break;
            }
            if (na < 0 || nb < 0)
                throw new System.InvalidOperationException("NimMultiply requires nimber operands");
            return Nimber(NimProduct(na, nb));
        }
        #endregion
    }
}
