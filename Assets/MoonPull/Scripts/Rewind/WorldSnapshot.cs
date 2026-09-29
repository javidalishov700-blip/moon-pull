using MoonPull.Boat;
using MoonPull.Mechanics;
using MoonPull.Water;

namespace MoonPull.Rewind
{
    /// <summary>
    /// Everything needed to put the world back in time. Obstacles, weather and creatures are functions of boat X and
    /// level time, so only the kinematic core is stored (~120 bytes per sample).
    /// </summary>
    public struct WorldSnapshot
    {
        public float LevelTime;
        public MoonState Moon;
        public TideState Tide;
        public WaterState Water;
        public BoatState Boat;
        public FullMoonState FullMoon;
        public int KrakenHits;
    }
}
