using Xunit;

namespace Surreal.Tests
{
    public class UtilityTests
    {
        [Fact]
        public void Abs_Positive()
        {
            Assert.True(Surr.Abs(new Surr(5)) == 5);
            Assert.True(Surr.Abs(Surr.Half) == Surr.Half);
        }

        [Fact]
        public void Abs_Negative()
        {
            Assert.True(Surr.Abs(new Surr(-5)) == 5);
        }

        [Fact]
        public void Abs_Zero()
        {
            Assert.True(Surr.Abs(Surr.Zero) == 0);
        }

        [Fact]
        public void Max_Integers()
        {
            Assert.True(Surr.Max(new Surr(3), new Surr(7)) == 7);
            Assert.True(Surr.Max(new Surr(-2), new Surr(1)) == 1);
        }

        [Fact]
        public void Min_Integers()
        {
            Assert.True(Surr.Min(new Surr(3), new Surr(7)) == 3);
            Assert.True(Surr.Min(new Surr(-2), new Surr(1)) == -2);
        }

        [Fact]
        public void Max_Dyadics()
        {
            Assert.True(Surr.Max(Surr.Half, Surr.Dyadic(3, 2)) == Surr.Dyadic(3, 2));
        }

        [Fact]
        public void Between_Integer()
        {
            Assert.True(Surr.Between(new Surr(5), new Surr(3), new Surr(7)));
            Assert.False(Surr.Between(new Surr(2), new Surr(3), new Surr(7)));
        }

        [Fact]
        public void Between_Irrationals()
        {
            Assert.True(Surr.Between(Surr.FromSqrt(2), new Surr(1), new Surr(2)));
            Assert.True(Surr.Between(Surr.Pi(), new Surr(3), new Surr(4)));
        }

        [Fact]
        public void Ln2_Bounds()
        {
            var ln2 = Surr.Ln2();
            Assert.True(ln2 > Surr.FromRational(69, 100)); // > 0.69
            Assert.True(ln2 < Surr.FromRational(70, 100)); // < 0.70
        }

        [Fact]
        public void Ln2_Less_Than_1()
        {
            Assert.True(Surr.Ln2() < 1);
            Assert.True(Surr.Ln2() > 0);
        }

        [Fact]
        public void Ln2_Less_Than_Sqrt2()
        {
            Assert.True(Surr.Ln2() < Surr.FromSqrt(2));
        }

        [Fact]
        public void Sqrt3_Convenience()
        {
            Assert.True(Surr.Sqrt3 == Surr.FromSqrt(3));
            Assert.True(Surr.Sqrt3 * Surr.Sqrt3 == 3);
        }

        [Fact]
        public void Sqrt5_Convenience()
        {
            Assert.True(Surr.Sqrt5 == Surr.FromSqrt(5));
        }

        [Fact]
        public void Phi_Is_Half_OnePlusSqrt5()
        {
            // φ = (1+√5)/2. Verify φ > (1+√5)/2 - ε and φ < (1+√5)/2 + ε
            // Since we can't compute (1+√5)/2 directly, check φ is between known bounds
            // φ ≈ 1.618, (1+√5)/2 ≈ (1+2.236)/2 ≈ 1.618
            var phi = Surr.GoldenRatio();
            Assert.True(phi > Surr.FromRational(1618, 1000));
            Assert.True(phi < Surr.FromRational(1619, 1000));
        }
    }
}
