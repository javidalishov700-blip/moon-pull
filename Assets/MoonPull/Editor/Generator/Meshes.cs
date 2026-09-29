using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Procedural low-poly meshes that primitives cannot give: a real boat hull with a pointed bow, a wind-filled sail
    /// and irregular faceted rocks. All flat-shaded (unshared vertices) for the stylised night-sea look.
    /// </summary>
    internal static class Meshes
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static void ResetCache() => Cache.Clear();

        /// <summary>Hull along +X: length, beam, depth. Deck at y = 0, keel below, bow sweeps up and to a point.</summary>
        public static Mesh Hull(float length, float beam, float depth, float bowRise)
        {
            string key = $"Hull_{length:0.##}_{beam:0.##}_{depth:0.##}_{bowRise:0.##}";
            return Build(key, () =>
            {
                // Cross sections from stern (0) to bow (1): half-width at deck, half-width at chine, keel depth, deck height.
                const int sections = 7;
                var rings = new List<Vector3[]>();
                for (int s = 0; s < sections; s++)
                {
                    float u = s / (float)(sections - 1);
                    float x = Mathf.Lerp(-length * 0.5f, length * 0.5f, u);
                    float taper = u < 0.55f ? 1f : Mathf.Cos((u - 0.55f) / 0.45f * Mathf.PI * 0.5f);
                    float stern = u < 0.12f ? Mathf.Lerp(0.82f, 1f, u / 0.12f) : 1f;
                    float half = beam * 0.5f * Mathf.Max(0.02f, taper) * stern;
                    float rise = bowRise * Mathf.Pow(u, 3f);
                    float deck = rise;
                    float keel = -depth * Mathf.Lerp(1f, 0.35f, Mathf.Pow(u, 2f)) + rise * 0.6f;
                    rings.Add(new[]
                    {
                        new Vector3(x, deck, half),
                        new Vector3(x, keel * 0.45f + deck * 0.1f, half * 0.92f),
                        new Vector3(x, keel, 0f),
                        new Vector3(x, keel * 0.45f + deck * 0.1f, -half * 0.92f),
                        new Vector3(x, deck, -half)
                    });
                }

                var tris = new List<Vector3>();
                for (int s = 0; s < sections - 1; s++)
                {
                    Vector3[] a = rings[s];
                    Vector3[] b = rings[s + 1];
                    for (int k = 0; k < a.Length - 1; k++)
                    {
                        Quad(tris, a[k], b[k], b[k + 1], a[k + 1]);
                    }

                    // Deck.
                    Quad(tris, a[4], b[4], b[0], a[0]);
                }

                // Transom.
                Vector3[] st = rings[0];
                tris.Add(st[0]); tris.Add(st[1]); tris.Add(st[2]);
                tris.Add(st[0]); tris.Add(st[2]); tris.Add(st[3]);
                tris.Add(st[0]); tris.Add(st[3]); tris.Add(st[4]);
                return tris;
            });
        }

        /// <summary>Triangular sail billowing towards +Z: foot along +X from the mast, luff up the mast.</summary>
        public static Mesh Sail(float height, float foot, float belly)
        {
            string key = $"Sail_{height:0.##}_{foot:0.##}_{belly:0.##}";
            return Build(key, () =>
            {
                const int rows = 5;
                var tris = new List<Vector3>();
                Vector3 P(float v, float w)
                {
                    // v: 0 foot → 1 head, w: 0 luff (mast) → 1 leech.
                    float width = foot * (1f - v);
                    float bulge = belly * Mathf.Sin(Mathf.PI * w) * (1f - v * 0.7f);
                    return new Vector3(width * w, height * v, -bulge);
                }

                for (int r = 0; r < rows; r++)
                {
                    float v0 = r / (float)rows, v1 = (r + 1) / (float)rows;
                    for (int c = 0; c < 3; c++)
                    {
                        float w0 = c / 3f, w1 = (c + 1) / 3f;
                        Quad(tris, P(v0, w0), P(v1, w0), P(v1, w1), P(v0, w1));
                    }
                }

                return tris;
            });
        }

        /// <summary>Irregular faceted boulder of unit radius (scale it per use). Different seeds give different rocks.</summary>
        public static Mesh Rock(int seed, float squash = 1f)
        {
            string key = $"Rock_{seed}_{squash:0.##}";
            return Build(key, () =>
            {
                var random = new System.Random(seed * 7919 + 17);
                float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
                var verts = new List<Vector3>
                {
                    new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                    new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                    new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
                };
                int[] faces =
                {
                    0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                    3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
                };

                // One subdivision for a rounder silhouette, then jitter every vertex (shared jitter keeps it closed).
                var mid = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mid.TryGetValue(k, out int idx))
                    {
                        return idx;
                    }

                    verts.Add((verts[a] + verts[b]) * 0.5f);
                    mid[k] = verts.Count - 1;
                    return verts.Count - 1;
                }

                var sub = new List<int>();
                for (int i = 0; i < faces.Length; i += 3)
                {
                    int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    sub.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                for (int i = 0; i < verts.Count; i++)
                {
                    float jitter = 0.72f + (float)random.NextDouble() * 0.5f;
                    Vector3 v = verts[i].normalized * jitter;
                    v.y *= squash;
                    verts[i] = v;
                }

                var tris = new List<Vector3>();
                for (int i = 0; i < sub.Count; i += 3)
                {
                    tris.Add(verts[sub[i]]);
                    tris.Add(verts[sub[i + 2]]);
                    tris.Add(verts[sub[i + 1]]);
                }

                return tris;
            });
        }

        /// <summary>A renderer-only child using <paramref name="mesh"/> with the shared flat material.</summary>
        public static GameObject Part(Mesh mesh, Transform parent, Vector3 position, Vector3 scale, Color color,
            Vector3 euler = default, float emission = 0f)
        {
            var go = new GameObject(mesh != null ? mesh.name : "Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Art.Flat(color, emission);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static void Quad(List<Vector3> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }

        private static Mesh Build(string key, System.Func<List<Vector3>> triangles)
        {
            if (Cache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            string dir = Gen.Root + "/Meshes";
            Gen.Folder(dir);
            string path = dir + "/" + key + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                List<Vector3> tris = triangles();
                var indices = new int[tris.Count];
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = i;
                }

                var created = new Mesh { name = key };
                created.SetVertices(tris);
                created.SetTriangles(indices, 0);
                created.RecalculateNormals();
                created.RecalculateBounds();
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }

            Cache[key] = mesh;
            return mesh;
        }
    }
}
