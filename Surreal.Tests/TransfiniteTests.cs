using Xunit;

namespace Surreal.Tests
{
    public class TransfiniteTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(10000)]
        [InlineData(1000000)]
        public void Omega_Greater_Than_Integer(long n)
        {
            Assert.True(Surr.Omega > n);
        }

        [Fact]
        public void Omega_Equality()
        {
            var w = Surr.Omega;
            Assert.True(w == w);
            Assert.True(w >= w);
            Assert.False(w > w);
            Assert.False(w < w);
        }

        [Fact]
        public void Omega_Plus_One()
        {
            var w = Surr.Omega;
            var w1 = new Surr(null, new[] { w }, null, null, "ω+1");
            Assert.True(w1 > w);
            Assert.False(w > w1);
            Assert.True(w1 > 100);
        }

        [Fact]
        public void Omega_Minus_One()
        {
            var w = Surr.Omega;
            var wm1 = new Surr(NaturalNumbers.Instance, null, null, new[] { w }, "ω-1");
            Assert.True(wm1 < w);
            Assert.True(wm1 > 100);
        }

        [Fact]
        public void InverseOmega_Is_Positive_Infinitesimal()
        {
            var eps = Surr.InverseOmega;
            Assert.True(eps > 0);
            Assert.True(eps < 1);
            Assert.True(eps < Surr.Half);
            Assert.True(eps < Surr.Dyadic(1, 2)); // < 1/4
            Assert.True(eps < Surr.Dyadic(1, 10)); // < 1/1024
        }

        [Fact]
        public void Omega_Greater_Than_Rationals()
        {
            Assert.True(Surr.Omega > Surr.FromRational(1, 3));
            Assert.True(Surr.Omega > Surr.FromRational(999, 1000));
        }

        [Fact]
        public void Omega_Greater_Than_InverseOmega()
        {
            Assert.True(Surr.Omega > Surr.InverseOmega);
        }

        [Fact]
        public void Rational_Greater_Than_InverseOmega()
        {
            // Any positive rational > 1/ω
            Assert.True(Surr.FromRational(1, 5) > Surr.InverseOmega);
            Assert.True(Surr.FromRational(1, 1000) > Surr.InverseOmega);
        }

        [Fact(Skip = "Addition with transfinite operands not yet implemented")]
        public void Omega_Plus_Rational_Greater_Than_Omega()
        {
            Assert.True(Surr.Omega + Surr.FromRational(1, 5) > Surr.Omega);
        }

        [Fact(Skip = "Addition with infinitesimal operands not yet implemented")]
        public void One_Plus_InverseOmega_Greater_Than_One()
        {
            Assert.True(new Surr(1) + Surr.InverseOmega > 1);
        }

        [Fact]
        public void Omega_Plus_One_Ordering()
        {
            var w = Surr.Omega;
            var w1 = new Surr(null, new[] { w }, null, null, "ω+1");
            Assert.True(w1 > w);
            Assert.False(w > w1);
            Assert.True(w1 != w);
        }

        [Fact]
        public void Omega_Minus_One_Between()
        {
            var w = Surr.Omega;
            var wm1 = new Surr(NaturalNumbers.Instance, null, null, new[] { w }, "ω-1");
            Assert.True(wm1 < w);
            Assert.True(wm1 > 100);
            Assert.True(wm1 > 0);
        }

        [Fact]
        public void InverseOmega_Smaller_Than_All_Positive_Dyadics()
        {
            var eps = Surr.InverseOmega;
            Assert.True(eps < Surr.Dyadic(1, 10)); // < 1/1024
            Assert.True(eps < Surr.Dyadic(1, 3));  // < 1/8
        }

        [Fact]
        public void Rational_Greater_Than_Infinitesimal()
        {
            Assert.True(Surr.FromRational(1, 5) > Surr.InverseOmega);
        }
    }
}
