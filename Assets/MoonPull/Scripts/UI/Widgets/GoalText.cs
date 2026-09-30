using MoonPull.Meta;
using MoonPull.Rescue;

namespace MoonPull.UI
{
    /// <summary>
    /// "One more night" hook: the next thing to work towards, shown on the menu and the results screen. Points at
    /// the next island and whichever requirement (people, village level, coins) is still missing.
    /// </summary>
    public static class GoalText
    {
        public static string Next(Wallet wallet)
        {
            for (int i = 0; i < TycoonState.IslandCount; i++)
            {
                if (TycoonState.Owns(i))
                {
                    continue;
                }

                string name = UiText.Get("island." + TycoonState.IslandIds[i] + ".name");
                if (VillageState.Population < TycoonState.IslandRequiredPeople(i))
                {
                    return UiText.Format("goal.people", name, VillageState.Population, TycoonState.IslandRequiredPeople(i));
                }

                if (VillageState.Level < TycoonState.IslandRequiredLevel(i))
                {
                    return UiText.Format("goal.level", name, TycoonState.IslandRequiredLevel(i));
                }

                long coins = wallet != null ? wallet.Coins : 0;
                return UiText.Format("goal.coins", name, UiText.Number(System.Math.Min(coins, TycoonState.IslandCost(i))),
                    UiText.Number(TycoonState.IslandCost(i)));
            }

            return UiText.Get("goal.done");
        }
    }
}
