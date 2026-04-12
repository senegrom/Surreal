using Xunit;

namespace Surreal.Tests
{
    public class SqrtTests
    {
        [Fact]
        public void Sqrt2_Between_1_And_2()
        {
            var sqrt2 = Surr.FromSqrt(2);
            Assert.True(sqrt2 > 1);
            Assert.True(sqrt2 < 2);
        }

        [Fact]
        public void Sqrt2_Between_Tight_Dyadics()
        {
            var sqrt2 = Surr.FromSqrt(2);
            // √2 ≈ 1.41421...
            Assert.True(sqrt2 > Surr.Dyadic(11, 3));  // > 11/8 = 1.375
            Assert.True(sqrt2 < Surr.Dyadic(3, 1));   // < 3/2 = 1.5
        }

        [Fact]
        public void Sqrt2_Between_Tight_Rationals()
        {
            var sqrt2 = Surr.FromSqrt(2);
            // √2 ≈ 1.41421...
            Assert.True(sqrt2 > Surr.FromRational(7, 5));   // > 1.4
            Assert.True(sqrt2 < Surr.FromRational(3, 2));   // < 1.5
        }

        [Fact]
        public void Sqrt2_Equality()
        {
            var a = Surr.FromSqrt(2);
            var b = Surr.FromSqrt(2);
            Assert.True(a == b);
        }

        [Fact]
        public void Sqrt2_Greater_Than_Rational_Approx()
        {
            var sqrt2 = Surr.FromSqrt(2);
            // 99/70 ≈ 1.41428... > √2, and 140/99 ≈ 1.41414... < √2
            Assert.True(sqrt2 > Surr.FromRational(140, 99));
            Assert.True(sqrt2 < Surr.FromRational(99, 70));
        }

        [Fact]
        public void Sqrt2_ToString()
        {
            Assert.Equal("√2", Surr.FromSqrt(2).ToString());
        }

        [Fact]
        public void Sqrt3_Between_1_And_2()
        {
            var sqrt3 = Surr.FromSqrt(3);
            Assert.True(sqrt3 > 1);
            Assert.True(sqrt3 < 2);
        }

        [Fact]
        public void Sqrt3_Greater_Than_Sqrt2()
        {
            Assert.True(Surr.FromSqrt(3) > Surr.FromSqrt(2));
        }

        [Fact]
        public void Sqrt4_Equals_2()
        {
            // Perfect square: should return integer
            Assert.True(Surr.FromSqrt(4) == 2);
        }

        [Fact]
        public void Sqrt9_Equals_3()
        {
            Assert.True(Surr.FromSqrt(9) == 3);
        }

        [Fact]
        public void Sqrt2_Less_Than_Sqrt3_Less_Than_Sqrt5()
        {
            var s2 = Surr.FromSqrt(2);
            var s3 = Surr.FromSqrt(3);
            var s5 = Surr.FromSqrt(5);
            Assert.True(s2 < s3);
            Assert.True(s3 < s5);
            Assert.True(s2 < s5); // transitivity
        }

        [Fact]
        public void Sqrt2_Greater_Than_OneThird()
        {
            Assert.True(Surr.FromSqrt(2) > Surr.FromRational(1, 3));
        }

        [Fact]
        public void Omega_Greater_Than_Sqrt2()
        {
            Assert.True(Surr.Omega > Surr.FromSqrt(2));
        }
    }
}
