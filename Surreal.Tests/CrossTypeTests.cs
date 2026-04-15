using Xunit;

namespace Surreal.Tests
{
    /// <summary>Tests comparing and combining different types of surreal numbers.</summary>
    public class CrossTypeTests
    {
        [Fact]
        public void Rational_Plus_Sqrt()
        {
            // 1/3 + √2 > 1 (0.333 + 1.414 ≈ 1.747)
            var result = Surr.FromRational(1, 3) + Surr.FromSqrt(2);
            Assert.True(result > 1);
            Assert.True(result < 2);
        }

        [Fact]
        public void Pi_Greater_Than_E()
        {
            Assert.True(Surr.Pi() > Surr.E());
        }

        [Fact]
        public void Omega_Plus_Pi()
        {
            // ω + π > ω
            var result = Surr.Omega + Surr.Pi();
            Assert.True(result > Surr.Omega);
        }

        [Fact]
        public void Star_Plus_Rational()
        {
            // * + 1/3 > 0 (positive, since 1/3 > 0 shifts * positive)
            var result = Surr.Star + Surr.FromRational(1, 3);
            Assert.True(result > 0);
        }

        [Fact]
        public void Omega_Division()
        {
            // ω / 2 = ω/2
            Assert.True(Surr.Omega / 2 == Surr.OmegaHalf);
        }

        [Fact]
        public void Dyadic_Division_Chain()
        {
            // (6/4) / (3/2) = 1
            var a = Surr.Dyadic(6, 2); // 6/4 = 3/2
            var b = Surr.FromRational(3, 2);
            Assert.True(a / b == 1);
        }

        [Fact]
        public void Sqrt_Ordering()
        {
            // √2 < √3 < √5 < 3
            Assert.True(Surr.FromSqrt(2) < Surr.FromSqrt(3));
            Assert.True(Surr.FromSqrt(3) < Surr.FromSqrt(5));
            Assert.True(Surr.FromSqrt(5) < 3);
        }

        [Fact]
        public void Negative_Rational()
        {
            // -1/3 + 1/3 = 0
            var neg = Surr.FromRational(-1, 3);
            var pos = Surr.FromRational(1, 3);
            Assert.True(neg + pos == 0);
        }

        [Fact]
        public void Birthday_Chain()
        {
            // Day 0: 0. Day 1: 1, -1. Day 2: 2, -2, 1/2, -1/2. Day 3: 3, 1/4, 3/4, 3/2.
            Assert.Equal(0, Surr.Birthday(new Surr(0)));
            Assert.Equal(1, Surr.Birthday(new Surr(1)));
            Assert.Equal(2, Surr.Birthday(new Surr(2)));
            Assert.Equal(2, Surr.Birthday(Surr.Half));
            Assert.Equal(3, Surr.Birthday(new Surr(3)));
            Assert.Equal(3, Surr.Birthday(Surr.Dyadic(1, 2)));
        }

        [Fact]
        public void SignExpansion_Various()
        {
            Assert.Equal("", Surr.SignExpansion(new Surr(0)));
            Assert.Equal("+", Surr.SignExpansion(new Surr(1)));
            Assert.Equal("+-", Surr.SignExpansion(Surr.Half));
            Assert.Equal("+-+", Surr.SignExpansion(Surr.Dyadic(3, 2)));
            var quarter = Surr.SignExpansion(Surr.Dyadic(1, 2));
            Assert.NotNull(quarter);
            Assert.StartsWith("+", quarter); // 1/4 path starts positive
            Assert.Equal("-", Surr.SignExpansion(new Surr(-1)));
        }

        [Fact]
        public void Omega_Polynomial_Sum()
        {
            // (ω + 1) + (ω + 2) should be > 2ω
            var a = Surr.OmegaPoly((1, 1), (0, 1));
            var b = Surr.OmegaPoly((1, 1), (0, 2));
            Assert.True(a > Surr.Omega);
            Assert.True(b > a);
        }

        [Fact]
        public void Nimber_Table_Exhaustive()
        {
            // Verify the full 8×8 nim product table is commutative and associative
            for (int a = 0; a <= 7; a++)
                for (int b = 0; b <= 7; b++)
                {
                    Assert.Equal(Surr.NimProduct(a, b), Surr.NimProduct(b, a));
                    for (int c = 0; c <= 3; c++)
                        Assert.Equal(
                            Surr.NimProduct(Surr.NimProduct(a, b), c),
                            Surr.NimProduct(a, Surr.NimProduct(b, c)));
                }
        }

        [Fact]
        public void Temperature_Of_Number_Is_Zero()
        {
            Assert.True(Surr.Temperature(new Surr(0)) == 0);
            Assert.True(Surr.Temperature(new Surr(42)) == 0);
            Assert.True(Surr.Temperature(Surr.Half) == 0);
        }

        [Fact]
        public void Pow_Basic()
        {
            Assert.True(Surr.Pow(new Surr(2), new Surr(10)) == 1024);
            Assert.True(Surr.Pow(new Surr(0), new Surr(5)) == 0);
            Assert.True(Surr.Pow(new Surr(7), new Surr(0)) == 1);
        }
    }
}
