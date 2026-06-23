using System;
using System.Threading;

namespace Runage.Utils
{
    public sealed class ThreadLocalRandomProvider : IRandomProvider
    {
        private static readonly ThreadLocal<XorShiftRandom> _localRandom =
            new(() => new XorShiftRandom((uint)Guid.NewGuid().GetHashCode()));

        public double NextDouble() => _localRandom.Value!.NextDouble();

        public int Next(int maxValue) => _localRandom.Value!.Next(maxValue);

        public int Next(int minValue, int maxValue) => _localRandom.Value!.Next(minValue, maxValue);

        internal static XorShiftRandom CurrentRandom => _localRandom.Value!;
    }

    public static class RandomProvider
    {
        public static IRandomProvider Default { get; } = new ThreadLocalRandomProvider();
    }
}
