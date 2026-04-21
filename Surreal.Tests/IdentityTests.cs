using Xunit;

namespace Surreal.Tests
{
    /// <summary>Algebraic identity tests: a/b*b=a, sqrt(x)²=x, log(exp(x))=x, Veblen etc.</summary>
    public class IdentityTests
    {
        #region Division identities
        [Theory]
        [InlineData(6, 3)]
        [InlineData(10, 2)]
        [InlineData(15, 5)]
        [InlineData(-12, 4)]
        public void Divide_Then_Multiply_Integers(long a, long b)
        {
            Assert.True((new Surr(a) / new Surr(b)) * new Surr(b) == a);
        }

        [Fact]
        public void Divide_Then_Multiply_Rationals()
        {
            var a = Surr.FromRational(2, 3);
            var b = Surr.FromRational(5, 7);
            Assert.True((a / b) * b == a);
        }

        [Fact]
        public void Divide_Then_Multiply_Sqrt()
        {
            // √6 / √2 * √2 = √6
            var a = Surr.FromSqrt(6);
            var b = Surr.FromSqrt(2);
            Assert.True((a / b) * b == a);
        }

        [Fact]
        public void Self_Division_All_Types()
        {
            Assert.True(new Surr(7) / new Surr(7) == 1);
            Assert.True(Surr.Half / Surr.Half == 1);
            Assert.True(Surr.FromRational(1, 3) / Surr.FromRational(1, 3) == 1);
            Assert.True(Surr.FromSqrt(2) / Surr.FromSqrt(2) == 1);
            Assert.True(Surr.Omega / Surr.Omega == 1);
            Assert.True(Surr.EpsilonNaught / Surr.EpsilonNaught == 1);
        }

        [Fact]
        public void Divide_By_One()
        {
            Assert.True(Surr.FromSqrt(2) / new Surr(1) == Surr.FromSqrt(2));
            Assert.True(Surr.FromRational(3, 7) / new Surr(1) == Surr.FromRational(3, 7));
        }
        #endregion

