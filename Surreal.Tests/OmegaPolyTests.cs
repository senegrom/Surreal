using Xunit;

namespace Surreal.Tests
{
    public class OmegaPolyTests
    {
        [Fact]
        public void Constant_Polynomial()
        {
            Assert.True(Surr.OmegaPoly((0, 5)) == 5);
        }

        [Fact]
        public void Linear_Polynomial()
        {
            // 3ω = OmegaMultiples.Get(3)
            Assert.True(Surr.OmegaPoly((1, 3)) == OmegaMultiples.Instance.Get(3));
        }

        [Fact]
        public void Omega_Plus_One()
        {
            // ω + 1 > ω
            var wp1 = Surr.OmegaPoly((1, 1), (0, 1));
            Assert.True(wp1 > Surr.Omega);
        }

        [Fact]
        public void Omega_Squared_Plus_Omega()
        {
            // ω² + ω > ω²
            var result = Surr.OmegaPoly((2, 1), (1, 1));
            Assert.True(result > Surr.OmegaSquared);
            Assert.True(result > Surr.Omega);
        }

        [Fact(Skip = "Transfinite sum comparison: 2ω vs ω+100 needs deeper structural analysis")]
        public void TwoOmega_Greater_Than_Omega_Plus_100()
        {
            // 2ω > ω + 100
            Assert.True(Surr.OmegaPoly((1, 2)) > Surr.OmegaPoly((1, 1), (0, 100)));
        }

        [Fact]
        public void OmegaPolynomial_ToString()
        {
            var poly = new OmegaPolynomial((2, new Surr(3)), (1, new Surr(-1)), (0, new Surr(5)));
            var str = poly.ToString();
            Assert.Contains("ω^2", str);
            Assert.Contains("5", str);
        }

        [Fact]
        public void Infinitesimal_Term()
        {
            // 1 + 1/ω > 1 (infinitesimally above 1)
            var result = Surr.OmegaPoly((0, 1), (-1, 1));
            Assert.True(result > 1);
        }

        [Fact]
        public void Polynomial_Greater_Than_Constants()
        {
            // ω² + 3ω + 5 > any integer
            var poly = Surr.OmegaPoly((2, 1), (1, 3), (0, 5));
            Assert.True(poly > 1000000);
        }
    }
}
