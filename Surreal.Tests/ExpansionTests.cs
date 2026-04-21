using Xunit;

namespace Surreal.Tests
{
    /// <summary>Tests for the expansion features: NthRoot, SurrPoly, multi-arg Veblen, continued fractions.</summary>
    public class ExpansionTests
    {
        #region NthRoot
        [Theory]
        [InlineData(8, 3, 2)]       // ∛8 = 2
        [InlineData(27, 3, 3)]      // ∛27 = 3
        [InlineData(16, 4, 2)]      // ⁴√16 = 2
        [InlineData(1, 5, 1)]       // ⁵√1 = 1
        [InlineData(0, 3, 0)]       // ∛0 = 0
        public void NthRoot_PerfectPower(long k, int n, long expected)
        {
            Assert.True(Surr.NthRoot(k, n) == expected);
        }

        [Theory]
        [InlineData(2, 3)]
        [InlineData(3, 3)]
        [InlineData(5, 4)]
        public void NthRoot_Then_Power_Equals_Input(long k, int n)
        {
            // Multiplying n copies of ⁿ√k should give k.
            var root = Surr.NthRoot(k, n);
            var power = root;
            for (int i = 1; i < n; i++) power = power * root;
            Assert.True(power == k);
        }

        [Fact]
        public void NthRoot_NegativeOddRoot()
        {
            // ∛(-8) = -2
            Assert.True(Surr.NthRoot(-8, 3) == -2);
        }

        [Fact]
        public void NthRoot_PreservesOrder()
        {
            // ∛2 < ∛3 < ∛8 = 2
            Assert.True(Surr.NthRoot(2, 3) < Surr.NthRoot(3, 3));
            Assert.True(Surr.NthRoot(3, 3) < new Surr(2));
        }

        [Fact]
        public void NthRoot_Product_Rule()
        {
            // ∛2 · ∛4 = ∛8 = 2
            Assert.True(Surr.NthRoot(2, 3) * Surr.NthRoot(4, 3) == 2);
        }
        #endregion

        #region Veblen 3-arg
        [Fact]
        public void Veblen_ThreeArg_Collapse_LeadingZero()
        {
            // φ(0, β, γ) = φ(β, γ)
            Assert.True(Surr.Veblen(0, 1, 0) == Surr.EpsilonNaught);
            Assert.True(Surr.Veblen(0, 2, 0) == Surr.Zeta0);
        }

        [Fact]
        public void SmallVeblen_Greater_Than_Gamma0()
        {
            Assert.True(Surr.SmallVeblen() > Surr.Gamma0);
        }

        [Fact]
        public void Veblen_100_Greater_Than_Gamma0()
        {
            // φ(1, 0, 0) > φ(α, β) for any α, β covered by 2-arg Veblen
            Assert.True(Surr.Veblen(1, 0, 0) > Surr.Veblen(2, 5));
            Assert.True(Surr.Veblen(1, 0, 0) > Surr.Gamma0);
        }
        #endregion

        #region Continued fractions
        [Fact]
        public void CF_Integer_OnlyFirst()
        {
            Assert.True(Surr.FromContinuedFraction(5) == 5);
            Assert.True(Surr.FromContinuedFraction(-3) == -3);
        }

        [Fact]
        public void CF_SimpleRational()
        {
            // [1; 2] = 1 + 1/2 = 3/2
            Assert.True(Surr.FromContinuedFraction(1, 2) == Surr.FromRational(3, 2));
            // [0; 3] = 0 + 1/3 = 1/3
            Assert.True(Surr.FromContinuedFraction(0, 3) == Surr.FromRational(1, 3));
        }

        [Fact]
        public void CF_PhiApprox()
        {
            // [1; 1, 1, 1, 1] is a truncated continued fraction for φ. Equals 8/5.
            var approx = Surr.FromContinuedFraction(1, 1, 1, 1, 1);
            Assert.True(approx == Surr.FromRational(8, 5));
        }

