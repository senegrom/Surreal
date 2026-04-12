using System;
using Xunit;

namespace Surreal.Tests
{
    public class DivisionTests
    {
        [Theory]
        [InlineData(6, 3, 2)]
        [InlineData(10, 2, 5)]
        [InlineData(1, 1, 1)]
        [InlineData(0, 5, 0)]
        [InlineData(-6, 3, -2)]
        public void Integer_Division(long a, long b, long expected)
        {
            Assert.True(new Surr(a) / new Surr(b) == expected);
        }

        [Fact]
        public void Division_Produces_Rational()
        {
            // 1 / 3 = 1/3
            Assert.True(new Surr(1) / new Surr(3) == Surr.FromRational(1, 3));
        }

        [Fact]
        public void Division_Produces_Dyadic()
        {
            // 1 / 2 = 1/2
            Assert.True(new Surr(1) / new Surr(2) == Surr.Half);
        }

        [Fact]
        public void Rational_Division()
        {
            // (1/3) / (2/5) = 5/6
            var result = Surr.FromRational(1, 3) / Surr.FromRational(2, 5);
            Assert.True(result == Surr.FromRational(5, 6));
        }

        [Fact]
        public void Dyadic_Divided_By_Rational()
        {
            // (1/2) / (1/3) = 3/2
            var result = Surr.Half / Surr.FromRational(1, 3);
            Assert.True(result == Surr.FromRational(3, 2));
        }

        [Fact]
        public void Sqrt_Division()
        {
            // √8 / √2 = √4 = 2
            Assert.True(Surr.FromSqrt(8) / Surr.FromSqrt(2) == 2);
        }

        [Fact]
        public void Sqrt_Division_Non_Integer()
        {
            // √6 / √2 = √3
            Assert.True(Surr.FromSqrt(6) / Surr.FromSqrt(2) == Surr.FromSqrt(3));
        }

        [Fact]
        public void Omega_Divided_By_Integer()
        {
            // ω / 2 = ω/2
            var result = Surr.Omega / 2;
            Assert.True(result == Surr.OmegaHalf);
            Assert.True(result > 100);
            Assert.True(result < Surr.Omega);
        }

        [Fact]
        public void Omega_Divided_By_3()
        {
            var result = Surr.Omega / 3;
            Assert.True(result > 100);
            Assert.True(result < Surr.OmegaHalf);
            Assert.True(result < Surr.Omega);
        }

        [Fact]
        public void Divide_By_Zero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => new Surr(1) / Surr.Zero);
        }

        [Fact]
        public void Self_Division_Equals_One()
        {
            Assert.True(new Surr(7) / new Surr(7) == 1);
            Assert.True(Surr.Half / Surr.Half == 1);
            Assert.True(Surr.FromRational(1, 3) / Surr.FromRational(1, 3) == 1);
        }

        [Fact]
        public void Multiply_Then_Divide()
        {
            // (3 * 5) / 5 = 3
            Assert.True(new Surr(3) * new Surr(5) / new Surr(5) == 3);
        }
    }
}
