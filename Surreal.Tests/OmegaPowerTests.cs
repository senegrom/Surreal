using Xunit;

namespace Surreal.Tests
{
    public class OmegaPowerTests
    {
        [Fact]
        public void OmegaSquared_Greater_Than_Omega()
        {
            Assert.True(Surr.OmegaSquared > Surr.Omega);
        }

        [Fact]
        public void OmegaSquared_Greater_Than_NTimesOmega()
        {
            // ω² > n·ω for all finite n
            var w2 = Surr.OmegaSquared;
            Assert.True(w2 > OmegaMultiples.Instance.Get(2));  // > 2ω
            Assert.True(w2 > OmegaMultiples.Instance.Get(10)); // > 10ω
        }

        [Fact]
        public void OmegaSquared_ToString()
        {
            Assert.Equal("ω²", Surr.OmegaSquared.ToString());
        }

        [Fact]
        public void Omega_Times_Omega_Equals_OmegaSquared()
        {
            Assert.True(Surr.Omega * Surr.Omega == Surr.OmegaSquared);
        }

        [Fact]
        public void Omega_Times_Integer()
        {
            // ω * 3 = 3ω
            var threeOmega = Surr.Omega * new Surr(3);
            Assert.Equal("3ω", threeOmega.ToString());
            Assert.True(threeOmega > Surr.Omega);
            Assert.True(threeOmega < Surr.OmegaSquared);
        }

        [Fact]
        public void TwoOmega_Greater_Than_Omega()
        {
            var twoOmega = OmegaMultiples.Instance.Get(2);
            Assert.True(twoOmega > Surr.Omega);
            Assert.True(twoOmega > 1000000);
        }

        [Fact]
        public void NTimesOmega_Ordering()
        {
            // 2ω < 3ω < 4ω
            var w2 = OmegaMultiples.Instance.Get(2);
            var w3 = OmegaMultiples.Instance.Get(3);
            var w4 = OmegaMultiples.Instance.Get(4);
            Assert.True(w2 < w3);
            Assert.True(w3 < w4);
        }

        [Fact]
        public void OmegaToOmega_Greater_Than_All_Powers()
        {
            var ww = Surr.OmegaToOmega;
            Assert.True(ww > Surr.Omega);
            Assert.True(ww > Surr.OmegaSquared);
            Assert.True(ww > OmegaPowers.Instance.Get(3)); // > ω³
        }

        [Fact]
        public void OmegaToOmega_ToString()
        {
            Assert.Equal("ω^ω", Surr.OmegaToOmega.ToString());
        }

        [Fact]
        public void Full_Ordering_Chain()
        {
            // 100 < √ω < ω/2 < ω < 2ω < ω² < ω^ω
            Assert.True(new Surr(100) < Surr.SqrtOmega);
            Assert.True(Surr.SqrtOmega < Surr.OmegaHalf);
            Assert.True(Surr.OmegaHalf < Surr.Omega);
            Assert.True(Surr.Omega < OmegaMultiples.Instance.Get(2));
            Assert.True(OmegaMultiples.Instance.Get(2) < Surr.OmegaSquared);
            Assert.True(Surr.OmegaSquared < Surr.OmegaToOmega);
        }

        [Fact]
        public void InverseOmega_Less_Than_Everything_Positive()
        {
            // 1/ω < √2 < π < ω < ω² < ω^ω
            var eps = Surr.InverseOmega;
            Assert.True(eps < Surr.FromSqrt(2));
            Assert.True(eps < Surr.Pi());
            Assert.True(eps < Surr.Omega);
            Assert.True(eps < Surr.OmegaSquared);
        }
    }
}
