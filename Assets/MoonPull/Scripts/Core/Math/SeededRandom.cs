namespace MoonPull.Core
{
    /// <summary>
    /// Deterministic xorshift32 RNG. Value type so it can be stored in rewind snapshots and level plans
    /// without allocation, and results are identical across Mono, IL2CPP and EditMode tests.
    /// </summary>
    public struct SeededRandom
    {
        private const float UIntToFloat = 1f / 4294967296f;
        private uint state;

        public SeededRandom(int seed)
        {
            // Zero is a fixed point of xorshift, so remap it.
            state = seed == 0 ? 0x9E3779B9u : (uint)seed;
            NextUInt();
        }

        public uint State => state;

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat() => NextUInt() * UIntToFloat;

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public bool Chance(float probability01) => NextFloat() < probability01;
    }
}
