using System.Linq;
using Xunit;

namespace Surreal.Tests
{
    /// <summary>Direct tests for IInfiniteSet implementations that were previously only covered indirectly.</summary>
    public class InfiniteSetDirectTests
    {
        #region NaturalNumbers
        [Fact]
        public void NaturalNumbers_HasElement_GE_ZeroYes()
        {
            Assert.True(NaturalNumbers.Instance.HasElementGreaterOrEqual(Surr.Zero));
            Assert.True(NaturalNumbers.Instance.HasElementGreaterOrEqual(new Surr(5)));
            Assert.True(NaturalNumbers.Instance.HasElementGreaterOrEqual(new Surr(1000000)));
        }

        [Fact]
        public void NaturalNumbers_HasElement_LE_IsZeroOrPositive()
        {
            Assert.True(NaturalNumbers.Instance.HasElementLessOrEqual(Surr.Zero));
            Assert.True(NaturalNumbers.Instance.HasElementLessOrEqual(new Surr(5)));
            Assert.False(NaturalNumbers.Instance.HasElementLessOrEqual(new Surr(-1)));
        }

        [Fact]
        public void NaturalNumbers_SampleElements_InOrder()
        {
            var samples = NaturalNumbers.Instance.SampleElements(5);
            Assert.Equal(5, samples.Length);
            Assert.True(samples[0] == 0);
            Assert.True(samples[4] == 4);
        }
        #endregion

        #region PositivePowersOfHalf
        [Fact]
        public void PositivePowersOfHalf_ContainsOne()
        {
            Assert.True(PositivePowersOfHalf.Instance.HasElementGreaterOrEqual(new Surr(1)));
            Assert.True(PositivePowersOfHalf.Instance.HasElementLessOrEqual(new Surr(1)));
        }

        [Fact]
        public void PositivePowersOfHalf_Decreasing()
        {
            var samples = PositivePowersOfHalf.Instance.SampleElements(5);
            Assert.Equal(5, samples.Length);
            Assert.True(samples[0] == 1);
            Assert.True(samples[1] == Surr.Half);
            Assert.True(samples[2] == Surr.Dyadic(1, 2));
        }

        [Fact]
        public void PositivePowersOfHalf_LessOrEqual_ToSmall()
        {
            // 1/2^50 ≤ ε for any positive ε down to ≈ 10^-15
            Assert.True(PositivePowersOfHalf.Instance.HasElementLessOrEqual(Surr.FromRational(1, 1000)));
        }
        #endregion

        #region ShiftedNaturals
        [Fact]
        public void ShiftedNaturals_OffsetOneThird()
        {
            // {0 + 1/3, 1 + 1/3, 2 + 1/3, ...}
            var s = new ShiftedNaturals(1, 3);
            var samples = s.SampleElements(3);
            Assert.True(samples[0] == Surr.FromRational(1, 3));
            Assert.True(samples[1] == Surr.FromRational(4, 3));
            Assert.True(samples[2] == Surr.FromRational(7, 3));
        }

        [Fact]
        public void ShiftedNaturals_HasElementsLargerThanTarget()
        {
            var s = new ShiftedNaturals(0, 1); // = naturals
            Assert.True(s.HasElementGreaterOrEqual(new Surr(100)));
        }
        #endregion

        #region OmegaMinusNaturals
        [Fact]
        public void OmegaMinusNaturals_ContainsOmega()
        {
            Assert.True(OmegaMinusNaturals.Instance.HasElementGreaterOrEqual(Surr.Omega));
            Assert.True(OmegaMinusNaturals.Instance.HasElementLessOrEqual(Surr.Omega));
        }

        [Fact]
        public void OmegaMinusNaturals_AllGreaterThanFinite()
        {
            Assert.True(OmegaMinusNaturals.Instance.HasElementGreaterOrEqual(new Surr(1000000)));
        }

        [Fact]
        public void OmegaMinusNaturals_SampleElements()
        {
            var samples = OmegaMinusNaturals.Instance.SampleElements(3);
            Assert.Equal(3, samples.Length);
            Assert.True(samples[0] == Surr.Omega);
        }
        #endregion

        #region OmegaPowersOfHalf (ω, ω/2, ω/4, ...)
        [Fact]
        public void OmegaPowersOfHalf_ContainsOmega()
        {
            var o = new OmegaPowersOfHalf();
            Assert.True(o.Get(0) == Surr.Omega);
            Assert.True(o.HasElementGreaterOrEqual(Surr.Omega));
        }

        [Fact]
        public void OmegaPowersOfHalf_HalfLessThanOmega()
        {
            var o = new OmegaPowersOfHalf();
            var half = o.Get(1); // ω/2
            Assert.True(half < Surr.Omega);
            Assert.True(half > new Surr(1000));
        }
        #endregion

