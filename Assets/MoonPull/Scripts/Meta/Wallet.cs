using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    /// <summary>Coins and keys. The only code that changes balances, so every source and sink is visible to analytics.</summary>
    public sealed class Wallet
    {
        private readonly ISaveService save;

        public Wallet(ISaveService save)
        {
            this.save = save;
        }

        /// <summary>(amount, source) for analytics economy tracking.</summary>
        public event Action<long, string> CoinsEarned;

        /// <summary>(amount, sink) for analytics economy tracking.</summary>
        public event Action<long, string> CoinsSpent;

        public event Action<int> KeysChanged;

        public long Coins => save.Data.Coins;
        public int Keys => save.Data.Keys;

        public void AddCoins(long amount, string source)
        {
            if (amount <= 0)
            {
                return;
            }

            save.Data.Coins += amount;
            save.MarkDirty();
            CoinsEarned?.Invoke(amount, source);
            GameEvents.RaiseCoinsChanged(save.Data.Coins, amount);
        }

        public bool CanAfford(long amount) => amount >= 0 && save.Data.Coins >= amount;

        public bool TrySpendCoins(long amount, string sink)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            save.Data.Coins -= amount;
            save.MarkDirty();
            CoinsSpent?.Invoke(amount, sink);
            GameEvents.RaiseCoinsChanged(save.Data.Coins, -amount);
            return true;
        }

        public void AddKeys(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            save.Data.Keys += amount;
            save.MarkDirty();
            KeysChanged?.Invoke(save.Data.Keys);
        }

        public bool TrySpendKeys(int amount)
        {
            if (amount < 0 || save.Data.Keys < amount)
            {
                return false;
            }

            save.Data.Keys -= amount;
            save.MarkDirty();
            KeysChanged?.Invoke(save.Data.Keys);
            return true;
        }

        public void Grant(RewardEntry reward, string source)
        {
            if (reward.Type == RewardType.Keys)
            {
                AddKeys(reward.Amount);
            }
            else
            {
                AddCoins(reward.Amount, source);
            }
        }
    }
}
