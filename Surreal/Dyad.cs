using System;

namespace Surreal
{
    /// <summary>
    /// Dyadic rational number: Num / 2^Exp.
    /// Normalized so Num is odd (or zero). Used for fast evaluation and caching.
    /// </summary>
    internal readonly struct Dyad : IComparable<Dyad>, IEquatable<Dyad>
    {
        public readonly long Num;
        public readonly int Exp;

        public Dyad(long num, int exp)
        {
            while (exp > 0 && num != 0 && num % 2 == 0) { num /= 2; exp--; }
            Num = num; Exp = exp;
        }

        public bool Equals(Dyad other) => Num == other.Num && Exp == other.Exp;
        public override bool Equals(object obj) => obj is Dyad d && Equals(d);
        public override int GetHashCode() => HashCode.Combine(Num, Exp);

        public int CompareTo(Dyad other)
        {
            int mx = Math.Max(Exp, other.Exp);
            long a = Num << (mx - Exp), b = other.Num << (mx - other.Exp);
            return a.CompareTo(b);
        }

        public static bool operator <(Dyad a, Dyad b) => a.CompareTo(b) < 0;
        public static bool operator >(Dyad a, Dyad b) => a.CompareTo(b) > 0;

        public long CeilExclusive()
        {
            if (Exp == 0) return Num + 1;
            long den = 1L << Exp;
            return Num >= 0 ? (Num / den) + 1 : -((-Num - 1) / den);
        }

        public long FloorExclusive()
        {
            if (Exp == 0) return Num - 1;
            long den = 1L << Exp;
            return Num >= 0 ? Num / den : -((-Num + (den - 1)) / den);
        }

        public long Floor()
        {
            if (Exp == 0) return Num;
            long den = 1L << Exp;
            return Num >= 0 ? Num / den : -((-Num + (den - 1)) / den);
        }

        public static Dyad operator +(Dyad a, Dyad b)
        {
            int mx = Math.Max(a.Exp, b.Exp);
            return new Dyad((a.Num << (mx - a.Exp)) + (b.Num << (mx - b.Exp)), mx);
        }

        public static Dyad operator -(Dyad a) => new Dyad(-a.Num, a.Exp);

        public static Dyad operator *(Dyad a, Dyad b)
            => new Dyad(a.Num * b.Num, a.Exp + b.Exp);

        /// <summary>Find the simplest dyadic rational strictly between lo and hi.</summary>
        internal static Dyad SimplestBetween(Dyad? lo, Dyad? hi)
        {
            var zero = new Dyad(0, 0);
            if (lo == null && hi == null) return zero;
            if (lo == null) return hi.Value > zero ? zero : new Dyad(hi.Value.FloorExclusive(), 0);
            if (hi == null) return lo.Value < zero ? zero : new Dyad(lo.Value.CeilExclusive(), 0);
            return SimplestBounded(lo.Value, hi.Value);
        }

        private static Dyad SimplestBounded(Dyad lo, Dyad hi)
        {
            var zero = new Dyad(0, 0);
            if (lo < zero && hi > zero) return zero;

            long loInt = lo.CeilExclusive();
            long hiInt = hi.FloorExclusive();
            if (loInt <= hiInt)
            {
                if (loInt <= 0 && hiInt >= 0) return zero;
                return loInt > 0 ? new Dyad(loInt, 0) : new Dyad(hiInt, 0);
            }

            long n = lo.Floor();
            int mx = Math.Max(lo.Exp, hi.Exp);
            long loN = lo.Num << (mx - lo.Exp);
            long hiN = hi.Num << (mx - hi.Exp);
            long nScaled = n << mx;
            var newLo = new Dyad(2 * (loN - nScaled), mx);
            var newHi = new Dyad(2 * (hiN - nScaled), mx);

            var mid = SimplestBounded(newLo, newHi);
            return new Dyad(n * (1L << (mid.Exp + 1)) + mid.Num, mid.Exp + 1);
        }
    }
}