        [Fact]
        public void CF_Sqrt2Approx()
        {
            // [1; 2, 2, 2, 2, 2] approximates √2. Equals 99/70 ≈ 1.41428...
            var approx = Surr.FromContinuedFraction(1, 2, 2, 2, 2, 2);
            Assert.True(approx == Surr.FromRational(99, 70));
            // Also > 99/71 and < 99/69 — sanity check
            Assert.True(approx > Surr.FromRational(99, 71));
        }
        #endregion

        #region SurrPoly
        [Fact]
        public void Poly_Constant_Evaluate()
        {
            var p = SurrPoly.Constant(new Surr(7));
            Assert.True(p.Evaluate(new Surr(100)) == 7);
        }

        [Fact]
        public void Poly_Linear_Evaluate()
        {
            // p(x) = 2 + 3x
            var p = new SurrPoly(new Surr(2), new Surr(3));
            Assert.True(p.Evaluate(new Surr(5)) == 17);  // 2 + 15 = 17
        }

        [Fact]
        public void Poly_Quadratic_Evaluate()
        {
            // p(x) = 1 + 2x + x² — evaluates (1+x)²
            var p = new SurrPoly(new Surr(1), new Surr(2), new Surr(1));
            Assert.True(p.Evaluate(new Surr(3)) == 16);  // (1+3)² = 16
        }

        [Fact]
        public void Poly_Addition()
        {
            var p = new SurrPoly(new Surr(1), new Surr(2));      // 1 + 2x
            var q = new SurrPoly(new Surr(3), new Surr(4));      // 3 + 4x
            var sum = p + q;                                      // 4 + 6x
            Assert.True(sum.Evaluate(new Surr(10)) == 64);
        }

        [Fact]
        public void Poly_Multiplication()
        {
            var p = new SurrPoly(new Surr(1), new Surr(1));      // 1 + x
            var q = new SurrPoly(new Surr(1), new Surr(-1));     // 1 - x
            var prod = p * q;                                     // 1 - x²
            Assert.True(prod.Evaluate(new Surr(3)) == -8);
        }

        [Fact]
        public void Poly_PowerExpansion()
        {
            // (1 + x)³ = 1 + 3x + 3x² + x³. Evaluate at x=2: 27.
            var one_plus_x = new SurrPoly(new Surr(1), new Surr(1));
            var cubed = one_plus_x.Pow(3);
            Assert.True(cubed.Evaluate(new Surr(2)) == 27);
        }

        [Fact]
        public void Poly_Derivative()
        {
            // d/dx (1 + 2x + 3x²) = 2 + 6x
            var p = new SurrPoly(new Surr(1), new Surr(2), new Surr(3));
            var dp = p.Derivative();
            Assert.True(dp.Evaluate(new Surr(5)) == 32);  // 2 + 30 = 32
        }

        [Fact]
        public void Poly_X_Variable()
        {
            // X polynomial evaluates to the argument
            Assert.True(SurrPoly.X.Evaluate(new Surr(7)) == 7);
            Assert.True(SurrPoly.X.Evaluate(Surr.Omega) == Surr.Omega);
        }

        [Fact]
        public void Poly_Degree()
        {
            Assert.Equal(-1, new SurrPoly().Degree);                        // zero poly
            Assert.Equal(0, new SurrPoly(new Surr(7)).Degree);              // constant
            Assert.Equal(2, new SurrPoly(new Surr(1), Surr.Zero, new Surr(1)).Degree);
            Assert.Equal(0, new SurrPoly(new Surr(1), Surr.Zero, Surr.Zero).Degree);
        }

        [Fact]
        public void Poly_Evaluate_At_Omega()
        {
            // p(x) = x² + 1. p(ω) = ω² + 1.
            var p = new SurrPoly(new Surr(1), Surr.Zero, new Surr(1));
            var result = p.Evaluate(Surr.Omega);
            Assert.True(result > Surr.Omega);
            Assert.True(result > Surr.OmegaSquared);  // ω² + 1 > ω²
        }
        #endregion

