namespace MoonPull.Water
{
    public struct MoonState
    {
        public float Height01;
        public float TargetHeight01;
        public float Velocity01;
    }

    public struct TideState
    {
        public float Level;
        public float Velocity;
    }

    public struct WaterState
    {
        public float WaveTime;
        public float PulseX;
        public float PulseAmplitude;
        public float PulseAge;
    }
}
