using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Shared helpers for the content generator. Every generated asset lives under <see cref="Root"/>, which is
    /// git-ignored and rebuilt on each CI build, so the repository holds only source.
    /// Serialized fields are written through SerializedObject exactly as the Inspector would. A field name that does
    /// not exist is recorded and fails the build at the end, so wiring typos can never ship silently.
    /// </summary>
    internal static class Gen
    {
        public const string Root = "Assets/MoonPull/Generated";

        private static readonly List<string> Problems = new List<string>();

        public static IReadOnlyList<string> Errors => Problems;

        public static void ResetErrors() => Problems.Clear();

        public static void Error(string message)
        {
            Problems.Add(message);
            Debug.LogError("[MoonPull] " + message);
        }

        public static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Creates (or reuses) a ScriptableObject asset with its code defaults.</summary>
        public static T So<T>(string folder, string name) where T : ScriptableObject
        {
            string dir = Root + "/" + folder;
            Folder(dir);
            string path = dir + "/" + name + ".asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        public static void Set(Object target, string field, object value)
        {
            if (target == null)
            {
                Error("Set on null target for field " + field);
                return;
            }

            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Error(target.GetType().Name + " has no serialized field '" + field + "'");
                return;
            }

            Assign(property, value, target.GetType().Name + "." + field);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets several fields in one pass: Set(target, "a", 1, "b", obj, ...).</summary>
        public static void Wire(Object target, params object[] pairs)
        {
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                Set(target, (string)pairs[i], pairs[i + 1]);
            }
        }

        public static void SetArray<T>(Object target, string field, IList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
            {
                Error(target.GetType().Name + " has no array field '" + field + "'");
                return;
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills an array of serializable structs; <paramref name="fill"/> writes each element's children.</summary>
        public static void SetStructArray(Object target, string field, int count, Action<SerializedProperty, int> fill)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
            {
                Error(target.GetType().Name + " has no array field '" + field + "'");
                return;
            }

            property.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                fill(property.GetArrayElementAtIndex(i), i);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Child(SerializedProperty parent, string field, object value)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            if (property == null)
            {
                Error("Struct has no field '" + field + "' (" + parent.propertyPath + ")");
                return;
            }

            Assign(property, value, parent.propertyPath + "." + field);
        }

        private static void Assign(SerializedProperty property, object value, string label)
        {
            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case bool b:
                    property.boolValue = b;
                    break;
                case int i:
                    property.intValue = i;
                    break;
                case float f:
                    property.floatValue = f;
                    break;
                case string s:
                    property.stringValue = s;
                    break;
                case Color c:
                    property.colorValue = c;
                    break;
                case Vector2 v2:
                    property.vector2Value = v2;
                    break;
                case Vector3 v3:
                    property.vector3Value = v3;
                    break;
                case Enum e:
                    property.intValue = Convert.ToInt32(e);
                    break;
                case int[] ints:
                    property.arraySize = ints.Length;
                    for (int k = 0; k < ints.Length; k++)
                    {
                        property.GetArrayElementAtIndex(k).intValue = ints[k];
                    }

                    break;
                default:
                    Error("Unsupported value type " + value.GetType().Name + " for " + label);
                    break;
            }
        }

        // ---------------------------------------------------------------- GameObjects

        public static GameObject Go(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go;
        }

        public static T Add<T>(GameObject go) where T : Component => go.AddComponent<T>();

        /// <summary>A collider-free primitive with a flat-shaded material.</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color,
            Vector3 euler = default, float emission = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.GetComponent<MeshRenderer>().sharedMaterial = Art.Flat(color, emission);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Saves <paramref name="root"/> as a prefab, destroys the scene copy, returns the prefab component.</summary>
        public static T SavePrefab<T>(GameObject root, string folder, string name) where T : Component
        {
            string dir = Root + "/Prefabs/" + folder;
            Folder(dir);
            string path = dir + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                Error("Could not save prefab " + path);
                return null;
            }

            return typeof(T) == typeof(Transform) ? prefab.transform as T : prefab.GetComponent<T>();
        }

        public static GameObject SavePrefabObject(GameObject root, string folder, string name)
        {
            Transform t = SavePrefab<Transform>(root, folder, name);
            return t != null ? t.gameObject : null;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color color);
            return color;
        }
    }
}
