using Xunit;

namespace Surreal.Tests
{
    public class ConstantsAndStructureTests
    {
        [Fact]
        public void E_Between_2_And_3()
        {
            var e = Surr.E();
            Assert.True(e > 2);
            Assert.True(e < 3);
        }

        [Fact]
        public void E_Tight_Bounds()
        {
            var e = Surr.E();
            Assert.True(e > Surr.FromRational(271, 100));  // > 2.71
            Assert.True(e < Surr.FromRational(272, 100));  // < 2.72
        }

        [Fact]
        public void E_Greater_Than_Sqrt5()
        {
            Assert.True(Surr.E() > Surr.FromSqrt(5));  // e ≈ 2.718 > √5 ≈ 2.236
        }

        [Fact]
        public void E_Less_Than_Pi()
        {
            Assert.True(Surr.E() < Surr.Pi());
        }

        [Fact]
        public void E_Equality()
        {
            Assert.True(Surr.E() == Surr.E());
        }

        [Fact]
        public void EpsilonNaught_Greater_Than_OmegaPowers()
        {
            Assert.True(Surr.EpsilonNaught > Surr.Omega);
            Assert.True(Surr.EpsilonNaught > Surr.OmegaSquared);
            Assert.True(Surr.EpsilonNaught > Surr.OmegaToOmega);
        }

        [Fact]
        public void EpsilonNaught_ToString()
        {
            Assert.Equal("ε₀", Surr.EpsilonNaught.ToString());
        }

        [Fact]
        public void Birthday_Zero()
        {
            Assert.Equal(0, Surr.Birthday(Surr.Zero));
        }

        [Fact]
        public void Birthday_Integers()
        {
            Assert.Equal(1, Surr.Birthday(new Surr(1)));
            Assert.Equal(1, Surr.Birthday(new Surr(-1)));
            Assert.Equal(5, Surr.Birthday(new Surr(5)));
        }

        [Fact]
        public void Birthday_Half()
        {
            // 1/2 = {0|1}, born day after max(birthday(0), birthday(1)) = day 2
            Assert.Equal(2, Surr.Birthday(Surr.Half));
        }

        [Fact]
        public void Birthday_Quarter()
        {
            // 1/4 = {0|1/2}, born day 3
            Assert.Equal(3, Surr.Birthday(Surr.Dyadic(1, 2)));
        }

        [Fact]
        public void Birthday_ThreeQuarters()
        {
            // 3/4 = {1/2|1}, born day 3
            Assert.Equal(3, Surr.Birthday(Surr.Dyadic(3, 2)));
        }

        [Fact]
        public void Birthday_ThreeHalves()
        {
            // 3/2 = {1|2}, born day 3
            Assert.Equal(3, Surr.Birthday(Surr.FromRational(3, 2)));
        }

        [Fact]
        public void Birthday_Transfinite_Unknown()
        {
            Assert.Equal(-1, Surr.Birthday(Surr.Omega));
        }

        [Fact]
        public void SignExpansion_Zero()
        {
            Assert.Equal("", Surr.SignExpansion(Surr.Zero));
        }

        [Fact]
        public void SignExpansion_Integers()
        {
            Assert.Equal("+", Surr.SignExpansion(new Surr(1)));
            Assert.Equal("++", Surr.SignExpansion(new Surr(2)));
            Assert.Equal("+++", Surr.SignExpansion(new Surr(3)));
            Assert.Equal("-", Surr.SignExpansion(new Surr(-1)));
            Assert.Equal("--", Surr.SignExpansion(new Surr(-2)));
        }

        [Fact]
        public void SignExpansion_Half()
        {
            // 1/2 is born on day 2: path from 0 is + (toward 1) then - (toward 0)
            Assert.Equal("+-", Surr.SignExpansion(Surr.Half));
        }

        [Fact]
        public void SignExpansion_Transfinite_Null()
        {
            Assert.Null(Surr.SignExpansion(Surr.Omega));
        }

        #region Exponentiation and ε₀
        [Theory]
        [InlineData(2, 3, 8)]
        [InlineData(3, 2, 9)]
        [InlineData(5, 0, 1)]
        [InlineData(1, 100, 1)]
        public void Pow_Integer(long b, long e, long expected)
        {
            Assert.True(Surr.Pow(new Surr(b), new Surr(e)) == expected);
        }

        [Fact]
        public void Pow_Omega_Squared()
        {
            Assert.True(Surr.Pow(Surr.Omega, new Surr(2)) == Surr.OmegaSquared);
        }

        [Fact]
        public void Pow_Omega_To_Omega()
        {
            Assert.True(Surr.Pow(Surr.Omega, Surr.Omega) == Surr.OmegaToOmega);
        }

        [Fact]
        public void Pow_Omega_To_EpsilonNaught_Equals_EpsilonNaught()
        {
            // ω^ε₀ = ε₀ — the defining property of epsilon-naught
            Assert.True(Surr.Pow(Surr.Omega, Surr.EpsilonNaught) == Surr.EpsilonNaught);
        }

        [Fact]
        public void Pow_Omega_3()
        {
            var w3 = Surr.Pow(Surr.Omega, new Surr(3));
            Assert.True(w3 > Surr.OmegaSquared);
            Assert.True(w3 > Surr.Omega);
        }

        [Fact]
        public void EpsilonNaught_Fixed_Point()
        {
            // ε₀ is a fixed point of x → ω^x
            var inner = Surr.Pow(Surr.Omega, Surr.EpsilonNaught);
            var outer = Surr.Pow(Surr.Omega, inner);
            Assert.True(outer == Surr.EpsilonNaught);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(10)]
        public void Pow_FiniteInteger_To_Omega_Equals_Omega(int n)
        {
            // n^ω = ω for n ≥ 2 (sup of all finite powers = first transfinite)
            Assert.True(Surr.Pow(new Surr(n), Surr.Omega) == Surr.Omega);
        }

        [Fact]
        public void Pow_One_To_Omega_Equals_One()
        {
            Assert.True(Surr.Pow(new Surr(1), Surr.Omega) == 1);
        }

        [Fact]
        public void FixedPoint_XToOmega_OnlyTrivial()
        {
            // x → x^ω has only trivial fixed points: 0 and 1
            // For all n ≥ 2: n^ω = ω ≠ n (diverges away)
            Assert.True(Surr.Pow(Surr.Zero, Surr.Omega) == 0);  // 0^ω = 0 ✓
            Assert.True(Surr.Pow(new Surr(1), Surr.Omega) == 1); // 1^ω = 1 ✓
            Assert.True(Surr.Pow(new Surr(2), Surr.Omega) == Surr.Omega); // 2^ω = ω ≠ 2
            Assert.True(Surr.Pow(new Surr(2), Surr.Omega) != 2);
        }

        [Fact]
        public void Iteration_XToOmega_Diverges()
        {
            // Starting from 2: 2 → 2^ω = ω → ω^ω → (ω^ω)^ω = ...
            // Each step grows, never returns to the start
            var x = new Surr(2);
            var x1 = Surr.Pow(x, Surr.Omega);        // = ω
            Assert.True(x1 == Surr.Omega);
            var x2 = Surr.Pow(x1, Surr.Omega);       // = ω^ω
            Assert.True(x2 == Surr.OmegaToOmega);
            Assert.True(x2 > x1);                     // strictly increasing
        }
        #endregion
    }
}