        #region TransfinitePlusNaturals
        [Fact]
        public void TransfinitePlusNaturals_BaseOmega()
        {
            // {ω+0, ω+1, ω+2, ...}
            var tpn = new TransfinitePlusNaturals(Surr.Omega);
            Assert.True(tpn.HasElementLessOrEqual(Surr.Omega));  // ω is in the set
            Assert.True(tpn.HasElementGreaterOrEqual(new Surr(1000000))); // exceeds all finite
        }

        [Fact]
        public void TransfinitePlusNaturals_SampleElementsFirstIsBase()
        {
            var tpn = new TransfinitePlusNaturals(Surr.Omega);
            var samples = tpn.SampleElements(1);
            Assert.True(samples[0] == Surr.Omega);
        }
        #endregion

        #region OmegaMultiples and OmegaPowers
        [Fact]
        public void OmegaMultiples_GetValues()
        {
            Assert.True(OmegaMultiples.Instance.Get(0) == Surr.Zero);
            Assert.True(OmegaMultiples.Instance.Get(1) == Surr.Omega);
        }

        [Fact]
        public void OmegaPowers_Get1_EqualsOmega()
        {
            Assert.True(OmegaPowers.Instance.Get(1) == Surr.Omega);
            Assert.True(OmegaPowers.Instance.Get(2) == Surr.OmegaSquared);
        }

        [Fact]
        public void OmegaPowers_Increasing()
        {
            Assert.True(OmegaPowers.Instance.Get(2) < OmegaPowers.Instance.Get(3));
            Assert.True(OmegaPowers.Instance.Get(3) < OmegaPowers.Instance.Get(4));
        }
        #endregion

        #region NegatedSet
        [Fact]
        public void NegatedSet_OfNaturalNumbers_ContainsZeroAndNegativesOnly()
        {
            var neg = new NegatedSet(NaturalNumbers.Instance);
            var samples = neg.SampleElements(3);
            Assert.Equal(3, samples.Length);
            Assert.True(samples[0] == 0);
            Assert.True(samples[1] == -1);
            Assert.True(samples[2] == -2);
        }

        [Fact]
        public void NegatedSet_HasElement_LessOrEqual_Zero()
        {
            var neg = new NegatedSet(NaturalNumbers.Instance);
            Assert.True(neg.HasElementLessOrEqual(new Surr(0)));
            Assert.True(neg.HasElementLessOrEqual(new Surr(5)));   // 0 is in set and ≤ 5
        }

        [Fact]
        public void NegatedSet_HasElement_GreaterOrEqual_LargeNegative()
        {
            var neg = new NegatedSet(NaturalNumbers.Instance);
            // Any of 0, -1, -2, ... ≥ -1000000? Yes, 0 ≥ -1000000.
            Assert.True(neg.HasElementGreaterOrEqual(new Surr(-1000000)));
            Assert.False(neg.HasElementGreaterOrEqual(new Surr(1)));   // max element is 0
        }

        [Fact]
        public void NegatedSet_DoubleNegation_UnwrapsViaOmegaNegation()
        {
            // -(-ω) = ω after operator-. Negation unwraps NegatedSet.
            var negOmega = -Surr.Omega;
            var omegaAgain = -negOmega;
            Assert.True(omegaAgain == Surr.Omega);
        }
        #endregion

        #region DyadicApproxBelow / Above (via FromSqrt)
        [Fact]
        public void DyadicApproxBelow_ContainsApproxValues()
        {
            var sqrt2 = Surr.FromSqrt(2);
            var below = (DyadicApproxBelow)sqrt2.LeftInf;
            var samples = below.SampleElements(3);
            // All below √2 ≈ 1.414
            foreach (var s in samples)
                Assert.True(s < sqrt2);
        }

        [Fact]
        public void DyadicApproxAbove_ContainsApproxValues()
        {
            var sqrt2 = Surr.FromSqrt(2);
            var above = (DyadicApproxAbove)sqrt2.RightInf;
            var samples = above.SampleElements(3);
            foreach (var s in samples)
                Assert.True(s > sqrt2);
        }

        [Fact]
        public void DyadicApprox_GeneratorHasSqrtOfField()
        {
            var sqrt7 = Surr.FromSqrt(7);
            var gen = GeneratorHelper.GetGenerator(sqrt7);
            Assert.NotNull(gen);
            Assert.Equal(7L, gen.SqrtOf);
        }

        [Fact]
        public void Rational_GeneratorHasPQ()
        {
            var r = Surr.FromRational(3, 7);
            var gen = GeneratorHelper.GetGenerator(r);
            Assert.NotNull(gen);
            Assert.Equal(3L, gen.P);
            Assert.Equal(7L, gen.Q);
        }
        #endregion
    }
}
