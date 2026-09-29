namespace MoonPull.Core
{
    /// <summary>Implicit-Euler damped spring. Unconditionally stable, so variable frame times never explode.</summary>
    public static class Spring
    {
        private const float TwoPi = 6.28318530718f;

        /// <param name="frequency">Oscillation frequency in Hz.</param>
        /// <param name="dampingRatio">1 = critically damped, &lt;1 bouncy.</param>
        public static void Step(ref float value, ref float velocity, float target, float frequency, float dampingRatio, float deltaTime)
        {
            float omega = TwoPi * frequency;
            float f = 1f + 2f * deltaTime * dampingRatio * omega;
            float oo = omega * omega;
            float hoo = deltaTime * oo;
            float hhoo = deltaTime * hoo;
            float detInv = 1f / (f + hhoo);
            float detX = f * value + deltaTime * velocity + hhoo * target;
            float detV = velocity + hoo * (target - value);
            value = detX * detInv;
            velocity = detV * detInv;
        }

        /// <summary>Frame-rate independent exponential smoothing factor.</summary>
        public static float DampFactor(float sharpness, float deltaTime) => 1f - UnityEngine.Mathf.Exp(-sharpness * deltaTime);
    }
}
