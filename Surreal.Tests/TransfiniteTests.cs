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

        [Fact]
        public void Omega_Plus_Rational_Greater_Than_Omega()
        {
            Assert.True(Surr.Omega + Surr.FromRational(1, 5) > Surr.Omega);
        }

        [Fact]
        public void One_Plus_InverseOmega_Greater_Than_One()
        {
            Assert.True(new Surr(1) + Surr.InverseOmega > 1);
        }

        [Fact]
        public void One_Plus_Rational_Greater_Than_One_Plus_Infinitesimal()
        {
            var result1 = new Surr(1) + Surr.FromRational(1, 5);
            var result2 = new Surr(1) + Surr.InverseOmega;
            Assert.True(result1 > result2);
        }

        [Fact]
        public void Omega_Plus_Integer()
        {
            Assert.True(Surr.Omega + new Surr(1) > Surr.Omega);
        }

        [Fact]
        public void OmegaHalf_Ordering()
        {
            Assert.True(Surr.OmegaHalf > 100);
            Assert.True(Surr.OmegaHalf < Surr.Omega);
            Assert.True(Surr.OmegaHalf < Surr.Omega - 1);
        }

        [Fact]
        public void SqrtOmega_Ordering()
        {
            Assert.True(Surr.SqrtOmega > 100);
            Assert.True(Surr.SqrtOmega < Surr.Omega);
        }

        [Fact]
        public void SqrtOmega_Squared_Equals_Omega()
        {
            Assert.True(Surr.SqrtOmega * Surr.SqrtOmega == Surr.Omega);
        }

        [Fact]
        public void Difference_Of_Squares_Identity()
        {
            // (√ω - √2)(√ω + √2) + 2 = ω
            var sw = Surr.SqrtOmega;
            var s2 = Surr.FromSqrt(2);
            var product = (sw - s2) * (sw + s2);
            Assert.True(product + new Surr(2) == Surr.Omega);
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

        [Fact]
        public void Omega_Minus_N_Ordering()
        {
            // ω > ω-1 > ω-2 > ω-3 > ... > all naturals
            var w = Surr.Omega;
            var wm1 = w - 1;
            var wm2 = w - 2;
            var wm3 = w - 3;
            Assert.True(w > wm1);
            Assert.True(wm1 > wm2);
            Assert.True(wm2 > wm3);
            Assert.True(wm3 > 1000);
        }

        [Fact]
        public void OmegaHalf_Less_Than_All_OmegaMinusN()
        {
            // ω/2 < ω-n for all finite n
            Assert.True(Surr.OmegaHalf < Surr.Omega - 1);
            Assert.True(Surr.OmegaHalf < Surr.Omega - 10);
        }

        [Fact]
        public void SqrtOmega_Less_Than_OmegaHalf()
        {
            // √ω < ω/2 (since (√ω)² = ω < (ω/2)² = ω²/4)
            Assert.True(Surr.SqrtOmega < Surr.OmegaHalf);
        }

        [Fact]
        public void SqrtOmega_Times_Sqrt2()
        {
            // √ω * √2 = √(2ω)
            var result = Surr.SqrtOmega * Surr.FromSqrt(2);
            Assert.Equal("√(2ω)", result.ToString());
        }

        [Fact]
        public void Omega_Greater_Than_Sqrt()
        {
            Assert.True(Surr.Omega > Surr.FromSqrt(2));
            Assert.True(Surr.Omega > Surr.FromSqrt(1000));
        }

        [Fact]
        public void Difference_Of_Squares_With_Sqrt3()
        {
            // (√ω - √3)(√ω + √3) + 3 = ω
            var sw = Surr.SqrtOmega;
            var s3 = Surr.FromSqrt(3);
            var product = (sw - s3) * (sw + s3);
            Assert.True(product + new Surr(3) == Surr.Omega);
        }

        [Fact]
        public void Difference_Of_Squares_With_Integer()
        {
            // (√ω - 5)(√ω + 5) + 25 = ω
            var sw = Surr.SqrtOmega;
            var five = new Surr(5);
            var product = (sw - five) * (sw + five);
            Assert.True(product + new Surr(25) == Surr.Omega);
        }

        [Fact]
        public void InverseOmega_Positive_But_Less_Than_All_Rationals()
        {
            var eps = Surr.InverseOmega;
            Assert.True(eps > 0);
            Assert.True(eps < Surr.FromRational(1, 1000));
            Assert.True(eps < Surr.FromRational(1, 1000000));
        }

        [Fact]
        public void Omega_Plus_InverseOmega_Greater_Than_Omega()
        {
            Assert.True(Surr.Omega + Surr.InverseOmega > Surr.Omega);
        }

        [Fact]
        public void SqrtOmega_Greater_Than_10000()
        {
            Assert.True(Surr.SqrtOmega > 10000);
        }

        [Fact]
        public void Omega_Minus_One_Plus_One_Equals_Omega()
        {
            Assert.True((Surr.Omega - 1) + new Surr(1) == Surr.Omega);
        }

        [Fact]
        public void Omega_Minus_25_Plus_25_Equals_Omega()
        {
            Assert.True((Surr.Omega - 25) + new Surr(25) == Surr.Omega);
        }
    }
}
