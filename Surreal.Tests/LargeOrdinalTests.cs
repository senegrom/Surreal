using Xunit;

namespace Surreal.Tests
{
    public class LargeOrdinalTests
    {
        #region Epsilon numbers
        [Fact]
        public void Epsilon0_FixedPoint()
        {
            Assert.True(Surr.Pow(Surr.Omega, Surr.EpsilonNaught) == Surr.EpsilonNaught);
        }

        [Fact]
        public void Epsilon1_Greater_Than_Epsilon0()
        {
            Assert.True(Surr.Epsilon(1) > Surr.EpsilonNaught);
        }

        [Fact]
        public void Epsilon1_FixedPoint()
        {
            Assert.True(Surr.Pow(Surr.Omega, Surr.Epsilon(1)) == Surr.Epsilon(1));
        }

        [Fact]
        public void Epsilon_Ordering()
        {
            // ε₀ < ε₁ < ε₂ < ε₃
            Assert.True(Surr.Epsilon(0) < Surr.Epsilon(1));
            Assert.True(Surr.Epsilon(1) < Surr.Epsilon(2));
            Assert.True(Surr.Epsilon(2) < Surr.Epsilon(3));
        }

        [Fact]
        public void Epsilon_All_Greater_Than_Omega_Powers()
        {
            // Every ε_n > ω^ω
            Assert.True(Surr.EpsilonNaught > Surr.OmegaToOmega);
            Assert.True(Surr.Epsilon(1) > Surr.OmegaToOmega);
        }

        [Fact]
        public void Epsilon_N_FixedPoint()
        {
            // Each ε_n is a fixed point of x → ω^x
            for (int n = 0; n <= 3; n++)
                Assert.True(Surr.Pow(Surr.Omega, Surr.Epsilon(n)) == Surr.Epsilon(n));
        }
        #endregion

        #region Zeta
        [Fact]
        public void Zeta0_Greater_Than_All_Epsilons()
        {
            Assert.True(Surr.Zeta0 > Surr.Epsilon(0));
            Assert.True(Surr.Zeta0 > Surr.Epsilon(1));
            Assert.True(Surr.Zeta0 > Surr.Epsilon(4));
        }

        [Fact]
        public void Zeta0_FixedPoint_Of_Epsilon()
        {
            // ζ₀ is the first fixed point of x → ε_x
            // So ε_{ζ₀} = ζ₀. Since ε_n are all fixed points of ω^x,
            // ω^ζ₀ = ζ₀ as well.
            Assert.True(Surr.Pow(Surr.Omega, Surr.Zeta0) == Surr.Zeta0);
        }

        [Fact]
        public void Zeta0_ToString()
        {
            Assert.Equal("ζ₀", Surr.Zeta0.ToString());
        }
        #endregion

        #region Gamma
        [Fact]
        public void Gamma0_Greater_Than_Zeta0()
        {
            Assert.True(Surr.Gamma0 > Surr.Zeta0);
        }

        [Fact]
        public void Gamma0_Greater_Than_All_Epsilons()
        {
            Assert.True(Surr.Gamma0 > Surr.EpsilonNaught);
            Assert.True(Surr.Gamma0 > Surr.Epsilon(4));
        }

        [Fact]
        public void Gamma0_FixedPoint()
        {
            // Γ₀ is a fixed point of the Veblen hierarchy, so ω^Γ₀ = Γ₀
            Assert.True(Surr.Pow(Surr.Omega, Surr.Gamma0) == Surr.Gamma0);
        }

        [Fact]
        public void Gamma0_ToString()
        {
            Assert.Equal("Γ₀", Surr.Gamma0.ToString());
        }
        #endregion

        #region Full ordering chain
        [Fact]
        public void Complete_Ordinal_Chain()
        {
            // ω < ω² < ω^ω < ε₀ < ε₁ < ζ₀ < Γ₀
            Assert.True(Surr.Omega < Surr.OmegaSquared);
            Assert.True(Surr.OmegaSquared < Surr.OmegaToOmega);
            Assert.True(Surr.OmegaToOmega < Surr.EpsilonNaught);
            Assert.True(Surr.EpsilonNaught < Surr.Epsilon(1));
            Assert.True(Surr.Epsilon(1) < Surr.Zeta0);
            Assert.True(Surr.Zeta0 < Surr.Gamma0);
        }
        #endregion
    }
}
