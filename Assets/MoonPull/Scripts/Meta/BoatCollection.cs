using System;
using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Save;

namespace MoonPull.Meta
{
    public sealed class BoatCollection
    {
        private readonly ISaveService save;
        private readonly BoatCatalog catalog;
        private readonly Wallet wallet;

        public BoatCollection(ISaveService save, BoatCatalog catalog, Wallet wallet)
        {
            this.save = save;
            this.catalog = catalog;
            this.wallet = wallet;
        }

        public event Action<BoatDefinition> BoatUnlocked;
        public event Action<BoatDefinition> SelectionChanged;

        public BoatCatalog Catalog => catalog;

        public BoatDefinition Selected
        {
            get
            {
                BoatDefinition boat = catalog.Find(save.Data.SelectedBoatId);
                return IsOwned(boat) ? boat : catalog.Default;
            }
        }

        /// <summary>Boat lent for one level by the "Try This Boat" rewarded ad. Not persisted.</summary>
        public BoatDefinition TrialBoat { get; private set; }

        /// <summary>Boat used for the next level: the trial boat if one is active, otherwise the selection.</summary>
        public BoatDefinition BoatForNextLevel => TrialBoat != null ? TrialBoat : Selected;

        public BoatModifiers SelectedModifiers => BoatModifiers.From(Selected);

        public bool IsOwned(BoatDefinition boat) =>
            boat != null && (boat == catalog.Default || save.Data.OwnedBoats.Contains(boat.Id));

        public bool CanPurchase(BoatDefinition boat) => boat != null && !boat.IapOnly && !IsOwned(boat) && wallet.CanAfford(boat.Price);

        public bool TryPurchase(BoatDefinition boat)
        {
            if (boat == null || boat.IapOnly || IsOwned(boat) || !wallet.TrySpendCoins(boat.Price, "boat"))
            {
                return false;
            }

            Grant(boat);
            return true;
        }

        /// <summary>Unlocks without payment (IAP bundle, boss chest).</summary>
        public void Grant(BoatDefinition boat)
        {
            if (boat == null || IsOwned(boat))
            {
                return;
            }

            save.Data.OwnedBoats.Add(boat.Id);
            save.MarkDirty();
            BoatUnlocked?.Invoke(boat);
        }

        public bool Select(BoatDefinition boat)
        {
            if (!IsOwned(boat))
            {
                return false;
            }

            save.Data.SelectedBoatId = boat.Id;
            save.MarkDirty();
            SelectionChanged?.Invoke(boat);
            return true;
        }

        public void StartTrial(BoatDefinition boat) => TrialBoat = IsOwned(boat) ? null : boat;

        public void EndTrial() => TrialBoat = null;
    }
}
