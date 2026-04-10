using Xunit;

namespace Surreal.Tests
{
    /// <summary>Tests for algebraic properties that should hold for all surreal numbers.</summary>
    public class PropertyTests
    {
        [Theory]
        [InlineData(2, 3, 4)]
        [InlineData(0, 1, -1)]
        public void Distributive_Integer(long a, long b, long c)
        {
            var sa = new Surr(a); var sb = new Surr(b); var sc = new Surr(c);
            // a * (b + c) == a*b + a*c
            Assert.True(sa * (sb + sc) == sa * sb + sa * sc);
        }

        [Fact]
        public void Distributive_Dyadic()
        {
            var half = Surr.Half;
            var quarter = Surr.Dyadic(1, 2);
            var two = new Surr(2);
            // 2 * (1/2 + 1/4) = 2 * 3/4 = 3/2
            Assert.True(two * (half + quarter) == two * half + two * quarter);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-2)]
        public void Additive_Inverse(long n)
        {
            var s = new Surr(n);
            Assert.True(s + (-s) == 0);
        }

        [Fact]
        public void Additive_Inverse_Dyadic()
        {
            Assert.True(Surr.Half + (-Surr.Half) == 0);
        }

        [Fact]
        public void Multiplication_By_Negative_One()
        {
            var three = new Surr(3);
            Assert.True(new Surr(-1) * three == -three);
        }

        [Fact]
        public void Multiplication_By_Two_Equals_DoubleAdd()
        {
            var three = new Surr(3);
            Assert.True(new Surr(2) * three == three + three);
        }

        [Fact]
        public void Square_Of_Two()
        {
            Assert.True(new Surr(2) * new Surr(2) == 4);
        }

        [Fact]
        public void Subtraction_Self_Is_Zero()
        {
            Assert.True(new Surr(5) - new Surr(5) == 0);
            Assert.True(Surr.Half - Surr.Half == 0);
        }

        [Fact]
        public void Ordering_Preserved_By_Addition()
        {
            // If a < b then a + c < b + c
            var a = new Surr(1);
            var b = new Surr(3);
            var c = new Surr(2);
            Assert.True(a < b);
            Assert.True(a + c < b + c);
        }

        [Fact]
        public void Ordering_Preserved_By_Positive_Multiplication()
        {
            // If a < b and c > 0 then a*c < b*c
            var a = new Surr(1);
            var b = new Surr(3);
            var c = new Surr(2);
            Assert.True(a * c < b * c);
        }

        [Fact]
        public void IsNumeric_For_Standard_Values()
        {
            Assert.True(new Surr(0).IsNumeric);
            Assert.True(new Surr(5).IsNumeric);
            Assert.True(Surr.Half.IsNumeric);
        }

        [Fact]
        public void Star_Is_Not_Numeric()
        {
            // * = {0|0} — the simplest fuzzy game
            var star = new Surr(new[] { new Surr(0) }, new[] { new Surr(0) });
            Assert.False(star.IsNumeric);
        }
    }
}
