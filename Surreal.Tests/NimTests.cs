using Xunit;

namespace Surreal.Tests
{
    public class NimTests
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 5, 0)]
        [InlineData(1, 5, 5)]
        [InlineData(1, 1, 1)]
        public void NimProduct_Identity_And_Zero(int a, int b, int expected)
        {
            Assert.Equal(expected, Surr.NimProduct(a, b));
        }

        [Theory]
        [InlineData(2, 2, 3)]   // *2 ⊗ *2 = *3
        [InlineData(2, 3, 1)]   // *2 ⊗ *3 = *1
        [InlineData(3, 3, 2)]   // *3 ⊗ *3 = *(3⊗3) = *2 (since 3 = 2⊕1, distribute)
        [InlineData(4, 4, 6)]   // *4 ⊗ *4 = *6 (Fermat: 4=2^2, 4⊗4 = 3*4/2 = 6)
        [InlineData(2, 4, 8)]   // below Fermat boundary
        public void NimProduct_Small_Values(int a, int b, int expected)
        {
            Assert.Equal(expected, Surr.NimProduct(a, b));
        }

        [Fact]
        public void NimProduct_Commutative()
        {
            for (int a = 0; a <= 8; a++)
                for (int b = 0; b <= 8; b++)
                    Assert.Equal(Surr.NimProduct(a, b), Surr.NimProduct(b, a));
        }

        [Fact]
        public void NimProduct_Associative()
        {
            for (int a = 0; a <= 4; a++)
                for (int b = 0; b <= 4; b++)
                    for (int c = 0; c <= 4; c++)
                        Assert.Equal(
                            Surr.NimProduct(Surr.NimProduct(a, b), c),
                            Surr.NimProduct(a, Surr.NimProduct(b, c)));
        }

        [Fact]
        public void NimProduct_Distributive_Over_XOR()
        {
            // a ⊗ (b ⊕ c) = (a ⊗ b) ⊕ (a ⊗ c)
            for (int a = 0; a <= 4; a++)
                for (int b = 0; b <= 4; b++)
                    for (int c = 0; c <= 4; c++)
                        Assert.Equal(
                            Surr.NimProduct(a, b ^ c),
                            Surr.NimProduct(a, b) ^ Surr.NimProduct(a, c));
        }

        [Fact]
        public void NimMultiply_Surreal()
        {
            // *2 ⊗ *3 = *1 = *
            Assert.True(Surr.NimMultiply(Surr.Nimber(2), Surr.Nimber(3)) == Surr.Star);
        }

        [Fact]
        public void NimMultiply_Star_Times_Star()
        {
            // * ⊗ * = *1 ⊗ *1 = *1 = *
            Assert.True(Surr.NimMultiply(Surr.Star, Surr.Star) == Surr.Star);
        }

        [Fact]
        public void NimProduct_Has_Inverses()
        {
            // Every nonzero nimber has a multiplicative inverse:
            // for each a > 0, exists b such that a ⊗ b = 1
            for (int a = 1; a <= 8; a++)
            {
                bool foundInverse = false;
                for (int b = 1; b <= 15; b++)
                {
                    if (Surr.NimProduct(a, b) == 1) { foundInverse = true; break; }
                }
                Assert.True(foundInverse, $"*{a} has no nim-inverse in range");
            }
        }

        [Fact]
        public void NimProduct_Table_4x4()
        {
            // Standard nim multiplication table for {0,1,2,3}:
            //   ⊗ | 0 1 2 3
            //   0 | 0 0 0 0
            //   1 | 0 1 2 3
            //   2 | 0 2 3 1
            //   3 | 0 3 1 2
            int[,] expected = { {0,0,0,0}, {0,1,2,3}, {0,2,3,1}, {0,3,1,2} };
            for (int a = 0; a < 4; a++)
                for (int b = 0; b < 4; b++)
                    Assert.Equal(expected[a, b], Surr.NimProduct(a, b));
        }
    }
}
