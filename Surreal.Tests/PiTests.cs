using Xunit;

namespace Surreal.Tests
{
    public class PiTests
    {
        [Fact]
        public void Pi_Between_3_And_4()
        {
            var pi = Surr.Pi();
            Assert.True(pi > 3);
            Assert.True(pi < 4);
        }

        [Fact]
        public void Pi_Tight_Bounds()
        {
            var pi = Surr.Pi();
            // π ≈ 3.14159...
            Assert.True(pi > Surr.FromRational(314, 100)); // > 3.14
            Assert.True(pi < Surr.FromRational(3142, 1000)); // < 3.142
        }

        [Fact]
        public void Pi_Greater_Than_Sqrt2()
        {
            Assert.True(Surr.Pi() > Surr.FromSqrt(2));
        }

        [Fact]
        public void Pi_Greater_Than_Rational_Lower_Bound()
        {
            // π > 3.14
            Assert.True(Surr.Pi() > Surr.FromRational(157, 50));
        }

        [Fact]
        public void Pi_Less_Than_Omega()
        {
            Assert.True(Surr.Pi() < Surr.Omega);
        }

        [Fact]
        public void Pi_Equality()
        {
            Assert.True(Surr.Pi() == Surr.Pi());
        }

        [Fact]
        public void Pi_ToString()
        {
            Assert.Equal("π", Surr.Pi().ToString());
        }
    }
}
