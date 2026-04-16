using Xunit;
using static Surreal.Surr;

namespace Surreal.Tests
{
    public class GameOutcomeTests
    {
        [Fact]
        public void Zero_Is_Zero_Outcome()
        {
            Assert.Equal(GameOutcome.Zero, Outcome(Zero));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void Positive_Integer(long n)
        {
            Assert.Equal(GameOutcome.Positive, Outcome(new Surr(n)));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-3)]
        public void Negative_Integer(long n)
        {
            Assert.Equal(GameOutcome.Negative, Outcome(new Surr(n)));
        }

        [Fact]
        public void Star_Is_Fuzzy()
        {
            Assert.Equal(GameOutcome.Fuzzy, Outcome(Star));
        }

        [Fact]
        public void Nimber2_Is_Fuzzy()
        {
            Assert.Equal(GameOutcome.Fuzzy, Outcome(Nimber(2)));
        }

        [Fact]
        public void Up_Is_Positive()
        {
            Assert.Equal(GameOutcome.Positive, Outcome(Up));
        }

        [Fact]
        public void Down_Is_Negative()
        {
            Assert.Equal(GameOutcome.Negative, Outcome(Down));
        }

        [Fact]
        public void Half_Is_Positive()
        {
            Assert.Equal(GameOutcome.Positive, Outcome(Surr.Half));
        }

        [Fact]
        public void StarPlusStar_Is_Zero()
        {
            Assert.Equal(GameOutcome.Zero, Outcome(Star + Star));
        }

        [Fact]
        public void Hot_Game_Is_Fuzzy()
        {
            // {5 | -3} — both players want to move, first player wins
            var hot = new Surr(new[] { new Surr(5) }, new[] { new Surr(-3) });
            Assert.Equal(GameOutcome.Fuzzy, Outcome(hot));
        }

        [Fact]
        public void InverseOmega_Is_Positive()
        {
            Assert.Equal(GameOutcome.Positive, Outcome(InverseOmega));
        }

        [Fact]
        public void NimAdd_Basic()
        {
            // *1 ⊕ *2 = *3 (XOR: 1^2=3)
            Assert.True(NimAdd(Nimber(1), Nimber(2)) == Nimber(3));
        }

        [Fact]
        public void NimAdd_Self_Cancels()
        {
            // *n ⊕ *n = *0 = 0
            for (int n = 0; n <= 5; n++)
                Assert.True(NimAdd(Nimber(n), Nimber(n)) == Zero);
        }

        [Fact]
        public void NimAdd_Commutative()
        {
            for (int a = 0; a <= 7; a++)
                for (int b = 0; b <= 7; b++)
                    Assert.True(NimAdd(Nimber(a), Nimber(b)) == NimAdd(Nimber(b), Nimber(a)));
        }

        [Fact]
        public void NimAdd_Associative()
        {
            for (int a = 0; a <= 4; a++)
                for (int b = 0; b <= 4; b++)
                    for (int c = 0; c <= 4; c++)
                        Assert.True(
                            NimAdd(NimAdd(Nimber(a), Nimber(b)), Nimber(c))
                            == NimAdd(Nimber(a), NimAdd(Nimber(b), Nimber(c))));
        }
    }
}
