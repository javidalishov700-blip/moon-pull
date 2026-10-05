#if MOONPULL_CAPTURE
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MoonPull.Core;
using MoonPull.Meta;
using MoonPull.Rescue;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>
    /// Capture-only playtest bot (never shipped). Starts a fresh village and plays 20 nights at 8x simulation speed
    /// with an imperfect autopilot, spending coins between nights like a greedy player and letting 20 minutes of
    /// idle time pass. Writes bot-report.txt with per-night stats, the economy curve and WARN / FAIL lines.
    /// </summary>
    public static class PlaytestBot
    {
        public const int Nights = 20;
        private const float Skill = 0.75f;
        private const int TicksPerFrame = 8;
        private const float IdleMinutesBetweenNights = 20f;

        public static readonly List<string> Errors = new List<string>();

        public static void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                && !message.Contains("FMOD") && !message.Contains("ALSA") && Errors.Count < 50)
            {
                Errors.Add(type + ": " + message.Split('\n')[0] + (string.IsNullOrEmpty(stack) ? "" : " @ " + stack.Split('\n')[0]));
            }
        }

        public static IEnumerator Run(string folder)
        {
            var meta = Object.FindFirstObjectByType<MetaGame>();
            var rescue = Object.FindFirstObjectByType<NightRescue>();
            var game = Object.FindFirstObjectByType<GameManager>();
            var report = new StringBuilder();
            var warnings = new List<string>();
            if (meta == null || rescue == null || game == null)
            {
                File.WriteAllText(Path.Combine(folder, "bot-report.txt"), "FAIL: scene objects missing\n");
                yield break;
            }

            // Fresh economy, fast headless simulation.
            VillageState.DebugReset();
            int errorsBefore = Errors.Count;
            long startCoins = meta.Wallet.Coins;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }

            Time.captureFramerate = 60;
            bool completed = false;
            System.Action<LevelResult> onDone = _ => completed = true;
            GameEvents.LevelCompleted += onDone;

            report.AppendLine("MOON PULL PLAYTEST BOT  skill=" + Skill + "  nights=" + Nights);
            report.AppendLine("night lvl  secs  dist  rescued lost lant lh rocksHit/dodged flops perf hops stuck maxSpd coins supp | pop/homes hap vLv income/h supplies coins");
            int firstPurchaseNight = -1;
            float totalSecs = 0f, totalRescued = 0f, totalHit = 0f, totalDodged = 0f, firstNightSecs = 0f;
            int totalStuck = 0;
            var purchases = new List<string>();

            for (int night = 1; night <= Nights; night++)
            {
                Time.timeScale = 1f;
                completed = false;
                meta.PlayNext();
                yield return null;
                if (game.State != GameState.Playing)
                {
                    warnings.Add($"FAIL: night {night} did not start (state {game.State})");
                    break;
                }

                rescue.AutoPilotSkill = Skill;
                int guard = 0;
                while (!completed && guard < 20000)
                {
                    for (int i = 1; i < TicksPerFrame && !completed; i++)
                    {
                        rescue.SimulationTick(1f / 60f, 0f);
                    }

                    guard++;
                    yield return null;
                }

                rescue.AutoPilotSkill = -1f;
                if (!completed)
                {
                    warnings.Add($"FAIL: night {night} never ended");
                    rescue.EndNightNow();
                    yield return null;
                }

                NightRescue.NightStats s = rescue.Stats;
                if (night == 1)
                {
                    firstNightSecs = s.Duration;
                }

                totalSecs += s.Duration;
                totalRescued += s.Rescued;
                totalHit += s.RocksHit;
                totalDodged += s.RocksDodged;
                totalStuck += s.Unsticks;
                yield return null;

                // Between nights: time passes, the treasury is collected and coins are spent.
                VillageState.DebugAdvanceClock(IdleMinutesBetweenNights);
                VillageState.Simulate();
                TycoonState.DebugAdvanceClock(IdleMinutesBetweenNights);
                TycoonState.Collect(meta.Wallet);
                int bought = SpendGreedily(meta.Wallet, night, purchases);
                if (bought > 0 && firstPurchaseNight < 0)
                {
                    firstPurchaseNight = night;
                }

                report.AppendLine(string.Format(
                    "{0,5} {1,3} {2,5:0} {3,5:0} {4,7} {5,4} {6,4} {7,2} {8,8}/{9,-6} {10,5} {11,4} {12,4} {13,5} {14,6:0.0} {15,5} {16,4} | {17,3}/{23,-3} {24,3}% {18,3} {19,8:0} {20,5:0}/{21,-3} {22}",
                    night, s.Level + 1, s.Duration, s.Distance, s.Rescued, s.PassengersLost, s.Lanterns, s.Lighthouses, s.RocksHit, s.RocksDodged,
                    s.BellyFlops, s.Perfects, s.Hops, s.Unsticks, s.MaxSpeed, s.Coins, s.Supplies,
                    VillageState.Population, VillageState.Level, TycoonState.IncomePerMinute * 60f, TycoonState.Supplies, TycoonState.SupplyCapacity,
                    meta.Wallet.Coins, VillageState.Housing, VillageState.Happiness));
            }

            GameEvents.LevelCompleted -= onDone;
            Time.captureFramerate = 0;
            foreach (Camera cam in cameras)
            {
                if (cam != null)
                {
                    cam.enabled = true;
                }
            }

            GameEvents.RaiseMenuRequested();

            // Verdicts.
            float n = Mathf.Max(1, Nights);
            float avgSecs = totalSecs / n;
            float hitRate = totalHit / Mathf.Max(1f, totalHit + totalDodged);
            report.AppendLine();
            report.AppendLine("purchases: " + (purchases.Count == 0 ? "none" : string.Join(", ", purchases)));
            report.AppendLine($"avg night {avgSecs:0.0}s, avg rescued {totalRescued / n:0.0}, rock hit rate {hitRate:P0}, unsticks {totalStuck}, first purchase after night {firstPurchaseNight}");
            report.AppendLine($"final: population {VillageState.Population}, village Lv {VillageState.Level}, islands {TycoonState.IslandsOwned}, income {TycoonState.IncomePerMinute * 60f:0}/h, coins {meta.Wallet.Coins} (start {startCoins})");

            if (avgSecs < 30f) warnings.Add($"WARN: nights are short ({avgSecs:0}s)");
            if (avgSecs > 120f) warnings.Add($"WARN: nights drag on ({avgSecs:0}s)");
            if (totalStuck > 0) warnings.Add($"WARN: boat needed {totalStuck} unstick rescues");
            if (hitRate > 0.5f) warnings.Add($"WARN: rocks too hard ({hitRate:P0} hit)");
            if (totalHit + totalDodged > 0 && hitRate < 0.1f) warnings.Add($"WARN: rocks trivial ({hitRate:P0} hit)");
            if (totalRescued / n < 2f) warnings.Add($"WARN: few rescues per night ({totalRescued / n:0.0})");
            if (firstPurchaseNight < 0 || firstPurchaseNight > 3) warnings.Add($"WARN: first purchase only after night {firstPurchaseNight}");
            if (TycoonState.IslandsOwned == 0) warnings.Add("WARN: no island bought in 20 nights");
            if (VillageState.Happiness < 50) warnings.Add($"WARN: village unhappy at the end ({VillageState.Happiness}%)");
            if (firstNightSecs > 80f) warnings.Add($"WARN: first night too long ({firstNightSecs:0}s)");
            int newErrors = Errors.Count - errorsBefore;
            if (Errors.Count > 0) warnings.Add($"FAIL: {Errors.Count} errors/exceptions in the log ({newErrors} during the bot run)");
            foreach (string e in Errors)
            {
                report.AppendLine("  " + e);
            }

            report.AppendLine(warnings.Count == 0 ? "VERDICT: OK" : "VERDICT:\n  " + string.Join("\n  ", warnings));
            File.WriteAllText(Path.Combine(folder, "bot-report.txt"), report.ToString());
            Debug.Log("[MoonPull] Playtest bot report\n" + report);
        }

        /// <summary>Buys the cheapest affordable upgrade (building, boat part or island) until nothing is affordable.</summary>
        private static int SpendGreedily(Wallet wallet, int night, List<string> log)
        {
            int count = 0;
            for (int guard = 0; guard < 30; guard++)
            {
                int bestCost = int.MaxValue;
                System.Func<bool> best = null;
                string bestName = null;
                for (int b = 0; b < VillageService.BuildingCount; b++)
                {
                    var building = (VillageBuilding)b;
                    int cost = VillageService.NextCost(building);
                    if (cost >= 0 && cost < bestCost && !VillageService.IsCapped(building) && !VillageService.BlockedByWorkers(building) && wallet.CanAfford(cost))
                    {
                        bestCost = cost;
                        best = () => VillageService.TryUpgrade(building, wallet);
                        bestName = building.ToString();
                    }
                }

                for (int p = 0; p < BoatUpgrades.PartCount; p++)
                {
                    var part = (BoatPart)p;
                    int cost = BoatUpgrades.NextCost(part);
                    if (cost >= 0 && cost < bestCost && wallet.CanAfford(cost))
                    {
                        bestCost = cost;
                        best = () => BoatUpgrades.TryUpgrade(part, wallet);
                        bestName = "Boat" + part;
                    }
                }

                for (int i = 0; i < TycoonState.IslandCount; i++)
                {
                    int island = i;
                    int cost = TycoonState.IslandCost(i);
                    if (TycoonState.CanBuyNext(i) && cost < bestCost && wallet.CanAfford(cost))
                    {
                        bestCost = cost;
                        best = () => TycoonState.TryBuyIsland(island, wallet);
                        bestName = "Island" + i;
                    }
                }

                int levelBefore = VillageState.Level;
                if (best == null || !best())
                {
                    break;
                }

                if (VillageState.Level > levelBefore)
                {
                    wallet.AddCoins(VillageState.RewardSince(levelBefore), "village_level"); // same as the Village screen
                }

                log.Add($"n{night}:{bestName}({bestCost})");
                count++;
            }

            return count;
        }
    }
}
#endif
