using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Procedural art: materials, the water mesh, sprites drawn in code and the app icon. The game ships with zero
    /// hand-made assets, so a clean checkout builds a complete, good-looking game on CI.
    /// </summary>
    internal static class Art
    {
        private static readonly Dictionary<string, Material> FlatCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static void ResetCache()
        {
            FlatCache.Clear();
            SpriteCache.Clear();
        }

        // ---------------------------------------------------------------- materials

        public static Material Flat(Color color, float emission = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGB(color) + "_" + Mathf.RoundToInt(emission * 100f);
            if (FlatCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            Material material = LoadOrCreateMaterial("Flat_" + key, "MoonPull/Flat");
            material.SetColor("_Color", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Emission", emission);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            FlatCache[key] = material;
            return material;
        }

        public static Material Water() => LoadOrCreateMaterial("Water", "MoonPull/Water");

        public static Material Sky() => LoadOrCreateMaterial("Sky", "MoonPull/Sky");

        public static Material Moon()
        {
            Material material = LoadOrCreateMaterial("Moon", "MoonPull/Moon");
            material.SetColor("_Color", new Color(0.96f, 0.95f, 1f));
            return material;
        }

        /// <summary>Unlit additive-looking material for trails, beams and sparkles (Sprites/Default is always present).</summary>
        public static Material Glow(Color color)
        {
            Material material = LoadOrCreateMaterial("Glow_" + ColorUtility.ToHtmlStringRGBA(color), "Sprites/Default");
            material.color = color;
            return material;
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string dir = Gen.Root + "/Materials";
            Gen.Folder(dir);
            string path = dir + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Gen.Error("Shader not found: " + shaderName);
                shader = Shader.Find("Unlit/Color");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- meshes

        /// <summary>Grid for the sea: <paramref name="width"/> on X, <paramref name="depth"/> on Z, starting at z = zStart.</summary>
        public static Mesh WaterMesh(float width, float depth, float cell, float zStart)
        {
            string dir = Gen.Root + "/Meshes";
            Gen.Folder(dir);
            string path = dir + "/WaterGrid.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            int nx = Mathf.CeilToInt(width / cell);
            int nz = Mathf.CeilToInt(depth / cell);
            var vertices = new Vector3[(nx + 1) * (nz + 1)];
            for (int z = 0; z <= nz; z++)
            {
                for (int x = 0; x <= nx; x++)
                {
                    vertices[z * (nx + 1) + x] = new Vector3(-width * 0.5f + x * cell, 0f, zStart + z * cell);
                }
            }

            var triangles = new int[nx * nz * 6];
            int t = 0;
            for (int z = 0; z < nz; z++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int i = z * (nx + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + nx + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + nx + 1;
                    triangles[t++] = i + nx + 2;
                }
            }

            var mesh = new Mesh { name = "WaterGrid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            // Vertices are displaced in the shader; generous bounds stop the camera from culling raised waves.
            mesh.bounds = new Bounds(new Vector3(0f, 0f, zStart + depth * 0.5f), new Vector3(width, 20f, depth));
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- sprites

        public static Sprite BuiltinUi(string name) => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + name);

        public static Sprite Rounded => BuiltinUi("UISprite.psd");
        public static Sprite Circle => BuiltinUi("Knob.psd");
        public static Sprite Square => BuiltinUi("Background.psd");

        public static Sprite Star(bool filled) => Draw(filled ? "star_full" : "star_empty", 128, (x, y) =>
        {
            float d = StarDistance(x - 0.5f, y - 0.5f, 0.46f, 0.2f);
            if (filled)
            {
                return d < 0f ? new Color(1f, 0.85f, 0.35f, 1f) : Color.clear;
            }

            return d < 0f && d > -0.05f ? new Color(1f, 1f, 1f, 0.55f) : Color.clear;
        });

        public static Sprite Coin() => Draw("coin", 128, (x, y) =>
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
            if (r > 0.46f)
            {
                return Color.clear;
            }

            Color gold = new Color(1f, 0.78f, 0.25f);
            if (r > 0.38f)
            {
                return gold * 0.85f + new Color(0, 0, 0, 0.15f);
            }

            float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(0.4f, 0.62f)) * 3f);
            return Color.Lerp(gold, Color.white, shine * 0.5f);
        });

        public static Sprite Moonstone() => Draw("moonstone", 128, (x, y) =>
        {
            float d = Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f);
            return d < 0.44f ? Color.Lerp(new Color(0.75f, 0.9f, 1f), Color.white, Mathf.Clamp01(0.5f - d)) : Color.clear;
        });

        public static Sprite Key() => Draw("key", 128, (x, y) =>
        {
            float ring = Vector2.Distance(new Vector2(x, y), new Vector2(0.32f, 0.5f));
            bool isRing = ring < 0.2f && ring > 0.1f;
            bool shaft = y > 0.46f && y < 0.54f && x > 0.45f && x < 0.9f;
            bool teeth = x > 0.72f && x < 0.8f && y > 0.36f && y < 0.5f;
            return isRing || shaft || teeth ? new Color(1f, 0.85f, 0.4f) : Color.clear;
        });

        /// <summary>Stylized pointing hand for the gesture-only tutorial.</summary>
        public static Sprite Hand() => Draw("hand", 256, (x, y) =>
        {
            bool palm = RoundedRect(x, y, 0.28f, 0.1f, 0.72f, 0.55f, 0.12f);
            bool finger = RoundedRect(x, y, 0.42f, 0.45f, 0.58f, 0.95f, 0.08f);
            bool thumb = RoundedRect(x, y, 0.16f, 0.3f, 0.36f, 0.46f, 0.08f);
            if (!(palm || finger || thumb))
            {
                return Color.clear;
            }

            bool edge = !(RoundedRect(x, y, 0.3f, 0.12f, 0.7f, 0.53f, 0.1f) || RoundedRect(x, y, 0.44f, 0.47f, 0.56f, 0.93f, 0.06f)
                          || RoundedRect(x, y, 0.18f, 0.32f, 0.34f, 0.44f, 0.06f));
            return edge ? new Color(0.1f, 0.12f, 0.25f, 1f) : Color.white;
        });

        public static Sprite WeatherIcon(string kind) => Draw("weather_" + kind, 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            switch (kind)
            {
                case "storm":
                {
                    bool cloud = Vector2.Distance(p, new Vector2(0.38f, 0.62f)) < 0.2f || Vector2.Distance(p, new Vector2(0.6f, 0.6f)) < 0.22f;
                    bool bolt = InTriangle(p, new Vector2(0.55f, 0.5f), new Vector2(0.38f, 0.22f), new Vector2(0.52f, 0.3f))
                                || InTriangle(p, new Vector2(0.5f, 0.32f), new Vector2(0.62f, 0.34f), new Vector2(0.42f, 0.05f));
                    return bolt ? new Color(1f, 0.9f, 0.3f) : cloud ? new Color(0.75f, 0.78f, 0.9f) : Color.clear;
                }
                case "fog":
                {
                    bool band = (y > 0.25f && y < 0.33f) || (y > 0.45f && y < 0.53f) || (y > 0.65f && y < 0.73f);
                    return band && x > 0.15f && x < 0.85f ? new Color(0.85f, 0.88f, 0.95f) : Color.clear;
                }
                default:
                {
                    float r = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
                    float bite = Vector2.Distance(p, new Vector2(0.62f, 0.58f));
                    if (r < 0.4f && bite > 0.33f)
                    {
                        return new Color(0.95f, 0.95f, 1f);
                    }

                    return r < 0.44f && r > 0.4f ? new Color(1f, 0.6f, 0.3f) : Color.clear;
                }
            }
        });

        public static Sprite BoatIcon(string id, Color hull, Color sail) => Draw("boat_" + id, 192, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            bool hullShape = InTriangle(p, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.35f), new Vector2(0.78f, 0.18f))
                             || InTriangle(p, new Vector2(0.1f, 0.35f), new Vector2(0.78f, 0.18f), new Vector2(0.22f, 0.18f));
            bool mast = x > 0.48f && x < 0.52f && y > 0.35f && y < 0.88f;
            bool sailShape = InTriangle(p, new Vector2(0.53f, 0.86f), new Vector2(0.53f, 0.4f), new Vector2(0.84f, 0.4f));
            bool water = y < 0.16f && y > 0.1f && x > 0.05f && x < 0.95f;
            if (hullShape) return hull;
            if (sailShape) return sail;
            if (mast) return new Color(0.35f, 0.25f, 0.2f);
            return water ? new Color(0.5f, 0.8f, 0.9f, 0.8f) : Color.clear;
        });

        /// <summary>Village building badge: a rounded coloured tile with a bold cartoon symbol and a soft outline.</summary>
        public static Sprite BuildingIcon(string id, Color tile) => Draw("building_" + id, 192, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            if (!RoundedRect(x, y, 0.04f, 0.04f, 0.96f, 0.96f, 0.22f))
            {
                return Color.clear;
            }

            Color ink = new Color(0.16f, 0.13f, 0.24f);
            Color white = new Color(1f, 0.98f, 0.93f);
            Color accent = new Color(1f, 0.83f, 0.35f);
            Color bg = Color.Lerp(tile, Color.white, Mathf.Clamp01((y - 0.5f) * 0.5f)); // gentle top light
            if (!RoundedRect(x, y, 0.09f, 0.09f, 0.91f, 0.91f, 0.18f))
            {
                return tile * 0.7f + new Color(0f, 0f, 0f, 0.3f);
            }

            switch (id)
            {
                case "shelter":
                {
                    bool roof = InTriangle(p, new Vector2(0.18f, 0.55f), new Vector2(0.82f, 0.55f), new Vector2(0.5f, 0.82f));
                    bool walls = RoundedRect(x, y, 0.27f, 0.2f, 0.73f, 0.57f, 0.03f);
                    bool door = RoundedRect(x, y, 0.43f, 0.2f, 0.57f, 0.42f, 0.05f);
                    bool window = RoundedRect(x, y, 0.3f, 0.4f, 0.4f, 0.5f, 0.02f) || RoundedRect(x, y, 0.6f, 0.4f, 0.7f, 0.5f, 0.02f);
                    bool chimney = x > 0.63f && x < 0.71f && y > 0.62f && y < 0.8f;
                    if (door) return ink;
                    if (window) return accent;
                    if (roof || chimney) return new Color(0.78f, 0.3f, 0.24f);
                    return walls ? white : bg;
                }
                case "restaurant":
                {
                    float bowl = Vector2.Distance(p, new Vector2(0.5f, 0.46f));
                    bool bowlShape = bowl < 0.3f && y < 0.46f && y > 0.2f;
                    bool rim = y > 0.43f && y < 0.49f && x > 0.18f && x < 0.82f;
                    bool food = Vector2.Distance(p, new Vector2(0.4f, 0.5f)) < 0.09f || Vector2.Distance(p, new Vector2(0.57f, 0.51f)) < 0.1f;
                    float wave = Mathf.Sin(y * 30f) * 0.03f;
                    bool steam = y > 0.6f && y < 0.84f && (Mathf.Abs(x - 0.4f - wave) < 0.03f || Mathf.Abs(x - 0.6f - wave) < 0.03f);
                    if (rim) return ink;
                    if (bowlShape) return white;
                    if (food) return accent;
                    return steam ? new Color(1f, 1f, 1f, 1f) * 0.9f + bg * 0.1f : bg;
                }
                case "workshop":
                {
                    // Hammer, tilted.
                    float c = Mathf.Cos(0.7f), sn = Mathf.Sin(0.7f);
                    Vector2 q = new Vector2((p.x - 0.5f) * c - (p.y - 0.5f) * sn, (p.x - 0.5f) * sn + (p.y - 0.5f) * c);
                    bool handle = Mathf.Abs(q.x) < 0.05f && q.y > -0.33f && q.y < 0.18f;
                    bool head = RoundedRect(q.x + 0.5f, q.y + 0.5f, 0.3f, 0.66f, 0.72f, 0.82f, 0.03f);
                    if (head) return new Color(0.75f, 0.8f, 0.9f);
                    return handle ? new Color(0.65f, 0.42f, 0.25f) : bg;
                }
                case "shipyard":
                {
                    bool hull = InTriangle(p, new Vector2(0.14f, 0.38f), new Vector2(0.86f, 0.38f), new Vector2(0.74f, 0.2f))
                                || InTriangle(p, new Vector2(0.14f, 0.38f), new Vector2(0.74f, 0.2f), new Vector2(0.26f, 0.2f));
                    bool mast = x > 0.47f && x < 0.52f && y > 0.38f && y < 0.84f;
                    bool sail = InTriangle(p, new Vector2(0.53f, 0.82f), new Vector2(0.53f, 0.43f), new Vector2(0.82f, 0.43f))
                                || InTriangle(p, new Vector2(0.45f, 0.75f), new Vector2(0.45f, 0.43f), new Vector2(0.24f, 0.43f));
                    if (hull) return new Color(0.62f, 0.38f, 0.22f);
                    if (sail) return white;
                    return mast ? ink : bg;
                }
                default: // market
                {
                    bool awning = y > 0.56f && y < 0.74f && x > 0.16f && x < 0.84f;
                    bool scallop = y > 0.5f && y <= 0.56f && Vector2.Distance(new Vector2((x - 0.16f) % 0.136f, y), new Vector2(0.068f, 0.56f)) < 0.068f && x > 0.16f && x < 0.84f;
                    bool stripe = ((int)((x - 0.16f) / 0.136f)) % 2 == 0;
                    bool posts = (Mathf.Abs(x - 0.22f) < 0.03f || Mathf.Abs(x - 0.78f) < 0.03f) && y > 0.2f && y < 0.56f;
                    bool counter = RoundedRect(x, y, 0.18f, 0.2f, 0.82f, 0.34f, 0.03f);
                    bool fruit = Vector2.Distance(p, new Vector2(0.38f, 0.39f)) < 0.06f || Vector2.Distance(p, new Vector2(0.52f, 0.4f)) < 0.06f
                                 || Vector2.Distance(p, new Vector2(0.65f, 0.39f)) < 0.06f;
                    if (awning || scallop) return stripe ? new Color(0.85f, 0.3f, 0.3f) : white;
                    if (fruit) return accent;
                    if (counter) return new Color(0.62f, 0.4f, 0.24f);
                    return posts ? ink : bg;
                }
            }
        });

        /// <summary>1024 app icon: moon over a night sea, a small boat riding a wave.</summary>
        public static Texture2D DrawIcon()
        {
            const int size = 1024;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Color skyTop = new Color(0.12f, 0.1f, 0.32f);
            Color skyBottom = new Color(0.55f, 0.4f, 0.7f);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size;
                    float y = (py + 0.5f) / size;
                    Color c = Color.Lerp(skyBottom, skyTop, y);

                    float moon = Vector2.Distance(new Vector2(x, y), new Vector2(0.64f, 0.7f));
                    if (moon < 0.3f)
                    {
                        c = Color.Lerp(c, new Color(1f, 0.97f, 0.88f), Mathf.Clamp01((0.3f - moon) * 60f) * 0.25f);
                    }

                    if (moon < 0.19f)
                    {
                        float crater = Vector2.Distance(new Vector2(x, y), new Vector2(0.6f, 0.74f));
                        c = crater < 0.04f ? new Color(0.9f, 0.88f, 0.8f) : new Color(1f, 0.98f, 0.9f);
                    }

                    float wave = 0.36f + 0.06f * Mathf.Sin(x * 9f + 0.8f) + 0.02f * Mathf.Sin(x * 23f);
                    if (y < wave)
                    {
                        float depth = Mathf.Clamp01((wave - y) / 0.36f);
                        c = Color.Lerp(new Color(0.45f, 0.85f, 0.9f), new Color(0.08f, 0.2f, 0.42f), depth);
                        if (wave - y < 0.012f)
                        {
                            c = new Color(0.95f, 0.98f, 1f);
                        }
                    }

                    Vector2 p = new Vector2(x, y);
                    float boatY = 0.36f + 0.06f * Mathf.Sin(0.3f * 9f + 0.8f) + 0.03f;
                    if (InTriangle(p, new Vector2(0.18f, boatY + 0.02f), new Vector2(0.44f, boatY + 0.02f), new Vector2(0.4f, boatY - 0.05f))
                        || InTriangle(p, new Vector2(0.18f, boatY + 0.02f), new Vector2(0.4f, boatY - 0.05f), new Vector2(0.22f, boatY - 0.05f)))
                    {
                        c = new Color(0.95f, 0.45f, 0.35f);
                    }

                    if (InTriangle(p, new Vector2(0.31f, boatY + 0.22f), new Vector2(0.31f, boatY + 0.04f), new Vector2(0.42f, boatY + 0.04f)))
                    {
                        c = new Color(1f, 0.98f, 0.92f);
                    }

                    if (x > 0.3f && x < 0.312f && y > boatY + 0.02f && y < boatY + 0.24f)
                    {
                        c = new Color(0.3f, 0.2f, 0.18f);
                    }

                    c.a = 1f;
                    pixels[py * size + px] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D EnsureIcon(string path)
        {
            Gen.Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (!File.Exists(path))
            {
                Texture2D icon = DrawIcon();
                File.WriteAllBytes(path, icon.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(icon);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer && (importer.mipmapEnabled || !importer.isReadable))
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.alphaIsTransparency = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- raster helpers

        private static Sprite Draw(string name, int size, Func<float, float, Color> shader)
        {
            if (SpriteCache.TryGetValue(name, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string dir = Gen.Root + "/Sprites";
            Gen.Folder(dir);
            string path = dir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                const int samples = 3;
                for (int py = 0; py < size; py++)
                {
                    for (int px = 0; px < size; px++)
                    {
                        Color sum = Color.clear;
                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                sum += shader((px + (sx + 0.5f) / samples) / size, (py + (sy + 0.5f) / samples) / size);
                            }
                        }

                        pixels[py * size + px] = sum / (samples * samples);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Gen.Error("Sprite failed to import: " + path);
            }

            SpriteCache[name] = sprite;
            return sprite;
        }

        private static float StarDistance(float x, float y, float outer, float inner)
        {
            float angle = Mathf.Atan2(y, x) + Mathf.PI / 2f;
            float r = Mathf.Sqrt(x * x + y * y);
            float sector = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(angle, sector) / sector;
            float edge = Mathf.Lerp(inner, outer, Mathf.Abs(a * 2f - 1f));
            return r - edge;
        }

        private static bool RoundedRect(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Clamp(x, x0 + r, x1 - r);
            float cy = Mathf.Clamp(y, y0 + r, y1 - r);
            return x >= x0 && x <= x1 && y >= y0 && y <= y1 && Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= r;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool negative = d1 < 0 || d2 < 0 || d3 < 0;
            bool positive = d1 > 0 || d2 > 0 || d3 > 0;
            return !(negative && positive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
