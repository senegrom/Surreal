using Xunit;

namespace Surreal.Tests
{
    public class GoldenRatioTests
    {
        [Fact]
        public void Phi_Between_1_And_2()
        {
            var phi = Surr.GoldenRatio();
            Assert.True(phi > 1);
            Assert.True(phi < 2);
        }

        [Fact]
        public void Phi_Tight_Bounds()
        {
            var phi = Surr.GoldenRatio();
            Assert.True(phi > Surr.FromRational(161, 100)); // > 1.61
            Assert.True(phi < Surr.FromRational(162, 100)); // < 1.62
        }

        [Fact]
        public void Phi_Greater_Than_Sqrt2()
        {
            Assert.True(Surr.GoldenRatio() > Surr.FromSqrt(2)); // φ ≈ 1.618 > √2 ≈ 1.414
        }

        [Fact]
        public void Phi_Less_Than_Sqrt3()
        {
            Assert.True(Surr.GoldenRatio() < Surr.FromSqrt(3)); // φ ≈ 1.618 < √3 ≈ 1.732
        }

        [Fact]
        public void Phi_Equality()
        {
            Assert.True(Surr.GoldenRatio() == Surr.GoldenRatio());
        }

        [Fact]
        public void Phi_ToString()
        {
            Assert.Equal("φ", Surr.GoldenRatio().ToString());
        }

        [Fact]
        public void Phi_Less_Than_E()
        {
            Assert.True(Surr.GoldenRatio() < Surr.E()); // φ ≈ 1.618 < e ≈ 2.718
        }

        [Fact]
        public void Phi_Ordering_Chain()
        {
            // 1 < √2 < φ < √3 < 2 < e < 3 < π
            Assert.True(new Surr(1) < Surr.FromSqrt(2));
            Assert.True(Surr.FromSqrt(2) < Surr.GoldenRatio());
            Assert.True(Surr.GoldenRatio() < Surr.FromSqrt(3));
            Assert.True(Surr.FromSqrt(3) < new Surr(2));
            Assert.True(new Surr(2) < Surr.E());
            Assert.True(Surr.E() < new Surr(3));
            Assert.True(new Surr(3) < Surr.Pi());
        }
    }
}
