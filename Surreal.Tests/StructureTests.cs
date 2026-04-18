using Xunit;

namespace Surreal.Tests
{
    /// <summary>Tests for birthday and sign expansion across many surreal values.</summary>
    public class StructureTests
    {
        #region Birthday comprehensive
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, 1)]
        [InlineData(2, 2)]
        [InlineData(-2, 2)]
        [InlineData(10, 10)]
        public void Birthday_Integer(long n, int expected)
        {
            Assert.Equal(expected, Surr.Birthday(new Surr(n)));
        }

        [Fact]
        public void Birthday_Dyadics()
        {
            // 1/2 = {0|1} → day 2 (max(0,1)+1)
            Assert.Equal(2, Surr.Birthday(Surr.Half));
            // 1/4 = {0|1/2} → day 3 (max(0,2)+1)
            Assert.Equal(3, Surr.Birthday(Surr.Dyadic(1, 2)));
            // 3/4 = {1/2|1} → day 3 (max(2,1)+1)
            Assert.Equal(3, Surr.Birthday(Surr.Dyadic(3, 2)));
            // 1/8 = {0|1/4} → day 4
            Assert.Equal(4, Surr.Birthday(Surr.Dyadic(1, 3)));
        }

        [Fact]
        public void Birthday_Transfinite_Unknown()
        {
            Assert.Equal(-1, Surr.Birthday(Surr.Omega));
            Assert.Equal(-1, Surr.Birthday(Surr.InverseOmega));
            Assert.Equal(-1, Surr.Birthday(Surr.FromRational(1, 3)));
        }

        [Fact]
        public void Birthday_Star()
        {
            // * = {0|0} → day 1 (max(0,0)+1)
            Assert.Equal(1, Surr.Birthday(Surr.Star));
        }

        [Fact]
        public void Birthday_Up()
        {
            // ↑ = {0|*} → day 2 (max(0,1)+1)
            Assert.Equal(2, Surr.Birthday(Surr.Up));
        }
        #endregion

        #region Sign expansion comprehensive
        [Theory]
        [InlineData(0, "")]
        [InlineData(1, "+")]
        [InlineData(-1, "-")]
        [InlineData(2, "++")]
        [InlineData(-2, "--")]
        [InlineData(3, "+++")]
        [InlineData(5, "+++++")]
        public void SignExpansion_Integer(long n, string expected)
        {
            Assert.Equal(expected, Surr.SignExpansion(new Surr(n)));
        }

        [Fact]
        public void SignExpansion_Dyadics()
        {
            // 1/2: + (toward 1) then - (back to 1/2)
            Assert.Equal("+-", Surr.SignExpansion(Surr.Half));
            // 3/4: + (to 1) - (to 1/2) + (to 3/4)
            Assert.Equal("+-+", Surr.SignExpansion(Surr.Dyadic(3, 2)));
            // -1/2: - (toward -1) then + (back to -1/2)
            Assert.Equal("-+", Surr.SignExpansion(Surr.Dyadic(-1, 1)));
        }

        [Fact]
        public void SignExpansion_Length_Equals_Birthday()
        {
            // For finite surreals, sign expansion length = birthday
            var values = new[] { new Surr(0), new Surr(1), new Surr(3), Surr.Half,
                                 Surr.Dyadic(1, 2), Surr.Dyadic(3, 2) };
            foreach (var v in values)
            {
                var se = Surr.SignExpansion(v);
                var bd = Surr.Birthday(v);
                Assert.NotNull(se);
                Assert.Equal(bd, se.Length);
            }
        }

        [Fact]
        public void SignExpansion_Transfinite_Null()
        {
            Assert.Null(Surr.SignExpansion(Surr.Omega));
            Assert.Null(Surr.SignExpansion(Surr.FromSqrt(2)));
            Assert.Null(Surr.SignExpansion(Surr.Pi()));
        }
        #endregion

        #region ToString comprehensive
        [Fact]
        public void ToString_Integers()
        {
            Assert.Equal("0", new Surr(0).ToString());
            Assert.Equal("1", new Surr(1).ToString());
            Assert.Equal("-3", new Surr(-3).ToString());
            Assert.Equal("100", new Surr(100).ToString());
        }

        [Fact]
        public void ToString_Dyadics()
        {
            Assert.Equal("1/2", Surr.Half.ToString());
            Assert.Equal("3/4", Surr.Dyadic(3, 2).ToString());
            Assert.Equal("7/8", Surr.Dyadic(7, 3).ToString());
        }

        [Fact]
        public void ToString_Named_Constants()
        {
            Assert.Equal("ω", Surr.Omega.ToString());
            Assert.Equal("1/ω", Surr.InverseOmega.ToString());
            Assert.Equal("√ω", Surr.SqrtOmega.ToString());
            Assert.Equal("ω²", Surr.OmegaSquared.ToString());
            Assert.Equal("ω^ω", Surr.OmegaToOmega.ToString());
            Assert.Equal("ε₀", Surr.EpsilonNaught.ToString());
            Assert.Equal("ζ₀", Surr.Zeta0.ToString());
            Assert.Equal("Γ₀", Surr.Gamma0.ToString());
        }

        [Fact]
        public void ToString_Rationals()
        {
            Assert.Equal("1/3", Surr.FromRational(1, 3).ToString());
            Assert.Equal("2/3", Surr.FromRational(2, 3).ToString());
        }

        [Fact]
        public void ToString_Sqrt()
        {
            Assert.Equal("√2", Surr.FromSqrt(2).ToString());
            Assert.Equal("√3", Surr.FromSqrt(3).ToString());
        }

        [Fact]
        public void ToString_Transcendentals()
        {
            Assert.Equal("π", Surr.Pi().ToString());
            Assert.Equal("e", Surr.E().ToString());
            Assert.Equal("φ", Surr.GoldenRatio().ToString());
            Assert.Equal("ln2", Surr.Ln2().ToString());
        }
        #endregion
    }
}
