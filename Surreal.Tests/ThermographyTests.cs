using Xunit;

namespace Surreal.Tests
{
    public class ThermographyTests
    {
        [Fact]
        public void Number_Has_Temperature_Zero()
        {
            Assert.True(Surr.Temperature(new Surr(3)) == 0);
            Assert.True(Surr.Temperature(Surr.Half) == 0);
        }

        [Fact]
        public void Hot_Game_Temperature()
        {
            // {5 | -3}: Left can get 5, Right can get -3. Temp = (5-(-3))/2 = 4
            var hot = new Surr(new[] { new Surr(5) }, new[] { new Surr(-3) });
            Assert.True(Surr.Temperature(hot) == 4);
        }

        [Fact]
        public void Hot_Game_Mean()
        {
            // {5 | -3}: mean = (5+(-3))/2 = 1
            var hot = new Surr(new[] { new Surr(5) }, new[] { new Surr(-3) });
            Assert.True(Surr.Mean(hot) == 1);
        }

        [Fact]
        public void Tepid_Game()
        {
            // {1 | -1}: temp = (1-(-1))/2 = 1, mean = 0
            var tepid = new Surr(new[] { new Surr(1) }, new[] { new Surr(-1) });
            Assert.True(Surr.Temperature(tepid) == 1);
            Assert.True(Surr.Mean(tepid) == 0);
        }

        [Fact]
        public void Star_Temperature_Zero()
        {
            // * = {0|0}: temp = 0 (cold game)
            Assert.True(Surr.Temperature(Surr.Star) == 0);
        }

        [Fact]
        public void Star_Mean_Zero()
        {
            Assert.True(Surr.Mean(Surr.Star) == 0);
        }

        [Fact]
        public void Number_Mean_Is_Itself()
        {
            Assert.True(Surr.Mean(new Surr(7)) == 7);
            Assert.True(Surr.Mean(Surr.Half) == Surr.Half);
        }

        [Fact]
        public void Fractional_Temperature()
        {
            // {2 | 1}: temp = (2-1)/2 = 1/2, mean = 3/2
            var warm = new Surr(new[] { new Surr(2) }, new[] { new Surr(1) });
            Assert.True(Surr.Temperature(warm) == Surr.Half);
            Assert.True(Surr.Mean(warm) == Surr.FromRational(3, 2));
        }
    }
}
