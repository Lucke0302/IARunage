using System.Runtime.CompilerServices;

namespace Runage.Utils
{
    /// <summary>
    /// Gerador pseudoaleatório XorShift128 implementando IRandomProvider.
    /// ZERO alocação no hot path, inteiramente baseado em operações bitwise uint.
    /// Determinístico: mesma seed → mesma sequência cross-platform.
    /// </summary>
    public sealed class XorShiftRandom : IRandomProvider
    {
        private uint x, y, z, w;

        /// <summary>
        /// Inicializa o gerador com uma seed uint.
        /// Usa SplitMix32 para expandir a seed única nos 4 estados internos (x,y,z,w).
        /// </summary>
        public XorShiftRandom(uint seed)
        {
            uint s = seed;
            x = s;
            y = SplitMix32(ref s);
            z = SplitMix32(ref s);
            w = SplitMix32(ref s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SplitMix32(ref uint state)
        {
            state += 0x9E3779B9u;
            uint z = state;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            z = z ^ (z >> 16);
            return z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint NextUInt()
        {
            uint t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));
            return w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble()
        {
            // Gera valor fracionário em [0.0, 1.0)
            return NextUInt() * (1.0 / 4294967296.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int maxValue)
        {
            if (maxValue <= 0) return 0;
            return (int)(NextUInt() % (uint)maxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int minValue, int maxValue)
        {
            if (minValue >= maxValue) return minValue;
            uint range = (uint)(maxValue - minValue);
            return minValue + (int)(NextUInt() % range);
        }
    }
}