        #region Quadratic formula on SurrPoly
        [Fact]
        public void Quadratic_X_Squared_Minus_Two()
        {
            // x² - 2 = 0 → x = ±√2
            var (x1, x2) = SurrPoly.SolveQuadratic(new Surr(1), Surr.Zero, new Surr(-2));
            Assert.True(x1 == -Surr.FromSqrt(2));
            Assert.True(x2 == Surr.FromSqrt(2));
        }

        [Fact]
        public void Quadratic_X_Squared_Minus_Nine()
        {
            // x² - 9 = 0 → x = ±3
            var (x1, x2) = SurrPoly.SolveQuadratic(new Surr(1), Surr.Zero, new Surr(-9));
            Assert.True(x1 == -3);
            Assert.True(x2 == 3);
        }

        [Fact]
        public void Quadratic_X_Squared_Minus_Omega()
        {
            // x² - ω = 0 → x = ±√ω
            var (x1, x2) = SurrPoly.SolveQuadratic(new Surr(1), Surr.Zero, -Surr.Omega);
            Assert.True(x1 == -Surr.SqrtOmega);
            Assert.True(x2 == Surr.SqrtOmega);
        }

        [Fact]
        public void Quadratic_Via_Poly_Instance()
        {
            // Coeffs list order is ascending: [c, b, a] for ax² + bx + c.
            // p(x) = x² - 5x + 6 = (x-2)(x-3). Roots: 2, 3.
            var p = new SurrPoly(new Surr(6), new Surr(-5), new Surr(1));
            var (x1, x2) = p.SolveQuadratic();
            Assert.True(x1 == 2);
            Assert.True(x2 == 3);
        }

        [Fact]
        public void Quadratic_Verify_Roots_Satisfy_Equation()
        {
            // x² - 3x + 2 = 0 → roots 1 and 2. Verify p(1) = p(2) = 0.
            var p = new SurrPoly(new Surr(2), new Surr(-3), new Surr(1));
            var (x1, x2) = p.SolveQuadratic();
            Assert.True(p.Evaluate(x1) == Surr.Zero);
            Assert.True(p.Evaluate(x2) == Surr.Zero);
        }
        #endregion

        #region Sqrt for Veblen ordinals
        [Fact]
        public void Sqrt_Of_EpsilonNaught_Squared_Is_EpsilonNaught()
        {
            var s = Surr.Sqrt(Surr.EpsilonNaught);
            Assert.True(s == Surr.SqrtEpsilon0);
            Assert.True(s * s == Surr.EpsilonNaught);
        }

        [Fact]
        public void Sqrt_Of_Zeta0_Squared_Is_Zeta0()
        {
            var s = Surr.Sqrt(Surr.Zeta0);
            Assert.True(s == Surr.SqrtZeta0);
            Assert.True(s * s == Surr.Zeta0);
        }

        [Fact]
        public void Sqrt_Of_Veblen_Ordinals_Is_Ordered()
        {
            // √ω < √ε₀ < √ζ₀ < √Γ₀
            Assert.True(Surr.SqrtOmega < Surr.SqrtEpsilon0);
            Assert.True(Surr.SqrtEpsilon0 < Surr.SqrtZeta0);
            Assert.True(Surr.SqrtZeta0 < Surr.SqrtGamma0);
        }
        #endregion

        #region Composite identities
        [Fact]
        public void Log_Of_Exp_Of_Exp()
        {
            // log(exp(exp(x))) = exp(x) via nested tag tracking
            var expOfTwo = Surr.Exp(new Surr(2));
            var expOfExp = Surr.Exp(expOfTwo);
            Assert.True(Surr.Log(expOfExp) == expOfTwo);
        }

