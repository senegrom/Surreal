using Xunit;

namespace Surreal.Tests
{
    /// <summary>
    /// Deep tests mixing infinitesimals, large ordinals, games, and logarithms.
    /// </summary>
    public class DeepTests
    {
        #region 1/Γ₀ > 0
        [Fact]
        public void InverseGamma0_Is_Positive()
        {
            Assert.True(Surr.InverseGamma0 > 0);
        }

        [Fact]
        public void InverseGamma0_Less_Than_InverseEpsilon0()
        {
            Assert.True(Surr.InverseGamma0 < Surr.InverseEpsilon0);
        }

        [Fact]
        public void InverseEpsilon0_Less_Than_InverseOmega()
        {
            Assert.True(Surr.InverseEpsilon0 < Surr.InverseOmega);
        }

        [Fact]
        public void InverseGamma0_Less_Than_InverseOmega()
        {
            Assert.True(Surr.InverseGamma0 < Surr.InverseOmega);
        }

        [Fact]
        public void Infinitesimal_Chain()
        {
            // 0 < 1/Γ₀ < 1/ε₀ < 1/ω < 1/1000 < 1
            Assert.True(Surr.Zero < Surr.InverseGamma0);
            Assert.True(Surr.InverseGamma0 < Surr.InverseEpsilon0);
            Assert.True(Surr.InverseEpsilon0 < Surr.InverseOmega);
            Assert.True(Surr.InverseOmega < Surr.FromRational(1, 1000));
        }
        #endregion

        #region log(Γ₀) > log(ω) > 100000
        [Fact]
        public void LogOmega_Greater_Than_All_Integers()
        {
            Assert.True(Surr.LogOmega > 100);
            Assert.True(Surr.LogOmega > 100000);
        }

        [Fact]
        public void LogOmega_Less_Than_Omega()
        {
            Assert.True(Surr.LogOmega < Surr.Omega);
        }

        [Fact]
        public void LogOmega_Less_Than_SqrtOmega()
        {
            Assert.True(Surr.LogOmega < Surr.SqrtOmega);
        }

        [Fact]
        public void LogGamma0_Greater_Than_LogOmega()
        {
            Assert.True(Surr.LogGamma0 > Surr.LogOmega);
        }

        [Fact]
        public void LogGamma0_Greater_Than_100000()
        {
            Assert.True(Surr.LogGamma0 > 100000);
        }

        [Fact]
        public void LogGamma0_Less_Than_Gamma0()
        {
            Assert.True(Surr.LogGamma0 < Surr.Gamma0);
        }

        [Fact]
        public void Log_Chain()
        {
            // 100000 < log(ω) < √ω < ω < log(Γ₀) ... wait, log(Γ₀) < Γ₀ but > ω?
            // Actually log(Γ₀) > ω since Γ₀ > ω^ω, so log(Γ₀) > ω.
            // log(Γ₀) has left options including LogOmega which > all naturals.
            // And Γ₀ > ω so log(Γ₀) > log(ω) > all naturals.
            Assert.True(new Surr(100000) < Surr.LogOmega);
            Assert.True(Surr.LogOmega < Surr.LogGamma0);
        }
        #endregion

        #region star + 1/Γ₀ > star
        [Fact]
        public void Star_Plus_InverseGamma0_Greater_Than_Zero()
        {
            // * + 1/Γ₀ > 0 because 1/Γ₀ > 0 shifts * positively
            var result = Surr.Star + Surr.InverseGamma0;
            Assert.True(result > 0);
        }

        [Fact]
        public void Star_Plus_InverseGamma0_Greater_Than_Star()
        {
            // * + 1/Γ₀ > * because adding a positive value preserves order
            // (a + c > a + 0 when c > 0, and * + 0 = *)
            var starPlusEps = Surr.Star + Surr.InverseGamma0;
            Assert.True(starPlusEps > Surr.Star);
        }

        [Fact]
        public void Star_Plus_InverseGamma0_Less_Than_One()
        {
            var result = Surr.Star + Surr.InverseGamma0;
            Assert.True(result < 1);
        }
        #endregion

        [Fact]
        public void Star_Plus_InverseGamma0_Sqrt_Not_Defined()
        {
            // √(* + 1/Γ₀) is not defined: square root requires a numeric surreal,
            // but * + 1/Γ₀ contains game components (* is non-numeric).
            // While * + 1/Γ₀ > 0 (positive), it's not a "number" in Conway's sense
            // — it's a game with a positive advantage. FromSqrt/FromPredicate only
            // work for real numbers (Dedekind cuts among dyadics).
            //
            // In principle, one could define √G for positive games G as the game H
            // where H·H = G, but this requires game multiplication and isn't standard.
            var starPlusEps = Surr.Star + Surr.InverseGamma0;
            Assert.True(starPlusEps > 0);        // it IS positive
            Assert.False(starPlusEps.IsNumeric); // but NOT a number
        }

        #region Extreme ordering
        [Fact]
        public void Full_Number_Line()
        {
            // From tiniest infinitesimal to largest ordinal:
            // 0 < 1/Γ₀ < 1/ε₀ < 1/ω < √2 < π < 100000
            //   < log(ω) < √ω < ω < ω² < ε₀ < ζ₀ < Γ₀
            Assert.True(Surr.Zero < Surr.InverseGamma0);
            Assert.True(Surr.InverseGamma0 < Surr.InverseOmega);
            Assert.True(Surr.InverseOmega < Surr.FromSqrt(2));
            Assert.True(Surr.FromSqrt(2) < Surr.Pi());
            Assert.True(Surr.Pi() < new Surr(100000));
            Assert.True(new Surr(100000) < Surr.LogOmega);
            Assert.True(Surr.LogOmega < Surr.SqrtOmega);
            Assert.True(Surr.SqrtOmega < Surr.Omega);
            Assert.True(Surr.Omega < Surr.OmegaSquared);
            Assert.True(Surr.OmegaSquared < Surr.EpsilonNaught);
            Assert.True(Surr.EpsilonNaught < Surr.Zeta0);
            Assert.True(Surr.Zeta0 < Surr.Gamma0);
        }
        #endregion
    }
}
