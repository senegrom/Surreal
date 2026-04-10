using Xunit;

namespace Surreal.Tests
{
    public class DyadicTests
    {
        [Fact]
        public void Half_Plus_Half_Equals_One()
        {
            Assert.True(Surr.Half + Surr.Half == 1);
        }

        [Fact]
        public void Quarter_Times_Four_Equals_One()
        {
            Assert.True(Surr.Dyadic(1, 2) * 4 == 1);
        }

        [Fact]
        public void Half_Times_Half_Equals_Quarter()
        {
            Assert.True(Surr.Half * Surr.Half == Surr.Dyadic(1, 2));
        }

        [Fact]
        public void Quarter_Plus_ThreeQuarters_Equals_One()
        {
            Assert.True(Surr.Dyadic(1, 2) + Surr.Dyadic(3, 2) == 1);
        }

        [Fact]
        public void Half_Minus_Quarter_Equals_Quarter()
        {
            Assert.True(Surr.Half - Surr.Dyadic(1, 2) == Surr.Dyadic(1, 2));
        }

        [Fact]
        public void Simplify_Produces_Canonical_Form()
        {
            // {1,2,3|7} should simplify to 4 (simplest number in the interval)
            var bloated = new Surr(new[] { new Surr(1), new Surr(2), new Surr(3) }, new[] { new Surr(7) });
            Assert.True(bloated.Simplify() == 4);
            Assert.True(bloated == 4);
        }

        [Fact]
        public void FromRational_Dyadic_PassThrough()
        {
            // Dyadic rationals should produce standard form
            Assert.True(Surr.FromRational(3, 4) == Surr.Dyadic(3, 2));
            Assert.True(Surr.FromRational(1, 2) == Surr.Half);
            Assert.True(Surr.FromRational(1, 1) == 1);
        }

        [Fact]
        public void Negation_Of_Dyadic()
        {
            Assert.True(-Surr.Half == Surr.Dyadic(-1, 1));
            Assert.True(-Surr.Dyadic(3, 2) == Surr.Dyadic(-3, 2));
        }

        [Theory]
        [InlineData(1, 0, "1")]
        [InlineData(1, 1, "1/2")]
        [InlineData(3, 2, "3/4")]
        [InlineData(7, 3, "7/8")]
        public void ToString_Dyadic(long num, int exp, string expected)
        {
            Assert.Equal(expected, Surr.Dyadic(num, exp).ToString());
        }
    }
}
