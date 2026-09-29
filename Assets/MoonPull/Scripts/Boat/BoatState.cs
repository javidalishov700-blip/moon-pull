namespace MoonPull.Boat
{
    /// <summary>Complete kinematic state of the boat. A plain struct so rewind can copy it into a ring buffer.</summary>
    public struct BoatState
    {
        public float X;
        public float Y;
        public float VelocityY;
        public float Tilt;
        public float TiltVelocity;
        public bool Airborne;
        public float AirTime;
        public float GroundedTime;
        public float BoostMultiplier;
        public float BoostRemaining;
        public float InvulnerableRemaining;
        public int ShieldsLeft;
        public bool Crashed;
    }
}
