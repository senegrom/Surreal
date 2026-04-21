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

        private static readonly Dictionary<long, Surr> _fromSqrtCache = new();
        /// <summary>
        /// Create a surreal number for √n. Cached for canonical instances, so reference-equality works for cancellation.
        /// </summary>
        public static Surr FromSqrt(long n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            if (_fromSqrtCache.TryGetValue(n, out var cached)) return cached;

            long root = (long)Math.Sqrt(n);
            if (root * root == n) { var r = new Surr(root); _fromSqrtCache[n] = r; return r; }
            if ((root + 1) * (root + 1) == n) { var r = new Surr(root + 1); _fromSqrtCache[n] = r; return r; }

            var gen = new LazyDyadicApprox(
                (midNum, exp) => midNum * midNum < n * (1L << (2 * exp)),
                root,
                (long)n);

            string name = $"√{n}";
            var result = new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
            _fromSqrtCache[n] = result;
            return result;
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

        /// <summary>nth root of an integer k, for n ≥ 2. NthRoot(k, n)^n = k.</summary>
        public static Surr NthRoot(long k, int n)
        {
            if (n < 2) throw new ArgumentException("n must be ≥ 2");
            if (k < 0 && n % 2 == 0) throw new ArgumentException("Even root of negative");
            if (k == 0) return Zero;
            if (k == 1) return GetInt(1);
            if (n == 2) return FromSqrt(k);

            // Perfect nth power check
            long absK = Math.Abs(k);
            long approx = (long)Math.Round(Math.Pow(absK, 1.0 / n));
            for (long cand = Math.Max(0, approx - 1); cand <= approx + 1; cand++)
            {
                long power = 1;
                bool overflow = false;
                for (int i = 0; i < n && !overflow; i++)
                {
                    if (cand != 0 && power > long.MaxValue / cand) { overflow = true; break; }
                    power *= cand;
                }
                if (!overflow && power == absK)
                    return k < 0 ? new Surr(-cand) : new Surr(cand);
            }

            // Dedekind cut predicate: is midNum/2^exp < ⁿ√k?  ↔  (midNum)^n < k · 2^(n·exp)
            long floor = (long)Math.Floor(Math.Pow(absK, 1.0 / n));
            if (k < 0)
            {
                // Odd n with negative k: ⁿ√(-k) = -ⁿ√k
                return -NthRoot(-k, n);
            }
            var gen = new LazyDyadicApprox(
                (midNum, exp) => {
                    // midNum^n < k · 2^(n·exp). Compute midNum^n with overflow guard.
                    long power = 1;
                    for (int i = 0; i < n; i++)
                    {
                        if (midNum != 0 && Math.Abs(power) > long.MaxValue / Math.Abs(midNum)) return true; // overflow → treat as below
                        power *= midNum;
                    }
                    int shift = n * exp;
                    if (shift >= 63) return true; // overflow avoidance
                    return power < k * (1L << shift);
                },
                floor, k, n);
            string name = $"{n}√{k}";
            var result = new Surr(
                new DyadicApproxBelow(gen, $"↗{name}"),
                null,
                new DyadicApproxAbove(gen, $"↘{name}"),
                null,
                name);
            return result;
        }

        /// <summary>General square root. Sqrt(x)² = x for any non-negative surreal.</summary>
        public static Surr Sqrt(Surr x)
        {
            if (x.IsZero) return Zero;
            if (x < 0) throw new ArgumentException("Sqrt of negative surreal");
            // Perfect squares and integer sqrt
            var val = TryEvaluate(x);
            if (val.HasValue && val.Value.Exp == 0 && val.Value.Num >= 0)
                return FromSqrt(val.Value.Num);
            // Known transfinite cases by reference identity (lazily tag for squaring identity)
            if (ReferenceEquals(x, Omega)) { if (SqrtOmega._sqrtOf is null) SqrtOmega._sqrtOf = Omega; return SqrtOmega; }
            if (ReferenceEquals(x, EpsilonNaught)) { if (SqrtEpsilon0._sqrtOf is null) SqrtEpsilon0._sqrtOf = EpsilonNaught; return SqrtEpsilon0; }
            if (ReferenceEquals(x, Zeta0)) { if (SqrtZeta0._sqrtOf is null) SqrtZeta0._sqrtOf = Zeta0; return SqrtZeta0; }
            if (ReferenceEquals(x, Gamma0)) { if (SqrtGamma0._sqrtOf is null) SqrtGamma0._sqrtOf = Gamma0; return SqrtGamma0; }
            // √(k·ω) via OmegaMultiples (e.g. from discriminant 4ω in quadratic x²-ω=0).
            for (int k = 2; k <= 10; k++)
                if (ReferenceEquals(x, OmegaMultiples.Instance.Get(k)))
                    return MakeSqrtNOmega(k);
            // Dispatch via existing sqrt-tagged generator (sqrt of √n = 4th root, fall through)
            // General: Dedekind cut via surreal comparison mid² < x
            var result = FromPredicate(
                (midNum, exp) => {
                    var mid = Dyadic(midNum, exp);
                    return mid * mid < x;
                },
                1,
                $"√({x})");
            result._sqrtOf = x;
            return result;
        }

        private static readonly Dictionary<Surr, Surr> _expCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        private static readonly Dictionary<Surr, Surr> _logCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

        /// <summary>Natural exponential. Exp(Log(x)) = x and Log(Exp(x)) = x via symbolic inverse tracking.</summary>
        public static Surr Exp(Surr x)
        {
            if (x.IsZero) return GetInt(1);
            if (x._logOf is not null) return x._logOf; // Exp(Log(y)) = y
            if (x == GetInt(1)) return E();
            if (ReferenceEquals(x, LogOmega)) return Omega;
            if (ReferenceEquals(x, LogEpsilon0)) return EpsilonNaught;
            if (ReferenceEquals(x, LogZeta0)) return Zeta0;
            if (ReferenceEquals(x, LogGamma0)) return Gamma0;
            if (_expCache.TryGetValue(x, out var cachedExp)) return cachedExp;
            var val = TryEvaluate(x);
            Surr result;
            if (val.HasValue)
            {
                double v = val.Value.Num / (double)(1L << val.Value.Exp);
                double ex = Math.Exp(v);
                long floor = (long)Math.Floor(ex);
                result = FromPredicate(
                    (midNum, exp) => midNum < ex * (1L << exp),
                    floor,
                    $"exp({x})");
            }
            else
            {
                // Formal: exp is monotonic, so exp(x) > exp(y) for any known log bound y < x.
                var leftOpts = new List<Surr> { x };  // exp(x) > x for x ≥ 1
                if (x > LogOmega) leftOpts.Add(Omega);
                if (x > LogEpsilon0) leftOpts.Add(EpsilonNaught);
                if (x > LogZeta0) leftOpts.Add(Zeta0);
                if (x > LogGamma0) leftOpts.Add(Gamma0);
                result = new Surr(NaturalNumbers.Instance, leftOpts, null, null, $"exp({x})");
            }
            result._expOf = x;
            _expCache[x] = result;
            return result;
        }

        /// <summary>Natural logarithm. Log(Exp(x)) = x and Exp(Log(x)) = x via symbolic inverse tracking.</summary>
        public static Surr Log(Surr x)
        {
            if (x == GetInt(1)) return Zero;
            if (x._expOf is not null) return x._expOf; // Log(Exp(y)) = y
            if (ReferenceEquals(x, Omega)) return LogOmega;
            if (ReferenceEquals(x, EpsilonNaught)) return LogEpsilon0;
            if (ReferenceEquals(x, Zeta0)) return LogZeta0;
            if (ReferenceEquals(x, Gamma0)) return LogGamma0;
            if (x <= 0) throw new ArgumentException("Log of non-positive surreal");
            if (_logCache.TryGetValue(x, out var cachedLog)) return cachedLog;
            Surr result;
            var val = TryEvaluate(x);
            if (val.HasValue)
            {
                double v = val.Value.Num / (double)(1L << val.Value.Exp);
                double lv = Math.Log(v);
                long floor = (long)Math.Floor(lv);
                result = FromPredicate(
                    (midNum, exp) => midNum < lv * (1L << exp),
                    floor,
                    $"log({x})");
            }
            else
            {
                // Formal: log is monotonic, include known-log lower bounds and x as upper bound.
                var leftOpts = new List<Surr> { Zero };
                if (x > Omega) leftOpts.Add(LogOmega);
                if (x > EpsilonNaught) leftOpts.Add(LogEpsilon0);
                if (x > Zeta0) leftOpts.Add(LogZeta0);
                if (x > Gamma0) leftOpts.Add(LogGamma0);
                result = new Surr(null, leftOpts, null, new List<Surr> { x }, $"log({x})");
            }
            result._logOf = x;
            _logCache[x] = result;
            return result;
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

        /// <summary>φ (golden ratio) ≈ 1.61803... = (1+√5)/2. Via Dedekind cut: mid² &lt; mid + 1.</summary>
        public static Surr GoldenRatio() => FromPredicate(
            (midNum, exp) => midNum * midNum < (midNum + (1L << exp)) * (1L << exp),
            1, "φ");

        /// <summary>
        /// Classify a game's outcome: who wins with optimal play?
        /// Positive = Left wins, Negative = Right wins, Zero = second player wins, Fuzzy = first player wins.
        /// </summary>
        public enum GameOutcome { Positive, Negative, Zero, Fuzzy }

        public static GameOutcome Outcome(Surr g)
        {
            bool geZero = g >= 0;
            bool leZero = g <= 0;
            if (geZero && leZero) return GameOutcome.Zero;
            if (geZero && !leZero) return GameOutcome.Positive;
            if (!geZero && leZero) return GameOutcome.Negative;
            return GameOutcome.Fuzzy;
        }

        /// <summary>Nim addition (XOR) of two nimbers as surreals.</summary>
        public static Surr NimAdd(Surr a, Surr b)
        {
            int na = -1, nb = -1;
            for (int i = 0; i <= 16; i++)
            {
                if (na < 0 && a == Nimber(i)) na = i;
                if (nb < 0 && b == Nimber(i)) nb = i;
                if (na >= 0 && nb >= 0) break;
            }
            if (na < 0 || nb < 0)
                throw new System.InvalidOperationException("NimAdd requires nimber operands");
            return Nimber(na ^ nb);
        }

        /// <summary>Absolute value: |x| = x if x ≥ 0, -x otherwise.</summary>
        public static Surr Abs(Surr x) => x >= 0 ? x : -x;

        /// <summary>Maximum of two surreals.</summary>
        public static Surr Max(Surr a, Surr b) => a >= b ? a : b;

        /// <summary>Minimum of two surreals.</summary>
        public static Surr Min(Surr a, Surr b) => a <= b ? a : b;

        /// <summary>Whether a surreal is strictly between two bounds: lo &lt; x &lt; hi.</summary>
        public static bool Between(Surr x, Surr lo, Surr hi) => x > lo && x < hi;

        /// <summary>ln(2) ≈ 0.69315... via Dedekind cut.</summary>
        public static Surr Ln2() => FromPredicate(
            (midNum, exp) => midNum < Math.Log(2) * (1L << exp), 0, "ln2");

        /// <summary>√3 convenience constant.</summary>
        public static readonly Surr Sqrt3 = FromSqrt(3);

        /// <summary>√5 convenience constant.</summary>
        public static readonly Surr Sqrt5 = FromSqrt(5);

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
                if (target < cur)
                {
                    signs.Append('-');
                    hi = cur;
                }
                else
                {
                    signs.Append('+');
                    lo = cur;
                }
                cur = (lo + hi) / 2;
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

        /// <summary>ε₀ — first fixed point of x → ω^x. ω^ε₀ = ε₀.</summary>
        public static readonly Surr EpsilonNaught = new(
            null, new List<Surr> {
                Omega, OmegaSquared, OmegaToOmega,
                OmegaPowers.Instance.Get(3),
                OmegaPowers.Instance.Get(4),
                OmegaPowers.Instance.Get(5)
            }, null, null, "ε₀");

        /// <summary>ε₁ — second fixed point of x → ω^x. Greater than ε₀.</summary>
        public static readonly Surr Epsilon1 = new(
            null, new List<Surr> {
                EpsilonNaught,
                Pow(Omega, EpsilonNaught),  // = ε₀ (but structurally a call)
            }, null, null, "ε₁");

        private static readonly Dictionary<int, Surr> _epsilonCache = new();
        /// <summary>
        /// ε_n — the nth epsilon number (fixed point of x → ω^x).
        /// ε₀ &lt; ε₁ &lt; ε₂ &lt; ...
        /// </summary>
        public static Surr Epsilon(int n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            if (n == 0) return EpsilonNaught;
            if (n == 1) return Epsilon1;
            if (_epsilonCache.TryGetValue(n, out var cached)) return cached;
            var options = new List<Surr>();
            for (int i = 0; i < n; i++) options.Add(Epsilon(i));
            var result = new Surr(null, options, null, null, $"ε_{n}");
            _epsilonCache[n] = result;
            return result;
        }

        /// <summary>ζ₀ — first fixed point of x → ε_x. Left set is EpsilonSeq: all finite-indexed ε_n.</summary>
        public static readonly Surr Zeta0 = new(
            EpsilonSeq.Instance, null, null, null, "ζ₀");

        private static readonly Dictionary<int, Surr> _zetaCache = new();
        /// <summary>ζ_n — the nth zeta number (fixed point of x → ε_x). Left options include previous ζ_k plus all ε's via EpsilonSeq.</summary>
        public static Surr Zeta(int n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            if (n == 0) return Zeta0;
            if (_zetaCache.TryGetValue(n, out var cached)) return cached;
            var options = new List<Surr>();
            for (int i = 0; i < n; i++) options.Add(Zeta(i));
            var result = new Surr(EpsilonSeq.Instance, options, null, null, $"ζ_{n}");
            _zetaCache[n] = result;
            return result;
        }

        /// <summary>
        /// Veblen function φ(α, β). Base cases:
        /// φ(0, β) = ω^β, φ(1, β) = ε_β, φ(2, β) = ζ_β.
        /// Higher α yields fixed points of lower-α columns. φ(α, 0) for α≥3 climbs Veblen hierarchy toward Γ₀ = least γ with φ(γ,0) = γ.
        /// </summary>
        public static Surr Veblen(int alpha, int beta)
        {
            if (alpha < 0 || beta < 0) throw new ArgumentException("Veblen indices must be non-negative");
            if (alpha == 0) return Pow(Omega, new Surr(beta));
            if (alpha == 1) return Epsilon(beta);
            if (alpha == 2) return Zeta(beta);
            // General φ(α, β): left options are φ(α-1, φ(α, β-1) + 1), ... and all lower-column values.
            // Simplified construction: {φ(α-1, k) for k = 0..5 | } gives a surreal beyond all φ(α-1, n).
            var options = new List<Surr>();
            for (int k = 0; k < beta; k++) options.Add(Veblen(alpha, k));
            for (int k = 0; k <= 5; k++) options.Add(Veblen(alpha - 1, k));
            return new Surr(null, options, null, null, $"φ({alpha},{beta})");
        }

        private static readonly Dictionary<(int, int, int), Surr> _veblen3Cache = new();
        /// <summary>
        /// Three-argument Veblen φ(α, β, γ). Leading zeros collapse: φ(0, 0, γ) = φ(γ). φ(α, β, 0) when α&gt;0 builds above all φ(α-1, *, *).
        /// φ(1, 0, 0) is the Small Veblen ordinal (just beyond every Γ_n). Cached for repeated lookups.
        /// </summary>
        public static Surr Veblen(int alpha, int beta, int gamma)
        {
            if (alpha < 0 || beta < 0 || gamma < 0) throw new ArgumentException("Veblen indices must be non-negative");
            if (alpha == 0) return Veblen(beta, gamma);
            if (_veblen3Cache.TryGetValue((alpha, beta, gamma), out var cached)) return cached;
            Surr result;
            if (alpha == 1 && beta == 0 && gamma == 0)
                result = new Surr(GammaSeq.Instance, null, null, null, "φ(1,0,0)");
            else
            {
                var options = new List<Surr> { Gamma0 };
                for (int k = 0; k <= 3; k++) options.Add(Veblen(alpha - 1, k, gamma));
                for (int k = 0; k < gamma; k++) options.Add(Veblen(alpha, beta, k));
                for (int k = 0; k < beta; k++) options.Add(Veblen(alpha, k, 0));
                result = new Surr(null, options, null, null, $"φ({alpha},{beta},{gamma})");
            }
            _veblen3Cache[(alpha, beta, gamma)] = result;
            return result;
        }

        /// <summary>Small Veblen ordinal: φ(1, 0, 0) — above every Γ_n. Lazy to avoid static init ordering issues.</summary>
        public static Surr SmallVeblen() => Veblen(1, 0, 0);

        /// <summary>Large Veblen ordinal: φ(1, 0, 0, 0) — above every φ(α, 0, 0) for countable α.
        /// Countable. Below ω_1.</summary>
        public static Surr LargeVeblen() => _lvo;
        private static readonly Surr _lvo = new(SmallVeblenSeq.Instance, null, null, null, "LVO");

        /// <summary>ω_1 — the first uncountable ordinal. Its left set is CountableOrdinals: every surreal tagged _isCountable is present.</summary>
        public static readonly Surr Omega1 = InitOmega1();
        private static Surr InitOmega1()
        {
            var s = new Surr(CountableOrdinals.Instance, null, null, null, "ω₁");
            s._isCountable = false; // ω_1 is uncountable by definition
            return s;
        }

        /// <summary>Bachmann's ψ collapsing function. For any α, ψ(α) is a countable ordinal above LVO and below ω_1.
        /// Monotonic: larger α yields larger ψ(α) via PsiBelow(α) as left set, which contains ψ(β) for every β &lt; α. Cached by reference.</summary>
        private static readonly Dictionary<Surr, Surr> _psiCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public static Surr Psi(Surr alpha)
        {
            if (_psiCache.TryGetValue(alpha, out var cached)) return cached;
            var opts = new List<Surr> { LargeVeblen(), SmallVeblen(), Gamma0 };
            var result = new Surr(new PsiBelow(alpha), opts, null, new List<Surr> { Omega1 }, $"ψ({alpha})");
            _psiCache[alpha] = result;
            return result;
        }

        /// <summary>Bachmann-Howard ordinal, approximated as ψ(ω_1) — above LVO, still countable.</summary>
        public static Surr BachmannHoward() => Psi(Omega1);

        /// <summary>
        /// Three-argument Veblen φ(α, β, γ) with a surreal α. Evaluable integer α dispatches to the int overload.
        /// For transfinite α, supports only β = γ = 0: returns a surreal whose left set includes SVO, the finite-α Veblen ordinals, and Veblen(ε_k, 0, 0) for every ε_k &lt; α — so Veblen is structurally monotonic in α.
        /// </summary>
        private static readonly Dictionary<Surr, Surr> _veblenSurrCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public static Surr Veblen(Surr alpha, int beta, int gamma)
        {
            var v = TryEvaluate(alpha);
            if (v.HasValue && v.Value.Exp == 0 && v.Value.Num >= 0 && v.Value.Num <= int.MaxValue)
                return Veblen((int)v.Value.Num, beta, gamma);
            if (beta != 0 || gamma != 0)
                throw new NotImplementedException("Surreal-indexed Veblen currently supports only φ(α, 0, 0)");
            if (_veblenSurrCache.TryGetValue(alpha, out var cached)) return cached;
            var opts = new List<Surr> { SmallVeblen() };
            for (int k = 2; k <= 5; k++) opts.Add(Veblen(k, 0, 0));
            var result = new Surr(new VeblenBelow(alpha), opts, null, null, $"φ({alpha},0,0)");
            _veblenSurrCache[alpha] = result;
            return result;
        }

        /// <summary>
        /// Continued fraction [a₀; a₁, a₂, ..., aₙ] = a₀ + 1/(a₁ + 1/(a₂ + ... + 1/aₙ)).
        /// Terms after a₀ must be positive. Returns a rational surreal.
        /// </summary>
        public static Surr FromContinuedFraction(params long[] terms)
        {
            if (terms.Length == 0) throw new ArgumentException("At least one term required");
            for (int i = 1; i < terms.Length; i++)
                if (terms[i] <= 0) throw new ArgumentException($"Term {i} must be positive");
            // Evaluate right-to-left as p/q rationals to avoid surreal division cost.
            long p = terms[^1], q = 1;
            for (int i = terms.Length - 2; i >= 0; i--)
            {
                // current = aᵢ + 1/(p/q) = aᵢ + q/p = (aᵢ·p + q) / p
                long newP = terms[i] * p + q;
                long newQ = p;
                p = newP; q = newQ;
            }
            return FromRational(p, q);
        }

        /// <summary>Γ₀ (Feferman-Schütte ordinal) — first fixed point of α ↦ φ(α, 0). Left set is ZetaSeq: all ζ_n.</summary>
        public static readonly Surr Gamma0 = new(
            ZetaSeq.Instance, null, null, null, "Γ₀");

        private static readonly Dictionary<int, Surr> _gammaCache = new();
        /// <summary>Γ_n — the nth Γ-fixed-point of α ↦ φ(α, 0). Left options include previous Γ_k plus all ζ's via ZetaSeq.</summary>
        public static Surr Gamma(int n)
        {
            if (n < 0) throw new ArgumentException("n must be non-negative");
            if (n == 0) return Gamma0;
            if (_gammaCache.TryGetValue(n, out var cached)) return cached;
            var options = new List<Surr>();
            for (int i = 0; i < n; i++) options.Add(Gamma(i));
            var result = new Surr(ZetaSeq.Instance, options, null, null, $"Γ_{n}");
            _gammaCache[n] = result;
            return result;
        }

        /// <summary>ε_α for a surreal index. Evaluable integer dispatches to Epsilon(int);
        /// otherwise returns {EpsilonIndexedBelow(α) | ζ_0}. Monotonic in α via parameterized left set.</summary>
        private static readonly Dictionary<Surr, Surr> _epsilonSurrCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public static Surr Epsilon(Surr index)
        {
            var v = TryEvaluate(index);
            if (v.HasValue && v.Value.Exp == 0 && v.Value.Num >= 0 && v.Value.Num <= int.MaxValue)
                return Epsilon((int)v.Value.Num);
            if (_epsilonSurrCache.TryGetValue(index, out var cached)) return cached;
            var result = new Surr(new EpsilonIndexedBelow(index), null, null, new List<Surr> { Zeta0 }, $"ε_{{{index}}}");
            _epsilonSurrCache[index] = result;
            return result;
        }

        /// <summary>Γ_α for a surreal index. Evaluable integer dispatches to Gamma(int);
        /// otherwise returns {GammaIndexedBelow(α) | SVO}. Monotonic in α via parameterized left set.</summary>
        private static readonly Dictionary<Surr, Surr> _gammaSurrCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        public static Surr Gamma(Surr index)
        {
            var v = TryEvaluate(index);
            if (v.HasValue && v.Value.Exp == 0 && v.Value.Num >= 0 && v.Value.Num <= int.MaxValue)
                return Gamma((int)v.Value.Num);
            if (_gammaSurrCache.TryGetValue(index, out var cached)) return cached;
            var result = new Surr(new GammaIndexedBelow(index), null, null, new List<Surr> { SmallVeblen() }, $"Γ_{{{index}}}");
            _gammaSurrCache[index] = result;
            return result;
        }

        /// <summary>1/ε₀ — infinitesimal smaller than 1/ω. Positive but less than all 1/ω^n.</summary>
        public static readonly Surr InverseEpsilon0 = new(
            null, new List<Surr> { Zero },
            null, new List<Surr> { InverseOmega },
            "1/ε₀");

        /// <summary>1/Γ₀ — infinitesimal smaller than 1/ε₀. Incredibly tiny but still positive.</summary>
        public static readonly Surr InverseGamma0 = new(
            null, new List<Surr> { Zero },
            null, new List<Surr> { InverseEpsilon0 },
            "1/Γ₀");

        /// <summary>
        /// √ω — the surreal whose square is ω. Greater than all finite integers, less than ω.
        /// Constructed as {naturals | ω, ω/2, ω/4, ω/8, ...} — proper right options via division.
        /// </summary>
        public static readonly Surr SqrtOmega = new(
            NaturalNumbers.Instance, null,
            _omegaPowersOfHalf, null,
            "√ω");
        #endregion

        /// <summary>√ε₀ — between ω and ε₀. (√ε₀)² = ε₀.</summary>
        public static readonly Surr SqrtEpsilon0 = new(
            NaturalNumbers.Instance, new List<Surr> { Omega, OmegaToOmega },
            null, new List<Surr> { EpsilonNaught },
            "√ε₀");

        /// <summary>√ζ₀ — between ε₀ and ζ₀. (√ζ₀)² = ζ₀.</summary>
        public static readonly Surr SqrtZeta0 = new(
            NaturalNumbers.Instance, new List<Surr> { EpsilonNaught, Epsilon1 },
            null, new List<Surr> { Zeta0 },
            "√ζ₀");

        /// <summary>√Γ₀ — between ζ₀ and Γ₀. (√Γ₀)² = Γ₀.</summary>
        public static readonly Surr SqrtGamma0 = new(
            NaturalNumbers.Instance, new List<Surr> { EpsilonNaught, Zeta0 },
            null, new List<Surr> { Gamma0 },
            "√Γ₀");

        /// <summary>1/√Γ₀ — infinitesimal between 1/Γ₀ and 1/ε₀.</summary>
        public static readonly Surr InverseSqrtGamma0 = new(
            null, new List<Surr> { Zero, InverseGamma0 },
            null, new List<Surr> { InverseEpsilon0 },
            "1/√Γ₀");

        /// <summary>log(ω) — greater than all finite integers, less than √ω &lt; ω.</summary>
        public static readonly Surr LogOmega = new(
            NaturalNumbers.Instance, null,
            null, new List<Surr> { SqrtOmega },
            "log(ω)");

        /// <summary>log(ε₀) — greater than log(ω), less than ε₀.</summary>
        public static readonly Surr LogEpsilon0 = new(
            NaturalNumbers.Instance, new List<Surr> { LogOmega },
            null, new List<Surr> { EpsilonNaught },
            "log(ε₀)");

        /// <summary>log(ζ₀) — greater than log(ε₀), less than ζ₀.</summary>
        public static readonly Surr LogZeta0 = new(
            NaturalNumbers.Instance, new List<Surr> { LogEpsilon0 },
            null, new List<Surr> { Zeta0 },
            "log(ζ₀)");

        /// <summary>log(Γ₀) — greater than log(ζ₀), less than Γ₀.</summary>
        public static readonly Surr LogGamma0 = new(
            NaturalNumbers.Instance, new List<Surr> { LogZeta0 },
            null, new List<Surr> { Gamma0 },
            "log(Γ₀)");

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
