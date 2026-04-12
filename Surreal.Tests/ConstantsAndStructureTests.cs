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
            Assert.Equal(1, Surr.Birthday(Surr.Half));
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
    }
}
