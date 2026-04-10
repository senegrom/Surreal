using Xunit;

namespace Surreal.Tests
{
    public class RationalTests
    {
        [Fact]
        public void OneThird_Basic_Comparisons()
        {
            var third = Surr.FromRational(1, 3);
            Assert.True(third > 0);
            Assert.True(third < 1);
            Assert.True(third < Surr.Half);
            Assert.True(third > Surr.Dyadic(1, 2)); // > 1/4
        }

        [Fact]
        public void OneThird_Equality()
        {
            var a = Surr.FromRational(1, 3);
            var b = Surr.FromRational(1, 3);
            Assert.True(a == b);
        }

        [Fact]
        public void Equivalent_Fractions_Equal()
        {
            // 2/6 reduces to 1/3
            var a = Surr.FromRational(1, 3);
            var b = Surr.FromRational(2, 6);
            Assert.True(a == b);
        }

        [Fact]
        public void Rational_Ordering()
        {
            var fifth = Surr.FromRational(1, 5);
            var third = Surr.FromRational(1, 3);
            var twoThirds = Surr.FromRational(2, 3);

            Assert.True(fifth < third);
            Assert.True(third < twoThirds);
            Assert.True(fifth < twoThirds); // transitivity
            Assert.True(twoThirds < 1);
        }

        [Fact]
        public void Rational_Between_Dyadics()
        {
            var third = Surr.FromRational(1, 3);
            Assert.True(Surr.Dyadic(1, 2) < third); // 1/4 < 1/3
            Assert.True(third < Surr.Dyadic(3, 3));  // 1/3 < 3/8
        }

        [Fact]
        public void OneFifth_Basic_Comparisons()
        {
            var fifth = Surr.FromRational(1, 5);
            var third = Surr.FromRational(1, 3);
            Assert.True(fifth > 0);
            Assert.True(fifth < third);
        }

        [Fact]
        public void ThreeSevenths_Comparisons()
        {
            var x = Surr.FromRational(3, 7);
            var third = Surr.FromRational(1, 3);
            Assert.True(x > third);   // 3/7 > 1/3
            Assert.True(x < Surr.Half); // 3/7 < 1/2
        }

        [Fact]
        public void Negative_Rational()
        {
            var neg = Surr.FromRational(-1, 3);
            Assert.True(neg < 0);
            Assert.True(neg > -1);
        }

        [Fact]
        public void ToString_Rational()
        {
            Assert.Equal("1/3", Surr.FromRational(1, 3).ToString());
            Assert.Equal("2/3", Surr.FromRational(2, 3).ToString());
            Assert.Equal("1/5", Surr.FromRational(1, 5).ToString());
        }

        [Fact]
        public void Dyadic_Plus_Rational()
        {
            // 1 + 1/5 = 6/5
            var result = new Surr(1) + Surr.FromRational(1, 5);
            Assert.Equal("6/5", result.ToString());
            Assert.True(result > 1);
            Assert.True(result < 2);
        }

        [Fact]
        public void Rational_Plus_Rational()
        {
            // 1/3 + 1/3 = 2/3
            var third = Surr.FromRational(1, 3);
            var result = third + third;
            Assert.Equal("2/3", result.ToString());
            Assert.True(result > Surr.Half);
            Assert.True(result < 1);
        }
    }
}
