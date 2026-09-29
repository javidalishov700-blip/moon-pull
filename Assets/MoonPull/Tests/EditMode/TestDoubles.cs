using System;
using System.Collections.Generic;
using MoonPull.Core;
using MoonPull.Save;
using UnityEditor;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; set; }
        public double RealtimeSinceStartup { get; set; }

        public void Advance(TimeSpan span) => UtcNow += span;
    }

    public sealed class InMemoryStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public string Read(string key) => Values.TryGetValue(key, out string value) ? value : string.Empty;
        public void Write(string key, string value) => Values[key] = value;
        public void Delete(string key) => Values.Remove(key);
        public void Flush()
        {
        }
    }

    /// <summary>Sets private [SerializeField] values on ScriptableObjects, exactly as the inspector would.</summary>
    public static class SoTestUtil
    {
        public static T Create<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

        public static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObjectArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(UnityEngine.Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public sealed class FakeSaveService : ISaveService
    {
        public FakeSaveService(int levels = 100, int regions = 5)
        {
            Data = SaveMigrator.Normalize(new SaveData(), levels, regions);
        }

        public SaveData Data { get; }
        public bool IsFreshInstall => true;
        public int DirtyCount { get; private set; }
        public int SaveCount { get; private set; }

        public void Load()
        {
        }

        public void MarkDirty() => DirtyCount++;
        public void SaveIfDirty() => SaveCount++;
        public void SaveNow() => SaveCount++;
        public void ResetAll()
        {
        }
    }
}