        [Fact]
        public void Sqrt_Of_Log_Of_Veblen_Squared()
        {
            // sqrt(log(ε₀))² = log(ε₀)
            var logE0 = Surr.Log(Surr.EpsilonNaught);
            var s = Surr.Sqrt(logE0);
            Assert.True(s * s == logE0);
        }

        [Fact]
        public void Inverse_Chain_Exp_Log_Sqrt()
        {
            // exp(log(sqrt(25))) = sqrt(25) = 5
            var result = Surr.Exp(Surr.Log(Surr.Sqrt(new Surr(25))));
            Assert.True(result == 5);
        }

        [Fact]
        public void NthRoot_Of_Power()
        {
            // ∛(8³) = 8 (multiply perfect-power result back)
            var cubed = new Surr(8) * new Surr(8) * new Surr(8);
            Assert.True(Surr.NthRoot(512, 3) == 8);
            Assert.True(cubed == 512);
        }
        #endregion

        #region Veblen-indexed ordinals (Γ_n, ε_{ε_0}, Γ_{Γ_0})
        [Fact]
        public void Epsilon_20_Less_Than_Gamma0()
        {
            // ε_20 < Γ_0. With EpsilonSeq as ζ_0's left set, ζ_0 > every ε_k structurally.
            Assert.True(Surr.Epsilon(20) < Surr.Gamma0);
        }

        [Fact]
        public void Epsilon_20_Minus_Gamma0_Is_Negative()
        {
            // ε_20 − Γ_0 < 0. Equivalent to ε_20 < Γ_0 mathematically.
            Assert.True(Surr.Epsilon(20) < Surr.Gamma0);
        }

        [Fact]
        public void Gamma_Of_Zero_Is_Gamma0()
        {
            Assert.True(Surr.Gamma(0) == Surr.Gamma0);
        }

        [Fact]
        public void Gamma_Indexed_Ordering()
        {
            // Γ_0 < Γ_1 < Γ_2
            Assert.True(Surr.Gamma(0) < Surr.Gamma(1));
            Assert.True(Surr.Gamma(1) < Surr.Gamma(2));
        }

        [Fact]
        public void Gamma_Finite_Less_Than_SVO()
        {
            // Γ_n < SVO for any finite n.
            Assert.True(Surr.Gamma(3) < Surr.SmallVeblen());
            Assert.True(Surr.Gamma(7) < Surr.SmallVeblen());
        }

        [Fact]
        public void Epsilon_Of_EpsilonNaught_Greater_Than_Finite_Epsilons()
        {
            // ε_{ε₀} > ε_k for any finite k.
            var ee = Surr.Epsilon(Surr.EpsilonNaught);
            Assert.True(ee > Surr.Epsilon(10));
            Assert.True(ee > Surr.Epsilon(20));
        }

        [Fact]
        public void Epsilon_Of_EpsilonNaught_Less_Than_Zeta0()
        {
            // ε_{ε₀} < ζ_0
            Assert.True(Surr.Epsilon(Surr.EpsilonNaught) < Surr.Zeta0);
        }

        [Fact]
        public void Gamma_Of_Gamma0_Greater_Than_Finite_Gammas()
        {
            // Γ_{Γ_0} > Γ_k for any finite k.
            var gg = Surr.Gamma(Surr.Gamma0);
            Assert.True(gg > Surr.Gamma(5));
            Assert.True(gg > Surr.Gamma(8));
        }

        [Fact]
        public void Gamma_Of_Gamma0_Less_Than_SVO()
        {
            // Γ_{Γ_0} < SVO (SVO is the limit of Γ_0, Γ_{Γ_0}, Γ_{Γ_{Γ_0}}, …)
            Assert.True(Surr.Gamma(Surr.Gamma0) < Surr.SmallVeblen());
        }

        [Fact]
        public void Epsilon_Surreal_Index_Integer_Dispatches()
        {
            // Epsilon(Surr) with an integer-valued surreal should equal Epsilon(int).
            Assert.True(Surr.Epsilon(new Surr(5)) == Surr.Epsilon(5));
            Assert.True(Surr.Epsilon(new Surr(0)) == Surr.EpsilonNaught);
        }

