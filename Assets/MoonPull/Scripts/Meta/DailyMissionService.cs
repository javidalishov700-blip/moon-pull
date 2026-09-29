using System.Collections.Generic;
using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    /// <summary>Three missions per local day, picked deterministically from the date so reinstalls can't reroll them.</summary>
    public sealed class DailyMissionService : IDisposable
    {
        private readonly ISaveService save;
        private readonly MissionCatalog catalog;
        private readonly IClock clock;
        private readonly Wallet wallet;
        private readonly LevelProgress progress;
        private readonly List<MissionDefinition> eligible = new List<MissionDefinition>(16);

        public DailyMissionService(ISaveService save, MissionCatalog catalog, IClock clock, Wallet wallet, LevelProgress progress)
        {
            this.save = save;
            this.catalog = catalog;
            this.clock = clock;
            this.wallet = wallet;
            this.progress = progress;

            GameEvents.NearMiss += OnNearMiss;
            GameEvents.PassengersDelivered += OnPassengersDelivered;
            GameEvents.TreasureFound += OnTreasureFound;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.WaveLaunched += OnWaveLaunched;
            GameEvents.FullMoonStarted += OnFullMoonStarted;
        }

        /// <summary>Raised whenever progress or claims change.</summary>
        public event Action Changed;

        public IReadOnlyList<MissionProgress> Today => save.Data.Missions;

        public MissionDefinition DefinitionOf(MissionProgress mission) => catalog.Find(mission.MissionId);

        public bool IsComplete(MissionProgress mission)
        {
            MissionDefinition definition = DefinitionOf(mission);
            return definition != null && mission.Progress >= definition.Target;
        }

        public bool CanClaim(MissionProgress mission) => !mission.Claimed && IsComplete(mission);

        public int ClaimableCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < save.Data.Missions.Count; i++)
                {
                    if (CanClaim(save.Data.Missions[i]))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Rolls the day over if needed. Call on boot and on resume.</summary>
        public void Refresh()
        {
            string today = DateKeys.Today(clock);
            if (save.Data.MissionsDate == today && save.Data.Missions.Count > 0)
            {
                return;
            }

            save.Data.MissionsDate = today;
            save.Data.Missions.Clear();

            eligible.Clear();
            MissionDefinition[] all = catalog.Missions;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].MinLevelIndex <= progress.HighestUnlockedLevel)
                {
                    eligible.Add(all[i]);
                }
            }

            var rng = new SeededRandom(DateKeys.Hash(today));
            int missionTypesMask = 0;
            while (save.Data.Missions.Count < catalog.MissionsPerDay && eligible.Count > 0)
            {
                int pick = rng.Range(0, eligible.Count);
                MissionDefinition mission = eligible[pick];
                eligible.RemoveAt(pick);

                // Prefer variety: skip a second mission of the same type while alternatives remain.
                int bit = 1 << (int)mission.Type;
                if ((missionTypesMask & bit) != 0 && HasOtherType(missionTypesMask))
                {
                    continue;
                }

                missionTypesMask |= bit;
                save.Data.Missions.Add(new MissionProgress { MissionId = mission.Id });
            }

            save.MarkDirty();
            Changed?.Invoke();
        }

        public bool Claim(MissionProgress mission)
        {
            if (!CanClaim(mission))
            {
                return false;
            }

            mission.Claimed = true;
            wallet.AddCoins(DefinitionOf(mission).RewardCoins, "mission");
            save.MarkDirty();
            Changed?.Invoke();
            return true;
        }

        public void Dispose()
        {
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.PassengersDelivered -= OnPassengersDelivered;
            GameEvents.TreasureFound -= OnTreasureFound;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.WaveLaunched -= OnWaveLaunched;
            GameEvents.FullMoonStarted -= OnFullMoonStarted;
        }

        private bool HasOtherType(int usedMask)
        {
            for (int i = 0; i < eligible.Count; i++)
            {
                if ((usedMask & (1 << (int)eligible[i].Type)) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void Add(MissionType type, int amount)
        {
            bool changed = false;
            List<MissionProgress> missions = save.Data.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                MissionDefinition definition = DefinitionOf(missions[i]);
                if (definition == null || definition.Type != type || missions[i].Progress >= definition.Target)
                {
                    continue;
                }

                missions[i].Progress = Math.Min(definition.Target, missions[i].Progress + amount);
                changed = true;
            }

            if (changed)
            {
                save.MarkDirty();
                Changed?.Invoke();
            }
        }

        private void OnNearMiss(int chain, int multiplier) => Add(MissionType.NearMisses, 1);
        private void OnPassengersDelivered(int count) => Add(MissionType.DeliverPassengers, count);
        private void OnTreasureFound(int coins) => Add(MissionType.FindChests, 1);
        private void OnWaveLaunched(float strength) => Add(MissionType.WaveLaunches, 1);
        private void OnFullMoonStarted(float duration) => Add(MissionType.FullMoons, 1);

        private void OnLevelCompleted(LevelResult result)
        {
            Add(MissionType.CompleteLevels, 1);
            Add(MissionType.CollectStars, result.StarsCollected);
            if (result.Stars >= 3)
            {
                Add(MissionType.ThreeStarLevels, 1);
            }
        }
    }
}
