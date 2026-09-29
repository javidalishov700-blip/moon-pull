using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    /// <summary>Each boss drops a chest: open with a key, or with the "Open Boss Chest" rewarded ad when out of keys.</summary>
    public sealed class BossChestService
    {
        private readonly ISaveService save;
        private readonly EconomyConfig config;
        private readonly Wallet wallet;
        private SeededRandom rng;

        public BossChestService(ISaveService save, EconomyConfig config, Wallet wallet, IClock clock)
        {
            this.save = save;
            this.config = config;
            this.wallet = wallet;
            rng = new SeededRandom((int)(clock.UtcNow.Ticks >> 8 & 0x7FFFFFFF));
        }

        public event Action<int> ChestOpened;

        public int Pending => save.Data.PendingBossChests;
        public int KeyCost => config.BossChestKeyCost;
        public bool CanOpenWithKey => Pending > 0 && wallet.Keys >= config.BossChestKeyCost;

        public void AddChest()
        {
            save.Data.PendingBossChests++;
            save.MarkDirty();
        }

        public int OpenWithKey()
        {
            if (!CanOpenWithKey || !wallet.TrySpendKeys(config.BossChestKeyCost))
            {
                return 0;
            }

            return Open("boss_chest_key");
        }

        /// <summary>Call only after the rewarded ad paid out.</summary>
        public int OpenWithAd() => Pending > 0 ? Open("boss_chest_ad") : 0;

        private int Open(string source)
        {
            save.Data.PendingBossChests--;
            int coins = rng.Range(config.BossChestCoins.x, config.BossChestCoins.y + 1);
            wallet.AddCoins(coins, source);
            save.MarkDirty();
            ChestOpened?.Invoke(coins);
            return coins;
        }
    }
}