        #region Sqrt identities
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(9)]
        [InlineData(16)]
        public void Sqrt_Perfect_Square(long n)
        {
            var r = Surr.Sqrt(new Surr(n));
            Assert.True(r * r == n);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Sqrt_Squared_Equals_Input(long n)
        {
            // Sqrt(n)² = n for any non-negative integer n
            var s = Surr.Sqrt(new Surr(n));
            Assert.True(s * s == n);
        }

        [Fact]
        public void Sqrt_Of_Omega_Squared_Equals_Omega()
        {
            Assert.True(Surr.Sqrt(Surr.Omega) * Surr.Sqrt(Surr.Omega) == Surr.Omega);
        }

        [Fact]
        public void Sqrt_Of_Gamma0_Squared_Equals_Gamma0()
        {
            Assert.True(Surr.Sqrt(Surr.Gamma0) * Surr.Sqrt(Surr.Gamma0) == Surr.Gamma0);
        }

        [Fact]
        public void Sqrt_Is_Positive()
        {
            Assert.True(Surr.Sqrt(new Surr(2)) > 0);
            Assert.True(Surr.Sqrt(Surr.Omega) > 0);
            Assert.True(Surr.Sqrt(Surr.Half) > 0);
        }

        [Fact]
        public void Sqrt_Preserves_Order()
        {
            // a < b (both positive) → √a < √b
            Assert.True(Surr.Sqrt(new Surr(2)) < Surr.Sqrt(new Surr(3)));
            Assert.True(Surr.Sqrt(new Surr(4)) < Surr.Sqrt(new Surr(9)));
        }
        #endregion

        #region Exp/Log identities
        [Fact]
        public void Exp_Of_Zero_Is_One()
        {
            Assert.True(Surr.Exp(Surr.Zero) == 1);
        }

        [Fact]
        public void Log_Of_One_Is_Zero()
        {
            Assert.True(Surr.Log(new Surr(1)) == Surr.Zero);
        }

        [Fact]
        public void Exp_Of_One_Is_E()
        {
            Assert.True(Surr.Exp(new Surr(1)) == Surr.E());
        }

        [Fact]
        public void Log_Of_E_Is_One()
        {
            Assert.True(Surr.Log(Surr.E()) == 1);
        }

        [Fact]
        public void Log_Of_Omega_Is_LogOmega()
        {
            Assert.True(Surr.Log(Surr.Omega) == Surr.LogOmega);
        }

        [Fact]
        public void Exp_Of_LogOmega_Is_Omega()
        {
            Assert.True(Surr.Exp(Surr.LogOmega) == Surr.Omega);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void Log_Of_Exp_Integer(long n)
        {
            var x = new Surr(n);
            Assert.True(Surr.Log(Surr.Exp(x)) == x);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public void Exp_Of_Log_Integer(long n)
        {
            // Exp(Log(n)) = n. Requires the symbolic _logOf path.
            var x = new Surr(n);
            Assert.True(Surr.Exp(Surr.Log(x)) == x);
        }

        [Fact]
        public void Log_Exp_On_Surreal_Argument()
        {
            // Log(Exp(ω)) = ω via _expOf tag
            Assert.True(Surr.Log(Surr.Exp(Surr.Omega)) == Surr.Omega);
            Assert.True(Surr.Log(Surr.Exp(Surr.Half)) == Surr.Half);
            Assert.True(Surr.Log(Surr.Exp(Surr.FromRational(1, 3))) == Surr.FromRational(1, 3));
        }

        [Fact]
        public void Exp_Log_On_Surreal_Argument()
        {
            Assert.True(Surr.Exp(Surr.Log(Surr.Omega)) == Surr.Omega);
            Assert.True(Surr.Exp(Surr.Log(Surr.Gamma0)) == Surr.Gamma0);
        }

        [Fact]
        public void Exp_Log_On_Veblen_Ordinals()
        {
            // Inverse identities should hold for ε₀, ε_n, ζ_n, and the small Veblen ordinal.
            Assert.True(Surr.Exp(Surr.Log(Surr.EpsilonNaught)) == Surr.EpsilonNaught);
            Assert.True(Surr.Exp(Surr.Log(Surr.Epsilon(1))) == Surr.Epsilon(1));
            Assert.True(Surr.Exp(Surr.Log(Surr.Epsilon(3))) == Surr.Epsilon(3));
            Assert.True(Surr.Exp(Surr.Log(Surr.Zeta0)) == Surr.Zeta0);
            Assert.True(Surr.Exp(Surr.Log(Surr.Zeta(1))) == Surr.Zeta(1));
            Assert.True(Surr.Exp(Surr.Log(Surr.SmallVeblen())) == Surr.SmallVeblen());
        }

        [Fact]
        public void Log_Exp_On_Veblen_Ordinals()
        {
            Assert.True(Surr.Log(Surr.Exp(Surr.EpsilonNaught)) == Surr.EpsilonNaught);
            Assert.True(Surr.Log(Surr.Exp(Surr.Epsilon(2))) == Surr.Epsilon(2));
            Assert.True(Surr.Log(Surr.Exp(Surr.Zeta0)) == Surr.Zeta0);
            Assert.True(Surr.Log(Surr.Exp(Surr.Gamma0)) == Surr.Gamma0);
            Assert.True(Surr.Log(Surr.Exp(Surr.SmallVeblen())) == Surr.SmallVeblen());
        }

        [Fact]
        public void Log_Monotonic_On_Veblen_Ordinals()
        {
            // Log is monotonic: x < y ⇒ log(x) < log(y). Test with Veblen tower.
            Assert.True(Surr.Log(Surr.Omega) < Surr.Log(Surr.EpsilonNaught));
            Assert.True(Surr.Log(Surr.EpsilonNaught) < Surr.Log(Surr.Zeta0));
            Assert.True(Surr.Log(Surr.Zeta0) < Surr.Log(Surr.Gamma0));
        }

        [Fact]
        public void Log_Of_Veblen_Is_Positive_And_Less_Than_Arg()
        {
            // For x > 1 (ordinal): 0 < log(x) < x.
            var le0 = Surr.Log(Surr.EpsilonNaught);
            Assert.True(le0 > Surr.Zero);
            Assert.True(le0 < Surr.EpsilonNaught);

            var lz0 = Surr.Log(Surr.Zeta0);
            Assert.True(lz0 > Surr.Zero);
            Assert.True(lz0 < Surr.Zeta0);
        }
        #endregion

        #region Veblen identities
        [Fact]
        public void Veblen_Zero_Equals_OmegaPower()
        {
            // φ(0, n) = ω^n
            Assert.True(Surr.Veblen(0, 0) == 1);  // ω^0 = 1
            Assert.True(Surr.Veblen(0, 1) == Surr.Omega);
            Assert.True(Surr.Veblen(0, 2) == Surr.OmegaSquared);
        }

        [Fact]
        public void Veblen_One_Equals_Epsilon()
        {
            // φ(1, n) = ε_n
            Assert.True(Surr.Veblen(1, 0) == Surr.EpsilonNaught);
            Assert.True(Surr.Veblen(1, 1) == Surr.Epsilon(1));
        }

        [Fact]
        public void Veblen_Two_Equals_Zeta()
        {
            // φ(2, n) = ζ_n
            Assert.True(Surr.Veblen(2, 0) == Surr.Zeta0);
            Assert.True(Surr.Veblen(2, 1) == Surr.Zeta(1));
        }

        [Fact]
        public void Veblen_Ordering()
        {
            // φ(0, β) < φ(1, β) < φ(2, β) for any β (climbing columns)
            Assert.True(Surr.Veblen(0, 0) < Surr.Veblen(1, 0));
            Assert.True(Surr.Veblen(1, 0) < Surr.Veblen(2, 0));
            Assert.True(Surr.Veblen(2, 0) < Surr.Gamma0);
        }

        [Fact]
        public void Zeta1_Greater_Than_All_Epsilon()
        {
            // ζ₁ > ζ₀ > all ε_n
            var z1 = Surr.Zeta(1);
            Assert.True(z1 > Surr.Zeta0);
            Assert.True(z1 > Surr.Epsilon(3));
        }
        #endregion

        #region Arithmetic identities
        [Fact]
        public void Negation_Double()
        {
            // -(-x) = x
            Assert.True(-(-new Surr(5)) == 5);
            Assert.True(-(-Surr.Half) == Surr.Half);
            Assert.True(-(-Surr.FromSqrt(2)) == Surr.FromSqrt(2));
            Assert.True(-(-Surr.Omega) == Surr.Omega);
        }

        [Fact]
        public void Addition_Subtract_Inverse()
        {
            // (a + b) - b = a
            Assert.True((new Surr(5) + new Surr(3)) - new Surr(3) == 5);
            Assert.True((Surr.FromRational(1, 3) + Surr.Half) - Surr.Half == Surr.FromRational(1, 3));
        }

        [Fact]
        public void Multiply_Divide_Inverse()
        {
            // (a * b) / b = a
            Assert.True((new Surr(5) * new Surr(3)) / new Surr(3) == 5);
            Assert.True((Surr.FromSqrt(3) * new Surr(2)) / new Surr(2) == Surr.FromSqrt(3));
        }
        #endregion

        #region Distributivity and cross-type
        [Fact]
        public void Sqrt_Distributes_Over_Product()
        {
            // √(a² · b) = a · √b for positive a, b
            Assert.True(Surr.FromSqrt(12) == new Surr(2) * Surr.FromSqrt(3));
            Assert.True(Surr.FromSqrt(18) == new Surr(3) * Surr.FromSqrt(2));
        }

        [Fact]
        public void NegativeTimesNegative_IsPositive()
        {
            // (-a) · (-b) = a · b
            Assert.True((new Surr(-3)) * (new Surr(-4)) == 12);
            Assert.True((-Surr.Half) * (-Surr.Half) == Surr.Dyadic(1, 2));
            Assert.True((-Surr.FromSqrt(2)) * (-Surr.FromSqrt(3)) == Surr.FromSqrt(6));
        }

        [Fact]
        public void Cancellation_With_Transfinite()
        {
            // (a + ω) - ω = a for finite a
            Assert.True((new Surr(5) + Surr.Omega) - Surr.Omega == 5);
            Assert.True((Surr.Half + Surr.Omega) - Surr.Omega == Surr.Half);
        }

        [Fact]
        public void Omega_Minus_N_Plus_N()
        {
            // (ω - n) + n = ω
            for (long n = 1; n <= 5; n++)
                Assert.True((Surr.Omega - new Surr(n)) + new Surr(n) == Surr.Omega);
        }
        #endregion

        #region Veblen fixed-point chain
        [Fact]
        public void Veblen_Column_1_At_0_Is_EpsilonNaught()
        {
            // φ(1, 0) = ε₀
            Assert.True(Surr.Veblen(1, 0) == Surr.EpsilonNaught);
        }

        [Fact]
        public void Veblen_Ordering_Across_Columns_And_Rows()
        {
            // φ(0, 5) = ω^5. φ(1, 0) = ε₀. ε₀ > ω^n for all n.
            Assert.True(Surr.Veblen(0, 5) < Surr.Veblen(1, 0));
            // φ(1, 2) < φ(2, 0) since ζ₀ > all ε_n
            Assert.True(Surr.Veblen(1, 2) < Surr.Veblen(2, 0));
            // φ(2, 0) < Γ₀
            Assert.True(Surr.Veblen(2, 0) < Surr.Gamma0);
        }
        #endregion
    }
}
