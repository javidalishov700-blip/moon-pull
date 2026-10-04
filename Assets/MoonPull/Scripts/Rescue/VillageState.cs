using System;
using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>
    /// The living village. People you rescue move in; they need food (from the Restaurant) and roofs (from the
    /// Shelter), and the Market keeps them cheerful. How well you look after them is Happiness, and a happy village
    /// grows: caring for villagers, rescuing people and building earn Village XP, and each Village Level lets every
    /// building grow one level taller and pays a coin reward. Food keeps cooking while the game is closed.
    /// </summary>
    public static class VillageState
    {
        public const string PopulationKey = NightRescue.VillageKey;
        private const string FoodKey = "mp_village_food";
        private const string XpKey = "mp_village_xp";
        private const string LevelKey = "mp_village_level";
        private const string TickKey = "mp_village_tick";
        private const string CheerKey = "mp_village_cheer";

        public const int MaxVillageLevel = 10;
        public const float FoodCap = 60f;

        public static event Action<int> LeveledUp;
        public static event Action Changed;

        public static int Population => PlayerPrefs.GetInt(PopulationKey, 0);
        public static float Food => PlayerPrefs.GetFloat(FoodKey, 8f);
        public static int Xp => PlayerPrefs.GetInt(XpKey, 0);
        public static int Level => Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 1), 1, MaxVillageLevel);

        /// <summary>Short-lived boost from caring for villagers in person (0..1), decays over a few hours.</summary>
        public static float Cheer => Mathf.Clamp01(PlayerPrefs.GetFloat(CheerKey, 0f));

        public static int XpForNextLevel => Mathf.RoundToInt(80f * Mathf.Pow(Level, 1.45f));

        /// <summary>Every building's level is capped by the village level (and by its own maximum).</summary>
        public static int BuildingLevelCap => Mathf.Min(VillageService.MaxLevel, 1 + (Level - 1) / 2);

        public static int Housing => 10 + 15 * VillageService.Level(VillageBuilding.Shelter) + TycoonState.ExtraHousing;

        /// <summary>The village takes in people up to a quarter over its homes; the rest sail on to other harbors.</summary>
        public static int Capacity => Mathf.CeilToInt(Housing * 1.25f);

        public static float FoodPerHour => 4f + 12f * VillageService.Level(VillageBuilding.Restaurant) + 3f * TycoonState.IslandsOwned + (TycoonState.Owns(0) ? 15f : 0f); // Palm Cove: fruit groves

        public static float EatingPerHour => Population * 0.12f;

        /// <summary>0..100. Fed, housed and entertained villagers are happy.</summary>
        public static int Happiness
        {
            get
            {
                if (Population == 0)
                {
                    return 70;
                }

                float fed = Food > 1f ? 1f : Mathf.Clamp01(FoodPerHour / Mathf.Max(0.1f, EatingPerHour));
                float housed = Mathf.Clamp01(Housing / (float)Population);
                float fun = 0.35f + 0.2f * VillageService.Level(VillageBuilding.Market) + 0.15f * VillageService.Level(VillageBuilding.Workshop);
                float score = 0.4f * fed + 0.35f * housed + 0.15f * Mathf.Clamp01(fun) + 0.1f + 0.2f * Cheer;
                return Mathf.Clamp(Mathf.RoundToInt(score * 100f), 0, 100);
            }
        }

        // Population milestones: a growing village pays back. Seats at 10/50/200 people, more coins at 25/100.
        public static readonly int[] PerkPeople = { 10, 25, 50, 100, 200 };
        public static readonly bool[] PerkIsSeat = { true, false, true, false, true };

        public static int PerkSeats
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PerkPeople.Length; i++) if (PerkIsSeat[i] && Population >= PerkPeople[i]) n++;
                return n;
            }
        }

        /// <summary>+10% coins at 25 people and another +15% at 100.</summary>
        public static float PerkCoinMultiplier => 1f + (Population >= 25 ? 0.1f : 0f) + (Population >= 100 ? 0.15f : 0f);

        /// <summary>Index of the next milestone, or -1 when all are reached.</summary>
        public static int NextPerk
        {
            get
            {
                for (int i = 0; i < PerkPeople.Length; i++) if (Population < PerkPeople[i]) return i;
                return -1;
            }
        }

        public static bool IsHungry => Population > 0 && Food < 1f;

        public static int Homeless => Mathf.Max(0, Population - Housing);

        /// <summary>Catch up on cooking and eating since the last visit (also covers time the game was closed).</summary>
        public static void Simulate()
        {
            long now = DateTime.UtcNow.Ticks;
            long last = long.TryParse(PlayerPrefs.GetString(TickKey, "0"), out long parsed) ? parsed : 0;
            PlayerPrefs.SetString(TickKey, now.ToString());
            if (last <= 0 || now <= last)
            {
                return;
            }

            float hours = Mathf.Min(24f, (float)TimeSpan.FromTicks(now - last).TotalHours);
            float food = Mathf.Clamp(Food + (FoodPerHour - EatingPerHour) * hours, 0f, FoodCap);
            PlayerPrefs.SetFloat(FoodKey, food);
            PlayerPrefs.SetFloat(CheerKey, Mathf.Max(0f, Cheer - hours * 0.25f));
            Changed?.Invoke();
        }

        /// <summary>Playtest bot only: pretend <paramref name="minutes"/> passed since the last simulation.</summary>
        public static void DebugAdvanceClock(float minutes)
        {
            long now = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(TickKey, (now - TimeSpan.FromMinutes(minutes).Ticks).ToString());
        }

        /// <summary>Playtest bot only: an empty village.</summary>
        public static void DebugReset()
        {
            foreach (string key in new[] { PopulationKey, FoodKey, XpKey, LevelKey, TickKey, CheerKey })
            {
                PlayerPrefs.DeleteKey(key);
            }

            for (int b = 0; b < VillageService.BuildingCount; b++)
            {
                PlayerPrefs.DeleteKey("mp_village_" + ((VillageBuilding)b).ToString().ToLowerInvariant());
            }

            for (int p = 0; p < BoatUpgrades.PartCount; p++)
            {
                PlayerPrefs.DeleteKey("mp_boat_" + ((BoatPart)p).ToString().ToLowerInvariant());
            }

            TycoonState.DebugReset();
            Changed?.Invoke();
        }

        /// <summary>Moves rescued people in (up to <see cref="Capacity"/>) and returns how many settled.</summary>
        public static int AddPeople(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int settled = Mathf.Clamp(Capacity - Population, 0, count);
            PlayerPrefs.SetInt(PopulationKey, Population + settled);
            AddXp(count * 10); // every rescue counts for the village's reputation
            return settled;
        }

        public static bool TryEat(float amount)
        {
            if (Food < amount)
            {
                return false;
            }

            PlayerPrefs.SetFloat(FoodKey, Food - amount);
            Changed?.Invoke();
            return true;
        }

        public static void AddFood(float amount)
        {
            PlayerPrefs.SetFloat(FoodKey, Mathf.Clamp(Food + amount, 0f, FoodCap));
            Changed?.Invoke();
        }

        public static void AddCheer(float amount)
        {
            PlayerPrefs.SetFloat(CheerKey, Mathf.Clamp01(Cheer + amount));
            Changed?.Invoke();
        }

        /// <summary>Adds XP (scaled by happiness) and returns how many levels were gained.</summary>
        public static int AddXp(int amount)
        {
            int gained = 0;
            int xp = Xp + Mathf.RoundToInt(amount * Mathf.Lerp(0.6f, 1.3f, Happiness / 100f));
            while (Level < MaxVillageLevel && xp >= XpForNextLevel)
            {
                xp -= XpForNextLevel;
                PlayerPrefs.SetInt(LevelKey, Level + 1);
                gained++;
            }

            PlayerPrefs.SetInt(XpKey, Level >= MaxVillageLevel ? 0 : xp);
            PlayerPrefs.Save();
            for (int i = 0; i < gained; i++)
            {
                LeveledUp?.Invoke(Level);
            }

            Changed?.Invoke();
            return gained;
        }

        /// <summary>Coins the village pays when it levels up.</summary>
        public static int LevelReward(int level) => 100 * level;
    }
}
