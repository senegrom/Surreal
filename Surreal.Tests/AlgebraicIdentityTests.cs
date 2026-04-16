using Xunit;

namespace Surreal.Tests
{
    /// <summary>Tests for algebraic identities across different number types.</summary>
    public class AlgebraicIdentityTests
    {
        [Fact]
        public void Square_Minus_Square_Integers()
        {
            // (5-3)(5+3) = 25-9 = 16
            var a = new Surr(5); var b = new Surr(3);
            Assert.True((a - b) * (a + b) == a * a - b * b);
        }

        [Fact]
        public void Perfect_Square_Dyadic()
        {
            // (3/2)² = 9/4
            var x = Surr.FromRational(3, 2);
            Assert.True(x * x == Surr.FromRational(9, 4));
        }

        [Fact]
        public void Difference_Of_Squares_Sqrt()
        {
            // (√3-√2)(√3+√2) = 3-2 = 1
            var s3 = Surr.FromSqrt(3); var s2 = Surr.FromSqrt(2);
            Assert.True(s3 * s3 - s2 * s2 == 1);
        }

        [Fact]
        public void Double_Is_Times_Two()
        {
            var x = Surr.FromRational(1, 3);
            Assert.True(x + x == x * 2);
        }

        [Fact]
        public void Subtraction_Self()
        {
            Assert.True(Surr.FromRational(7, 11) - Surr.FromRational(7, 11) == 0);
        }

        [Fact]
        public void Division_Inverse()
        {
            // x / x = 1 for various types
            Assert.True(new Surr(13) / new Surr(13) == 1);
            Assert.True(Surr.FromRational(5, 7) / Surr.FromRational(5, 7) == 1);
        }

        [Fact]
        public void Sqrt_Product_Rule()
        {
            // √(a*b) = √a * √b for perfect products
            // √2 * √8 = √16 = 4
            Assert.True(Surr.FromSqrt(2) * Surr.FromSqrt(8) == 4);
            // √3 * √12 = √36 = 6
            Assert.True(Surr.FromSqrt(3) * Surr.FromSqrt(12) == 6);
        }

        [Fact]
        public void Sqrt_Division_Rule()
        {
            // √a / √b = √(a/b) when a/b is integer
            // √18 / √2 = √9 = 3
            Assert.True(Surr.FromSqrt(18) / Surr.FromSqrt(2) == 3);
        }

        [Fact]
        public void Pow_Multiply_Rule()
        {
            // a^m * a^n = a^(m+n) for integers
            Assert.True(Surr.Pow(new Surr(2), new Surr(3)) * Surr.Pow(new Surr(2), new Surr(4))
                == Surr.Pow(new Surr(2), new Surr(7)));
        }

        [Fact]
        public void Nimber_Field_Axioms()
        {
            // Nim arithmetic forms a field (GF(2^n) for each n)
            // Additive identity: *0 ⊕ *a = *a
            for (int a = 0; a <= 7; a++)
                Assert.True(Surr.NimAdd(Surr.Nimber(0), Surr.Nimber(a)) == Surr.Nimber(a));

            // Multiplicative identity: *1 ⊗ *a = *a
            for (int a = 0; a <= 7; a++)
                Assert.Equal(a, Surr.NimProduct(1, a));

            // Distributivity: a ⊗ (b ⊕ c) = (a ⊗ b) ⊕ (a ⊗ c)
            for (int a = 0; a <= 4; a++)
                for (int b = 0; b <= 4; b++)
                    for (int c = 0; c <= 4; c++)
                        Assert.Equal(
                            Surr.NimProduct(a, b ^ c),
                            Surr.NimProduct(a, b) ^ Surr.NimProduct(a, c));
        }

        [Fact]
        public void FOIL_With_Sqrt3_And_Integer()
        {
            // (√ω - √3)(√ω + √3) + 3 = ω
            var sw = Surr.SqrtOmega;
            var s3 = Surr.FromSqrt(3);
            Assert.True((sw - s3) * (sw + s3) + new Surr(3) == Surr.Omega);
        }

        [Fact]
        public void FOIL_With_Integer_5()
        {
            // (√ω - 5)(√ω + 5) + 25 = ω
            var sw = Surr.SqrtOmega;
            var five = new Surr(5);
            Assert.True((sw - five) * (sw + five) + new Surr(25) == Surr.Omega);
        }
    }
}
