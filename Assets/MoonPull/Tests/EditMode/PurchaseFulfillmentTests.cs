using MoonPull.Config;
using MoonPull.IAP;
using NUnit.Framework;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class PurchaseFulfillmentTests
    {
        [Test]
        public void SameTransaction_IsGrantedOnce()
        {
            IapConfig config = ScriptableObject.CreateInstance<IapConfig>();
            var save = new FakeSaveService();
            var meta = new GameObject("meta").AddComponent<Meta.MetaGame>();
            // Only the wallet is needed for coin packs; MetaGame is initialized through its public API.
            SoTestUtil.SetObject(meta, "economy", ScriptableObject.CreateInstance<EconomyConfig>());
            SoTestUtil.SetObject(meta, "generation", ScriptableObject.CreateInstance<LevelGenConfig>());
            SoTestUtil.SetObject(meta, "regions", ScriptableObject.CreateInstance<RegionCatalog>());
            SoTestUtil.SetObject(meta, "boats", ScriptableObject.CreateInstance<BoatCatalog>());
            SoTestUtil.SetObject(meta, "missions", ScriptableObject.CreateInstance<MissionCatalog>());
            meta.Initialize(save, new FakeClock(System.DateTime.UtcNow));

            bool adsRemoved = false;
            var fulfillment = new PurchaseFulfillment(config, save, meta, removed => adsRemoved = removed);
            string coinsId = config.FirstIdOf(IapProductKind.CoinPack, false);
            config.TryGet(coinsId, out IapConfig.Product pack);

            Assert.IsTrue(fulfillment.Fulfill(coinsId, "tx-1"));
            Assert.IsTrue(fulfillment.Fulfill(coinsId, "tx-1"));
            Assert.AreEqual(pack.Coins, save.Data.Coins, "Redelivered transaction must not pay twice.");

            Assert.IsTrue(fulfillment.Fulfill(config.FirstIdOf(IapProductKind.RemoveAds, false), "tx-2"));
            Assert.IsTrue(save.Data.RemoveAds);
            Assert.IsTrue(adsRemoved);

            Assert.IsFalse(fulfillment.Fulfill("unknown.product", "tx-3"));

            meta.Missions.Dispose();
            Object.DestroyImmediate(meta.gameObject);
            Object.DestroyImmediate(config);
        }
    }
}
