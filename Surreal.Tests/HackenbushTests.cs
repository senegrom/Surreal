using Xunit;
using static Surreal.Surr;

namespace Surreal.Tests
{
    /// <summary>
    /// Tests modeling simple Hackenbush game positions as surreal numbers.
    /// Blue edges are Left moves, Red edges are Right moves.
    /// A single Blue edge = +1 (Left wins). Single Red = -1 (Right wins).
    /// A Blue-Red stack of height n has value depending on the coloring.
    /// </summary>
    public class HackenbushTests
    {
        // Single Blue edge: Left removes it, game ends. Left wins. Value = 1.
        [Fact]
        public void Single_Blue_Edge()
        {
            // {0 | } = 1
            Assert.True(new Surr(1) == 1);
            Assert.Equal(GameOutcome.Positive, Outcome(new Surr(1)));
        }

        // Single Red edge: Right removes it. Value = -1.
        [Fact]
        public void Single_Red_Edge()
        {
            Assert.Equal(GameOutcome.Negative, Outcome(new Surr(-1)));
        }

        // Blue on top of Blue: 2 moves for Left. Value = 2.
        [Fact]
        public void Two_Blue_Edges()
        {
            Assert.True(new Surr(2) == 2);
            Assert.Equal(GameOutcome.Positive, Outcome(new Surr(2)));
        }

        // Blue on top of Red: Left removes Blue (leaving Red for Right) or
        // Right removes Red (toppling Blue). Value = {0 | 0} = * (fuzzy).
        [Fact]
        public void Blue_On_Red()
        {
            // Left move: remove Blue → {Red} → value -1 for Right... simplified to {0|0} = *
            // In simple Hackenbush: Blue-Red stack = *
            Assert.Equal(GameOutcome.Fuzzy, Outcome(Star));
        }

        // Three Blue edges: value = 3
        [Fact]
        public void Three_Blue_Edges()
        {
            Assert.Equal(GameOutcome.Positive, Outcome(new Surr(3)));
        }

        // Blue edge with a branch: Left has 2 options → value = {0, 0 | } = {0|} = 1
        [Fact]
        public void Blue_Edge_With_Branch()
        {
            var game = new Surr(new[] { Zero, Zero }, new Surr[0]);
            Assert.True(game == 1);
        }

        // Hot game: both players benefit from moving → first player wins
        [Fact]
        public void Hot_Position()
        {
            // {3 | -1}: Left gets 3, Right gets -1. Temp = 2, Mean = 1.
            var game = new Surr(new[] { new Surr(3) }, new[] { new Surr(-1) });
            Assert.Equal(GameOutcome.Fuzzy, Outcome(game));
            Assert.True(Temperature(game) == 2);
            Assert.True(Mean(game) == 1);
        }

        // Tepid game: balanced hot position
        [Fact]
        public void Tepid_Position()
        {
            // {2 | -2}: Mean = 0, Temp = 2. First player wins.
            var game = new Surr(new[] { new Surr(2) }, new[] { new Surr(-2) });
            Assert.Equal(GameOutcome.Fuzzy, Outcome(game));
            Assert.True(Temperature(game) == 2);
            Assert.True(Mean(game) == 0);
        }

        // {1|-1}: a "hot" game with temperature 1. First player wins.
        [Fact]
        public void Hot_One_Minus_One()
        {
            var game = new Surr(new[] { new Surr(1) }, new[] { new Surr(-1) });
            Assert.Equal(GameOutcome.Fuzzy, Outcome(game));
            Assert.True(Temperature(game) == 1);
            Assert.True(Mean(game) == 0);
        }

        // Sum of games: Blue(1) + Red(-1) = 0
        [Fact]
        public void Sum_Of_Opposite_Games()
        {
            Assert.True(new Surr(1) + new Surr(-1) == 0);
        }

        // Nim position: * + * = 0 (second player wins by copying)
        [Fact]
        public void Nim_Position_Star_Plus_Star()
        {
            Assert.Equal(GameOutcome.Zero, Outcome(Star + Star));
        }

        // Nim position *2: first player wins
        [Fact]
        public void Nim_Position_Star2()
        {
            Assert.Equal(GameOutcome.Fuzzy, Outcome(Nimber(2)));
        }
    }
}
