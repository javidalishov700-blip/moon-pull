using UnityEngine;

namespace MoonPull.Core
{
    /// <summary>Axis-aligned box in the XY gameplay plane. Custom collision keeps gameplay deterministic and rewindable.</summary>
    public readonly struct Box2
    {
        public readonly float MinX;
        public readonly float MinY;
        public readonly float MaxX;
        public readonly float MaxY;

        public Box2(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public static Box2 FromCenter(Vector2 center, Vector2 halfExtents) =>
            new Box2(center.x - halfExtents.x, center.y - halfExtents.y, center.x + halfExtents.x, center.y + halfExtents.y);

        public float CenterX => (MinX + MaxX) * 0.5f;

        public bool Overlaps(in Box2 other) =>
            MinX < other.MaxX && MaxX > other.MinX && MinY < other.MaxY && MaxY > other.MinY;

        public bool OverlapsX(in Box2 other) => MinX < other.MaxX && MaxX > other.MinX;

        /// <summary>Signed vertical clearance between two boxes that share X range. Negative means overlap.</summary>
        public float VerticalGap(in Box2 other)
        {
            if (other.MinY >= MaxY)
            {
                return other.MinY - MaxY;
            }

            if (MinY >= other.MaxY)
            {
                return MinY - other.MaxY;
            }

            return -Mathf.Min(MaxY - other.MinY, other.MaxY - MinY);
        }

        public Box2 Expanded(float amount) => new Box2(MinX - amount, MinY - amount, MaxX + amount, MaxY + amount);
    }
}