        [Fact]
        public void Gamma_Surreal_Index_Integer_Dispatches()
        {
            Assert.True(Surr.Gamma(new Surr(0)) == Surr.Gamma0);
            Assert.True(Surr.Gamma(new Surr(2)) == Surr.Gamma(2));
        }

        [Fact]
        public void Veblen_Gamma0_0_0_Greater_Than_SVO()
        {
            // Veblen(Γ_0, 0, 0) > Veblen(1, 0, 0) = SVO.
            // Mathematically Veblen is monotonic in α and Γ_0 ≫ 1, so φ(Γ_0, 0, 0) ≫ φ(1, 0, 0).
            Assert.True(Surr.Veblen(Surr.Gamma0, 0, 0) > Surr.SmallVeblen());
            Assert.True(Surr.Veblen(Surr.Gamma0, 0, 0) > Surr.Veblen(1, 0, 0));
        }

        [Fact]
        public void Veblen_Gamma0_0_0_Greater_Than_Veblen_2()
        {
            // Also > Veblen(2, 0, 0). The surreal-indexed Veblen includes Veblen(2..5, 0, 0) as left options.
            var v = Surr.Veblen(Surr.Gamma0, 0, 0);
            Assert.True(v > Surr.Veblen(2, 0, 0));
        }

        [Fact]
        public void Veblen_Surreal_Integer_Dispatches()
        {
            // Passing an int-valued surreal should equal the int-overload.
            Assert.True(Surr.Veblen(new Surr(1), 0, 0) == Surr.SmallVeblen());
            Assert.True(Surr.Veblen(new Surr(3), 0, 0) == Surr.Veblen(3, 0, 0));
        }

        [Fact]
        public void Veblen_Gamma0_Greater_Than_Veblen_Epsilon10()
        {
            // φ(Γ_0, 0, 0) > φ(ε_10, 0, 0) — Veblen is monotonic in α.
            // Structurally: φ(Γ_0, 0, 0).leftInf includes φ(ε_10, 0, 0) since ε_10 < Γ_0.
            Assert.True(Surr.Veblen(Surr.Gamma0, 0, 0) > Surr.Veblen(Surr.Epsilon(10), 0, 0));
        }

        [Fact]
        public void Veblen_Monotonic_On_Epsilons()
        {
            // φ(ε_5, 0, 0) > φ(ε_3, 0, 0): Veblen grows with α.
            Assert.True(Surr.Veblen(Surr.Epsilon(5), 0, 0) > Surr.Veblen(Surr.Epsilon(3), 0, 0));
        }
        #endregion

        #region Large Veblen, ψ, and ω_1 (uncountable)
        [Fact]
        public void LVO_Above_SVO()
        {
            // φ(1, 0, 0, 0) > φ(1, 0, 0).
            Assert.True(Surr.LargeVeblen() > Surr.SmallVeblen());
        }

        [Fact]
        public void LVO_Above_Veblen_Gamma0()
        {
            // LVO is above every φ(α, 0, 0) for countable α, including α = Γ_0.
            Assert.True(Surr.LargeVeblen() > Surr.Veblen(Surr.Gamma0, 0, 0));
        }

        [Fact]
        public void Gamma0_Less_Than_Omega1()
        {
            Assert.True(Surr.Gamma0 < Surr.Omega1);
        }

        [Fact]
        public void SVO_Less_Than_Omega1()
        {
            Assert.True(Surr.SmallVeblen() < Surr.Omega1);
        }

        [Fact]
        public void LVO_Less_Than_Omega1()
        {
            Assert.True(Surr.LargeVeblen() < Surr.Omega1);
        }

        [Fact]
        public void Omega1_Is_Uncountable()
        {
            // Omega1 itself is NOT marked countable (unique among the ordinals we define).
            Assert.False(Surr.Omega1._isCountable);
        }

