using MoonPull.Save;
using NUnit.Framework;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class SaveServiceTests
    {
        private const int Levels = 100;
        private const int Regions = 5;

        private InMemoryStorage storage;

        [SetUp]
        public void SetUp() => storage = new InMemoryStorage();

        private SaveService NewService() => new SaveService(storage, Levels, Regions);

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            SaveService first = NewService();
            first.Load();
            first.Data.Coins = 1234;
            first.Data.LevelStars[7] = 3;
            first.Data.OwnedBoats.Add("boat_skiff");
            first.Data.Settings.MusicOn = false;
            first.Data.Ads.SessionCount = 9;
            first.SaveNow();

            SaveService second = NewService();
            second.Load();

            Assert.IsFalse(second.IsFreshInstall);
            Assert.AreEqual(1234, second.Data.Coins);
            Assert.AreEqual(3, second.Data.LevelStars[7]);
            CollectionAssert.Contains(second.Data.OwnedBoats, "boat_skiff");
            Assert.IsFalse(second.Data.Settings.MusicOn);
            Assert.AreEqual(9, second.Data.Ads.SessionCount);
        }

        [Test]
        public void FirstLaunch_IsFreshWithSafeDefaults()
        {
            SaveService service = NewService();
            service.Load();

            Assert.IsTrue(service.IsFreshInstall);
            Assert.AreEqual(0, service.Data.Coins);
            Assert.AreEqual(Levels, service.Data.LevelStars.Length);
            Assert.AreEqual(Regions, service.Data.LighthouseStages.Length);
            Assert.IsNotNull(service.Data.Settings);
        }

        [Test]
        public void CorruptPrimary_FallsBackToBackup()
        {
            SaveService service = NewService();
            service.Load();
            service.Data.Coins = 100;
            service.SaveNow();
            service.Data.Coins = 200;
            service.SaveNow();

            storage.Write(SaveService.PrimaryKey, "deadbeef|{not json");
            string reported = null;
            SaveService reloaded = NewService();
            reloaded.CorruptionDetected += message => reported = message;
            reloaded.Load();

            Assert.AreEqual(100, reloaded.Data.Coins, "Backup holds the previous good save.");
            Assert.IsNotNull(reported);
        }

        [Test]
        public void CorruptEverything_ResetsToDefaultsAndReports()
        {
            storage.Write(SaveService.PrimaryKey, "garbage");
            storage.Write(SaveService.BackupKey, "also garbage");
            bool reported = false;
            SaveService service = NewService();
            service.CorruptionDetected += _ => reported = true;
            service.Load();

            Assert.IsTrue(reported);
            Assert.IsTrue(service.IsFreshInstall);
            Assert.AreEqual(0, service.Data.Coins);
        }

        [Test]
        public void TamperedJson_FailsChecksum()
        {
            SaveService service = NewService();
            service.Load();
            service.Data.Coins = 50;
            service.SaveNow();

            string payload = storage.Read(SaveService.PrimaryKey);
            storage.Write(SaveService.PrimaryKey, payload.Replace("\"Coins\":50", "\"Coins\":999999"));
            SaveService reloaded = NewService();
            reloaded.Load();

            Assert.AreNotEqual(999999, reloaded.Data.Coins);
        }

        [Test]
        public void OldVersionWithoutNewBlocks_IsMigratedAndRepaired()
        {
            string json = "{\"Version\":0,\"Coins\":77,\"LevelStars\":[3,9,1]}";
            storage.Write(SaveService.PrimaryKey, SaveService.Checksum(json).ToString("x8") + "|" + json);
            SaveService service = NewService();
            service.Load();

            Assert.AreEqual(SaveData.CurrentVersion, service.Data.Version);
            Assert.AreEqual(77, service.Data.Coins);
            Assert.AreEqual(Levels, service.Data.LevelStars.Length);
            Assert.AreEqual(3, service.Data.LevelStars[1], "Out-of-range stars are clamped.");
            Assert.IsNotNull(service.Data.Settings);
            Assert.IsNotNull(service.Data.Ads);
        }

        [Test]
        public void NewerVersionSave_IsRefused()
        {
            string json = JsonUtility.ToJson(new SaveData { Version = SaveData.CurrentVersion + 1, Coins = 5 });
            storage.Write(SaveService.PrimaryKey, SaveService.Checksum(json).ToString("x8") + "|" + json);
            SaveService service = NewService();
            service.Load();

            Assert.IsTrue(service.IsFreshInstall);
            Assert.AreEqual(0, service.Data.Coins);
        }

        [Test]
        public void Normalize_ClampsNegativeAndMissingValues()
        {
            var data = new SaveData { Coins = -10, Keys = -2, HighestUnlockedLevel = 500, OwnedBoats = null, Settings = null };
            SaveData fixedData = SaveMigrator.Normalize(data, Levels, Regions);

            Assert.AreEqual(0, fixedData.Coins);
            Assert.AreEqual(0, fixedData.Keys);
            Assert.AreEqual(Levels - 1, fixedData.HighestUnlockedLevel);
            Assert.IsNotNull(fixedData.OwnedBoats);
            Assert.IsNotNull(fixedData.Settings);
        }
    }
}
