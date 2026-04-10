using Xunit;

namespace Surreal.Tests
{
    public class IntegerTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 1, 2)]
        [InlineData(3, 4, 7)]
        [InlineData(-2, 5, 3)]
        [InlineData(-3, -4, -7)]
        public void Addition(long a, long b, long expected)
        {
            Assert.True(new Surr(a) + new Surr(b) == expected);
        }

        [Theory]
        [InlineData(5, 3, 2)]
        [InlineData(0, 1, -1)]
        [InlineData(-2, -5, 3)]
        public void Subtraction(long a, long b, long expected)
        {
            Assert.True(new Surr(a) - new Surr(b) == expected);
        }

        [Theory]
        [InlineData(2, 3, 6)]
        [InlineData(4, 4, 16)]
        [InlineData(-1, 3, -3)]
        [InlineData(0, 99, 0)]
        [InlineData(1, 1, 1)]
        public void Multiplication(long a, long b, long expected)
        {
            Assert.True(new Surr(a) * new Surr(b) == expected);
        }

        [Fact]
        public void Multiplication_Chained()
        {
            Assert.True(new Surr(3) * new Surr(3) * new Surr(3) == 27);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-5)]
        public void Negation_DoubleNegation(long n)
        {
            var s = new Surr(n);
            Assert.True(-(-s) == s);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-7)]
        public void AddZero_Identity(long n)
        {
            var s = new Surr(n);
            Assert.True(s + 0 == s);
            Assert.True(0 + s == s);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-3)]
        public void MultiplyOne_Identity(long n)
        {
            var s = new Surr(n);
            Assert.True(s * 1 == s);
            Assert.True(1 * s == s);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(-2)]
        public void MultiplyZero(long n)
        {
            Assert.True(new Surr(n) * 0 == 0);
        }

        [Theory]
        [InlineData(2, 3)]
        [InlineData(-1, 4)]
        public void Addition_Commutative(long a, long b)
        {
            Assert.True(new Surr(a) + new Surr(b) == new Surr(b) + new Surr(a));
        }

        [Theory]
        [InlineData(2, 3)]
        [InlineData(-1, 4)]
        public void Multiplication_Commutative(long a, long b)
        {
            Assert.True(new Surr(a) * new Surr(b) == new Surr(b) * new Surr(a));
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(-1, 0, 4)]
        public void Addition_Associative(long a, long b, long c)
        {
            var sa = new Surr(a); var sb = new Surr(b); var sc = new Surr(c);
            Assert.True((sa + sb) + sc == sa + (sb + sc));
        }
    }
}