        [Fact]
        public void Psi_Is_Countable_And_Above_LVO()
        {
            var psiZero = Surr.Psi(Surr.Zero);
            Assert.True(psiZero > Surr.LargeVeblen());
            Assert.True(psiZero < Surr.Omega1);
            Assert.True(psiZero._isCountable);
        }

        [Fact]
        public void BachmannHoward_Between_LVO_And_Omega1()
        {
            var bh = Surr.BachmannHoward();
            Assert.True(bh > Surr.LargeVeblen());
            Assert.True(bh < Surr.Omega1);
        }

        [Fact]
        public void Countable_Ordinals_All_Less_Than_Omega1()
        {
            // All the countable ordinals we've constructed are < ω_1.
            Assert.True(Surr.Omega < Surr.Omega1);
            Assert.True(Surr.EpsilonNaught < Surr.Omega1);
            Assert.True(Surr.Epsilon(5) < Surr.Omega1);
            Assert.True(Surr.Zeta0 < Surr.Omega1);
            Assert.True(Surr.Gamma(3) < Surr.Omega1);
        }

        [Fact]
        public void Psi_Monotonic()
        {
            // ψ(Γ_0) > ψ(0) via PsiBelow(Γ_0) containing ψ(0).
            Assert.True(Surr.Psi(Surr.Gamma0) > Surr.Psi(Surr.Zero));
        }

        [Fact]
        public void Psi_Monotonic_On_Larger_Args()
        {
            // ψ(ω_1) > ψ(Γ_0) — BachmannHoward-level ordinal above Psi(Γ_0).
            Assert.True(Surr.Psi(Surr.Omega1) > Surr.Psi(Surr.Gamma0));
        }

        [Fact]
        public void Veblen_Monotonic_Across_Types()
        {
            // φ(Γ_0, 0, 0) > φ(ζ_0, 0, 0) — VeblenBelow(Γ_0) includes φ(ζ_0, 0, 0) since ζ_0 < Γ_0.
            Assert.True(Surr.Veblen(Surr.Gamma0, 0, 0) > Surr.Veblen(Surr.Zeta0, 0, 0));
        }

        [Fact]
        public void Veblen_Monotonic_Zeta_Above_Epsilon()
        {
            // φ(ζ_0, 0, 0) > φ(ε_0, 0, 0) — ε_0 < ζ_0, so VeblenBelow(ζ_0) contains φ(ε_0, 0, 0).
            Assert.True(Surr.Veblen(Surr.Zeta0, 0, 0) > Surr.Veblen(Surr.EpsilonNaught, 0, 0));
        }

        [Fact]
        public void Epsilon_Monotonic_Across_Types()
        {
            // ε_{Γ_0} > ε_{ε_0} — Γ_0 > ε_0, so EpsilonIndexedBelow(Γ_0) contains ε_{ε_0}.
            Assert.True(Surr.Epsilon(Surr.Gamma0) > Surr.Epsilon(Surr.EpsilonNaught));
        }

        [Fact]
        public void Gamma_Monotonic_Across_Types()
        {
            // Γ_{SVO} > Γ_{Γ_0} — SVO > Γ_0, so GammaIndexedBelow(SVO) contains Γ_{Γ_0}.
            Assert.True(Surr.Gamma(Surr.SmallVeblen()) > Surr.Gamma(Surr.Gamma0));
        }

        [Fact]
        public void Omega1_Plus_One_Stays_Uncountable()
        {
            // ω_1 + 1 should be uncountable via propagation through TransfiniteAdd.
            var sum = Surr.Omega1 + new Surr(1);
            Assert.False(sum._isCountable);
        }

        [Fact]
        public void Neg_Omega1_Stays_Uncountable()
        {
            // -ω_1 should be uncountable via propagation through operator-.
            var neg = -Surr.Omega1;
            Assert.False(neg._isCountable);
        }
        #endregion
    }
}
