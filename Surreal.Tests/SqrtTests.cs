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

        [Fact]
        public void Sqrt2_Times_Sqrt2_Equals_2()
        {
            var sqrt2 = Surr.FromSqrt(2);
            Assert.True(sqrt2 * sqrt2 == 2);
        }

        [Fact]
        public void Sqrt3_Times_Sqrt3_Equals_3()
        {
            Assert.True(Surr.FromSqrt(3) * Surr.FromSqrt(3) == 3);
        }

        [Fact]
        public void Sqrt2_Times_Sqrt3_Equals_Sqrt6()
        {
            var result = Surr.FromSqrt(2) * Surr.FromSqrt(3);
            Assert.True(result == Surr.FromSqrt(6));
        }

        [Fact]
        public void Sqrt2_Times_Sqrt8_Equals_4()
        {
            // √2 * √8 = √16 = 4
            Assert.True(Surr.FromSqrt(2) * Surr.FromSqrt(8) == 4);
        }

        [Fact]
        public void Integer_Times_Sqrt()
        {
            // 3 * √2 > 4 (since 3*1.414 ≈ 4.243)
            var result = new Surr(3) * Surr.FromSqrt(2);
            Assert.True(result > 4);
            Assert.True(result < 5);
        }

        [Fact]
        public void Rational_Times_Sqrt()
        {
            // (1/2) * √2 ≈ 0.707
            var result = Surr.Half * Surr.FromSqrt(2);
            Assert.True(result > Surr.FromRational(7, 10));
            Assert.True(result < 1);
        }

        [Fact]
        public void Sqrt5_Between_2_And_3()
        {
            var s5 = Surr.FromSqrt(5);
            Assert.True(s5 > 2);
            Assert.True(s5 < 3);
        }

        [Fact]
        public void Sqrt2_Plus_Sqrt3_Greater_Than_Sqrt5()
        {
            // √2 + √3 ≈ 2.828 > √5 ≈ 2.236
            var sum = Surr.FromSqrt(2) + Surr.FromSqrt(3);
            Assert.True(sum > Surr.FromSqrt(5));
        }

        [Fact]
        public void Sqrt2_Squared_Minus_2_Equals_Zero()
        {
            var s2 = Surr.FromSqrt(2);
            Assert.True(s2 * s2 - 2 == 0);
        }

        [Fact]
        public void Sqrt_Large_Number()
        {
            // √100 = 10 (perfect square)
            Assert.True(Surr.FromSqrt(100) == 10);
            // √101 is between 10 and 11
            var s101 = Surr.FromSqrt(101);
            Assert.True(s101 > 10);
            Assert.True(s101 < 11);
        }

        [Fact]
        public void Sqrt2_Times_Sqrt2_Times_Sqrt2_Times_Sqrt2()
        {
            // (√2)^4 = 4
            var s2 = Surr.FromSqrt(2);
            Assert.True(s2 * s2 * s2 * s2 == 4);
        }
    }
}
