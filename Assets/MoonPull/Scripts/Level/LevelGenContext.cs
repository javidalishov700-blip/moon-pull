using UnityEngine;

namespace MoonPull.Level
{
    /// <summary>World facts the generator needs (tide range, boat shape, region art counts). Built from configs at level start.</summary>
    public struct LevelGenContext
    {
        public float MinLevel;
        public float MaxLevel;
        /// <summary>Distance from waterline down to the hull bottom.</summary>
        public float HullBottomOffset;
        /// <summary>Distance from waterline up to the mast top.</summary>
        public float MastTopOffset;
        public float Draft;
        public float BaseSpeed;
        public int LowVariantCount;
        public int HighVariantCount;
        public bool HasSignature;
        public PlacementKind SignatureKind;
        /// <summary>Remote-config difficulty multiplier (1 = design default).</summary>
        public float DifficultyScale;

        public float Level(float tide01) => Mathf.LerpUnclamped(MinLevel, MaxLevel, tide01);
    }
}
