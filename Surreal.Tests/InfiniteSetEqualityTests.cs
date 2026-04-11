using Xunit;

namespace Surreal.Tests
{
    /// <summary>
    /// Tests that different infinite set constructions produce equal surreal numbers.
    /// In surreal theory, the specific choice of left/right options doesn't matter —
    /// only the value they define.
    /// </summary>
    public class InfiniteSetEqualityTests
    {
        [Fact]
        public void EvenNaturals_Equals_Omega()
        {
            // {2,4,6,8,...|} should equal ω = {0,1,2,3,...|}
            // Both define the simplest number greater than all finite integers.
            var evenOmega = new Surr(EvenNaturals.Instance, null, null, null, "ω_even");
            Assert.True(evenOmega == Surr.Omega);
        }

        [Fact]
        public void NaturalsFrom2_Equals_Omega()
        {
            // {2,3,4,5,...|} should equal ω
            var omega2 = new Surr(new NaturalsFrom(2), null, null, null, "ω_from2");
            Assert.True(omega2 == Surr.Omega);
        }

        [Fact]
        public void NaturalsFrom10_Equals_Omega()
        {
            // {10,11,12,...|} should equal ω
            var omega10 = new Surr(new NaturalsFrom(10), null, null, null, "ω_from10");
            Assert.True(omega10 == Surr.Omega);
        }

        [Fact]
        public void InversePrimes_Equals_InverseOmega()
        {
            // {0 | 1/2, 1/3, 1/5, 1/7, 1/11, ...} should equal 1/ω = {0 | 1, 1/2, 1/4, ...}
            // Both define the simplest positive infinitesimal.
            var inversePrimesOmega = new Surr(
                null, new[] { Surr.Zero },
                InversePrimes.Instance, null,
                "1/ω_primes");
            Assert.True(inversePrimesOmega == Surr.InverseOmega);
        }

        [Fact]
        public void EvenNaturals_Greater_Than_All_Integers()
        {
            var evenOmega = new Surr(EvenNaturals.Instance, null, null, null);
            Assert.True(evenOmega > 0);
            Assert.True(evenOmega > 100);
            Assert.True(evenOmega > 1000);
        }

        [Fact]
        public void InversePrimes_Is_Positive_Infinitesimal()
        {
            var eps = new Surr(null, new[] { Surr.Zero }, InversePrimes.Instance, null);
            Assert.True(eps > 0);
            Assert.True(eps < Surr.Half);
            Assert.True(eps < Surr.Dyadic(1, 10)); // < 1/1024
        }

        [Fact]
        public void EvenNaturals_Greater_Than_InversePrimes()
        {
            var evenOmega = new Surr(EvenNaturals.Instance, null, null, null);
            var eps = new Surr(null, new[] { Surr.Zero }, InversePrimes.Instance, null);
            Assert.True(evenOmega > eps);
        }
    }
}
