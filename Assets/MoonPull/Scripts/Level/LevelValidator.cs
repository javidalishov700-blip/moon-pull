using System.Collections.Generic;

namespace MoonPull.Level
{
    /// <summary>Checks a generated plan for unfair or broken layouts. Used by EditMode tests and the debug panel.</summary>
    public static class LevelValidator
    {
        private const float Epsilon = 0.001f;

        public static bool Validate(LevelPlan plan, float startClearDistance, int moonstonesRequired, List<string> issues)
        {
            issues.Clear();
            List<LevelPlacement> placements = plan.Placements;

            for (int i = 1; i < placements.Count; i++)
            {
                if (placements[i].X < placements[i - 1].X - Epsilon)
                {
                    issues.Add($"Placements not sorted at index {i}.");
                    break;
                }
            }

            bool hasPrevious = false;
            LevelPlacement previous = default;
            TideDemand previousDemand = TideDemand.None;

            for (int i = 0; i < placements.Count; i++)
            {
                LevelPlacement p = placements[i];
                TideDemand demand = DemandOf(p);
                if (demand == TideDemand.None)
                {
                    continue;
                }

                if (p.IsObstacle && p.MinX < plan.StartX + startClearDistance - Epsilon)
                {
                    issues.Add($"Obstacle at {p.X:F1} inside the start clear zone.");
                }

                if (p.Kind == PlacementKind.LowObstacle && p.Tide01 > 1f + Epsilon && !p.Has(PlacementFlags.RequiresLaunch))
                {
                    issues.Add($"Low obstacle at {p.X:F1} needs tide {p.Tide01:F2} without a launch flag.");
                }

                if (p.Kind == PlacementKind.HighObstacle && p.Tide01 < -Epsilon)
                {
                    issues.Add($"High obstacle at {p.X:F1} needs tide below zero.");
                }

                if (hasPrevious)
                {
                    bool sameGate = previous.IsObstacle && p.IsObstacle && System.Math.Abs(previous.X - p.X) < Epsilon;
                    if (sameGate)
                    {
                        if (previous.Kind == p.Kind)
                        {
                            issues.Add($"Gate at {p.X:F1} has two obstacles of the same kind.");
                        }
                        else
                        {
                            float low = previous.Kind == PlacementKind.LowObstacle ? previous.Tide01 : p.Tide01;
                            float high = previous.Kind == PlacementKind.HighObstacle ? previous.Tide01 : p.Tide01;
                            if (high <= low)
                            {
                                issues.Add($"Gate at {p.X:F1} has no passable band.");
                            }
                        }

                        demand = TideDemand.Band;
                    }
                    else if (Conflicts(previousDemand, demand))
                    {
                        float gap = p.MinX - previous.MaxX;
                        if (gap < plan.MinSwitchDistance - Epsilon)
                        {
                            issues.Add($"Only {gap:F2} between opposite demands at {previous.X:F1} → {p.X:F1} (need {plan.MinSwitchDistance:F2}).");
                        }
                    }
                }

                hasPrevious = true;
                previous = p;
                previousDemand = demand;
            }

            if (moonstonesRequired > 0 && plan.Count(PlacementKind.Moonstone) < moonstonesRequired)
            {
                issues.Add($"Only {plan.Count(PlacementKind.Moonstone)} moonstones, need {moonstonesRequired}.");
            }

            if (placements.Count == 0 || placements[placements.Count - 1].Kind != PlacementKind.Harbor)
            {
                issues.Add("Harbor is not the last placement.");
            }

            if (plan.IsBoss && plan.Count(PlacementKind.KrakenSurface) < plan.KrakenHitsRequired)
            {
                issues.Add("Boss level has fewer Kraken surfaces than hits required.");
            }

            return issues.Count == 0;
        }

        public static TideDemand DemandOf(in LevelPlacement placement)
        {
            switch (placement.Kind)
            {
                case PlacementKind.LowObstacle:
                case PlacementKind.Shark:
                    return TideDemand.High;
                case PlacementKind.HighObstacle:
                case PlacementKind.Sandbar:
                    return TideDemand.Low;
                case PlacementKind.Dock:
                    return TideDemand.Band;
                default:
                    return TideDemand.None;
            }
        }

        private static bool Conflicts(TideDemand a, TideDemand b) =>
            a != TideDemand.None && b != TideDemand.None && (a != b || a == TideDemand.Band);
    }
}
