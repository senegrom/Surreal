using Xunit;

namespace Surreal.Tests
{
    public class ComparisonTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 3)]
        [InlineData(-5, -5)]
        public void Integer_Equality(long a, long b)
        {
            Assert.True(new Surr(a) == new Surr(b));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-3, -2)]
        [InlineData(-1, 1)]
        public void Integer_LessThan(long a, long b)
        {
            Assert.True(new Surr(a) < new Surr(b));
            Assert.True(new Surr(b) > new Surr(a));
            Assert.False(new Surr(b) < new Surr(a));
        }

        [Fact]
        public void Reflexive()
        {
            var values = new[] { new Surr(0), new Surr(5), Surr.Half, Surr.Dyadic(3, 2) };
            foreach (var v in values)
            {
                Assert.True(v <= v);
                Assert.True(v >= v);
                Assert.False(v < v);
                Assert.False(v > v);
            }
        }

        [Fact]
        public void Transitive()
        {
            var a = Surr.Dyadic(1, 2); // 1/4
            var b = Surr.Half;          // 1/2
            var c = new Surr(1);
            Assert.True(a < b);
            Assert.True(b < c);
            Assert.True(a < c); // transitivity
        }

        [Fact]
        public void Dyadic_Ordering()
        {
            var quarter = Surr.Dyadic(1, 2);
            var half = Surr.Half;
            var threeQuarters = Surr.Dyadic(3, 2);

            Assert.True(quarter < half);
            Assert.True(half < threeQuarters);
            Assert.True(quarter < threeQuarters);
            Assert.True(new Surr(0) < quarter);
            Assert.True(threeQuarters < new Surr(1));
        }

        [Fact]
        public void Negative_Symmetry()
        {
            var a = new Surr(3);
            var b = new Surr(-3);
            Assert.True(-a == b);
            Assert.True(b < new Surr(0));
            Assert.True(new Surr(0) < a);
        }
    }
}
