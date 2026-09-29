using System;
using UnityEngine;

namespace MoonPull.Save
{
    public interface ISaveService
    {
        SaveData Data { get; }

        /// <summary>True when the last load found no save or had to fall back to defaults.</summary>
        bool IsFreshInstall { get; }

        void Load();

        /// <summary>Flags data as changed; written on the next <see cref="SaveIfDirty"/>.</summary>
        void MarkDirty();

        void SaveIfDirty();

        /// <summary>Writes immediately. Use after purchases and level results.</summary>
        void SaveNow();

        void ResetAll();
    }

    /// <summary>Key/value backing store. PlayerPrefs in the game, in-memory in tests.</summary>
    public interface ISaveStorage
    {
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
        void Flush();
    }

    public sealed class PlayerPrefsStorage : ISaveStorage
    {
        public string Read(string key) => PlayerPrefs.GetString(key, string.Empty);
        public void Write(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public void Flush() => PlayerPrefs.Save();
    }

    /// <summary>
    /// JSON save with a checksum and a backup slot. Each write moves the last good save to the backup, so a corrupted
    /// or truncated primary falls back to the backup, then to safe defaults. Never throws into gameplay.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        public const string PrimaryKey = "mp.save";
        public const string BackupKey = "mp.save.bak";
        private const char Separator = '|';

        private readonly ISaveStorage storage;
        private readonly int levelCount;
        private readonly int regionCount;
        private bool dirty;

        public SaveService(ISaveStorage storage, int levelCount, int regionCount)
        {
            this.storage = storage;
            this.levelCount = levelCount;
            this.regionCount = regionCount;
            Data = CreateDefault();
        }

        /// <summary>Raised when a stored save was unreadable (for Crashlytics non-fatal logging).</summary>
        public event Action<string> CorruptionDetected;

        public SaveData Data { get; private set; }
        public bool IsFreshInstall { get; private set; }

        public void Load()
        {
            if (TryRead(PrimaryKey, out SaveData primary))
            {
                Data = primary;
                IsFreshInstall = false;
                return;
            }

            if (TryRead(BackupKey, out SaveData backup))
            {
                CorruptionDetected?.Invoke("Primary save unreadable, restored backup.");
                Data = backup;
                IsFreshInstall = false;
                dirty = true;
                return;
            }

            bool hadData = !string.IsNullOrEmpty(storage.Read(PrimaryKey)) || !string.IsNullOrEmpty(storage.Read(BackupKey));
            if (hadData)
            {
                CorruptionDetected?.Invoke("All save slots unreadable, reset to defaults.");
            }

            Data = CreateDefault();
            IsFreshInstall = true;
            dirty = true;
        }

        public void MarkDirty() => dirty = true;

        public void SaveIfDirty()
        {
            if (dirty)
            {
                SaveNow();
            }
        }

        public void SaveNow()
        {
            string json = JsonUtility.ToJson(Data);
            string payload = Checksum(json).ToString("x8") + Separator + json;
            string previous = storage.Read(PrimaryKey);
            if (!string.IsNullOrEmpty(previous))
            {
                storage.Write(BackupKey, previous);
            }

            storage.Write(PrimaryKey, payload);
            storage.Flush();
            dirty = false;
        }

        public void ResetAll()
        {
            storage.Delete(PrimaryKey);
            storage.Delete(BackupKey);
            storage.Flush();
            Data = CreateDefault();
            IsFreshInstall = true;
            dirty = true;
        }

        private bool TryRead(string key, out SaveData data)
        {
            data = null;
            string payload = storage.Read(key);
            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            int separator = payload.IndexOf(Separator);
            if (separator <= 0)
            {
                return false;
            }

            string json = payload.Substring(separator + 1);
            if (!uint.TryParse(payload.Substring(0, separator), System.Globalization.NumberStyles.HexNumber, null, out uint stored)
                || stored != Checksum(json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (data == null || data.Version > SaveData.CurrentVersion)
            {
                // A newer app version wrote this save; refuse rather than silently dropping its fields.
                data = null;
                return false;
            }

            data = SaveMigrator.Migrate(data, levelCount, regionCount);
            return true;
        }

        private SaveData CreateDefault() => SaveMigrator.Normalize(new SaveData(), levelCount, regionCount);

        // FNV-1a: cheap corruption/truncation detection, not security.
        public static uint Checksum(string text)
        {
            uint hash = 2166136261;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619;
            }

            return hash;
        }
    }
}
