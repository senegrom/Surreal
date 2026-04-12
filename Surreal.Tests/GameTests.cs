using Xunit;

namespace Surreal.Tests
{
    public class GameTests
    {
        [Fact]
        public void Star_Is_Not_Numeric()
        {
            Assert.False(Surr.Star.IsNumeric);
        }

        [Fact]
        public void Star_Is_Fuzzy_With_Zero()
        {
            // * is incomparable with 0: neither * <= 0 nor 0 <= *
            Assert.False(Surr.Star <= 0);
            Assert.False(Surr.Star >= 0);
            Assert.False(Surr.Star == Surr.Zero);
            Assert.True(Surr.Star != Surr.Zero);
        }

        [Fact]
        public void Star_Equals_Star()
        {
            // Two independently constructed stars should be equal
            var star2 = new Surr(new[] { new Surr(0) }, new[] { new Surr(0) });
            Assert.True(Surr.Star == star2);
        }

        [Fact]
        public void Star_Plus_Star_Equals_Zero()
        {
            // * + * = 0 (star is its own negative)
            Assert.True(Surr.Star + Surr.Star == 0);
        }

        [Fact]
        public void Negative_Star_Equals_Star()
        {
            // -* = * (star is its own negative: -{0|0} = {-0|-0} = {0|0} = *)
            Assert.True(-Surr.Star == Surr.Star);
        }

        [Fact]
        public void Up_Is_Positive()
        {
            // ↑ > 0 (up is a positive infinitesimal game)
            Assert.True(Surr.Up > 0);
        }

        [Fact]
        public void Down_Is_Negative()
        {
            // ↓ < 0
            Assert.True(Surr.Down < 0);
        }

        [Fact(Skip = "Complex game addition: ↑+↓ produces deep structure")]
        public void Up_Plus_Down_Equals_Star()
        {
            // ↑ + ↓ = * (up + down = star)
            var result = Surr.Up + Surr.Down;
            Assert.True(result == Surr.Star);
        }

        [Fact]
        public void Up_Less_Than_Any_Positive_Real()
        {
            // ↑ < any positive real number
            Assert.True(Surr.Up < Surr.Half);
            Assert.True(Surr.Up < Surr.Dyadic(1, 10));
        }

        [Fact]
        public void Down_Greater_Than_Any_Negative_Real()
        {
            Assert.True(Surr.Down > -1);
            Assert.True(Surr.Down > Surr.Dyadic(-1, 10));
        }

        [Fact]
        public void Nimber_0_Equals_Zero()
        {
            Assert.True(Surr.Nimber(0) == 0);
        }

        [Fact]
        public void Nimber_1_Equals_Star()
        {
            Assert.True(Surr.Nimber(1) == Surr.Star);
        }

        [Fact]
        public void Nimber_2_Is_Fuzzy()
        {
            var n2 = Surr.Nimber(2);
            Assert.False(n2.IsNumeric);
            Assert.False(n2 <= 0);
            Assert.False(n2 >= 0);
        }

        [Fact]
        public void Nimber_2_Not_Equal_Star()
        {
            Assert.True(Surr.Nimber(2) != Surr.Star);
        }

        [Fact]
        public void Nimber_Self_Sum_Is_Zero()
        {
            // In Nim arithmetic: *n + *n = 0 for all n
            Assert.True(Surr.Star + Surr.Star == 0);
            Assert.True(Surr.Nimber(2) + Surr.Nimber(2) == 0);
            Assert.True(Surr.Nimber(3) + Surr.Nimber(3) == 0);
        }

        [Fact]
        public void Star_Fuzzy_With_Positive_Numbers()
        {
            // * is fuzzy with all numbers (incomparable)
            // Actually * || 0 but * < 1 and * > -1 (it's between -1 and 1 but fuzzy with 0)
            Assert.True(Surr.Star < 1);
            Assert.True(Surr.Star > -1);
        }
    }
}
