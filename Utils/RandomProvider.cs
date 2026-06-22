using System;
using System.Threading;

namespace Runage.Utils
{
    public sealed class ThreadLocalRandomProvider : IRandomProvider
    {
        private static readonly ThreadLocal<Random> _localRandom = new(() => new Random(Guid.NewGuid().GetHashCode()));

        public double NextDouble() => _localRandom.Value!.NextDouble();

        public int Next(int maxValue) => _localRandom.Value!.Next(maxValue);

        public int Next(int minValue, int maxValue) => _localRandom.Value!.Next(minValue, maxValue);

        internal static Random CurrentRandom => _localRandom.Value!;
    }

    public static class RandomProvider
    {
        public static IRandomProvider Default { get; } = new ThreadLocalRandomProvider();

        public static Random Current => ThreadLocalRandomProvider.CurrentRandom;
    }
}
