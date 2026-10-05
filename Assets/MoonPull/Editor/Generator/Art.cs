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
            Material material = LoadOrCreateMaterial("GlowAdd_" + ColorUtility.ToHtmlStringRGBA(color), "MoonPull/Glow");
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
        /// <summary>Crisp anti-aliased disc (256 px): stays sharp when scaled up for wheels, moons and badges.</summary>
        public static Sprite Circle => Draw("ui_disc", 256, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
            return d < 0.49f ? Color.white : Color.clear;
        });
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

        /// <summary>Moon Pull coin: thick gold with a dark rim, an embossed crescent moon and a shine.</summary>
        public static Sprite Coin() => Draw("coin_v2", 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float r = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
            if (r > 0.47f) return Color.clear;
            Color outline = new Color(0.55f, 0.3f, 0.05f);
            Color rim = new Color(0.98f, 0.66f, 0.12f);
            Color face = new Color(1f, 0.8f, 0.2f);
            if (r > 0.435f) return outline;
            if (r > 0.36f) return y > 0.5f ? Color.Lerp(rim, Color.white, 0.25f) : rim;
            // Crescent moon emboss.
            bool moon = Vector2.Distance(p, new Vector2(0.47f, 0.5f)) < 0.2f && Vector2.Distance(p, new Vector2(0.56f, 0.56f)) > 0.17f;
            bool moonShadow = Vector2.Distance(p, new Vector2(0.48f, 0.48f)) < 0.2f && Vector2.Distance(p, new Vector2(0.57f, 0.54f)) > 0.17f;
            if (moon) return new Color(1f, 0.95f, 0.65f);
            if (moonShadow) return new Color(0.85f, 0.55f, 0.08f);
            float shine = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0.36f, 0.66f)) * 5f);
            return Color.Lerp(face, Color.white, shine * 0.6f);
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

        public static Sprite BoatIcon(string id, Color hull, Color sail) => Draw("boat3d_" + id, 256, (x, y) =>
        {
            // Side view matching the 3D boats: curved hull with plank lines and a curled prow, mast and yard, a billowing
            // striped square sail, round shields on the rail, outlined like the rest of the UI and shaded from above.
            Color stripe = Color.Lerp(sail, hull, 0.55f);
            if (id == "dinghy") { stripe = new Color(0.78f, 0.22f, 0.18f); hull = new Color(0.55f, 0.36f, 0.22f); }
            Color wood = new Color(0.36f, 0.25f, 0.18f);
            Color outline = new Color(0.08f, 0.06f, 0.18f);
            Func<float, float, int> shape = (u, v) =>
            {
                // 1 hull, 2 sail, 3 mast/yard, 4 prow
                float hullTop = 0.36f, keel = 0.17f + 0.07f * Mathf.Pow((u - 0.5f) / 0.42f, 2f);
                if (u > 0.08f && u < 0.92f && v < hullTop + 0.04f * Mathf.Pow((u - 0.5f) / 0.42f, 2f) && v > keel) return 1;
                if (Vector2.Distance(new Vector2(u, v), new Vector2(0.88f, 0.47f)) < 0.05f || (u > 0.84f && u < 0.9f && v > 0.34f && v < 0.47f)) return 4;
                if (u > 0.485f && u < 0.515f && v > 0.34f && v < 0.9f) return 3;
                if (v > 0.82f && v < 0.85f && u > 0.26f && u < 0.74f) return 3;
                float bulge = 0.04f * Mathf.Sin(Mathf.Clamp01((v - 0.45f) / 0.37f) * Mathf.PI);
                if (v > 0.45f && v < 0.82f && u > 0.28f - bulge && u < 0.72f + bulge) return 2;
                return 0;
            };

            int s0 = shape(x, y);
            if (s0 == 0)
            {
                const float o = 0.018f;
                bool edge = shape(x + o, y) != 0 || shape(x - o, y) != 0 || shape(x, y + o) != 0 || shape(x, y - o) != 0;
                if (edge) return outline;
                if (y > 0.1f && y < 0.15f && x > 0.06f && x < 0.94f) return new Color(0.45f, 0.78f, 0.9f, 0.85f); // water line
                return Color.clear;
            }

            Color c;
            switch (s0)
            {
                case 1:
                    c = hull * Mathf.Lerp(0.75f, 1.1f, (y - 0.17f) / 0.2f);
                    if (Mathf.Abs(y - 0.27f) < 0.006f || Mathf.Abs(y - 0.31f) < 0.006f) c *= 0.75f; // planks
                    for (int k = 0; k < 3; k++)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(0.3f + k * 0.2f, 0.34f)) < 0.04f) c = k % 2 == 0 ? new Color(0.91f, 0.75f, 0.42f) : stripe;
                    }

                    break;
                case 2:
                    int band = Mathf.FloorToInt((x - 0.28f) / 0.0629f);
                    c = (band % 2 == 0 ? stripe : sail) * Mathf.Lerp(0.85f, 1.05f, (y - 0.45f) / 0.37f);
                    break;
                case 4:
                    c = hull * 0.95f;
                    break;
                default:
                    c = wood;
                    break;
            }

            c.a = 1f;
            return c;
        });

        // ---------------------------------------------------------------- casual UI kit (9-sliced, tinted by Image.color)

        /// <summary>Signed distance to a rounded rectangle (negative inside), in the same units as the inputs.</summary>
        private static float RoundedBox(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            float hx = (x1 - x0) * 0.5f - r, hy = (y1 - y0) * 0.5f - r;
            float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static Color Shade(float v, float a = 1f) => new Color(v, v, v, a);

        /// <summary>Premium button: thin dark outline, a slim darker base edge, a soft vertical gradient and a fine top sheen.</summary>
        public static Sprite ChunkyButton => Draw("ui_button_v2", 128, (x, y) =>
        {
            float d = RoundedBox(x, y, 0.02f, 0.02f, 0.98f, 0.98f, 0.24f);
            if (d > 0f) return Color.clear;
            if (d > -0.02f) return Shade(0.25f, 0.9f);                              // outline
            float face = RoundedBox(x, y, 0.04f, 0.1f, 0.96f, 0.96f, 0.21f);
            if (face > 0f) return Shade(0.55f);                                     // base edge
            if (face > -0.02f && y > 0.6f) return Shade(1f);                        // top sheen line
            return Shade(Mathf.Lerp(0.72f, 0.95f, Mathf.SmoothStep(0.1f, 0.95f, y))); // body gradient
        }, new Vector4(40f, 40f, 40f, 40f));

        /// <summary>Button rim: a soft rounded frame with a faint dark outline, tinted cream / light by the Image.</summary>
        public static Sprite ButtonRim => Draw("ui_button_rim", 128, (x, y) =>
        {
            float d = RoundedBox(x, y, 0.02f, 0.02f, 0.98f, 0.98f, 0.3f);
            if (d > 0f) return Color.clear;
            if (d > -0.018f) return Shade(0.35f, 0.85f);
            return Shade(Mathf.Lerp(0.82f, 1f, Mathf.SmoothStep(0.05f, 0.4f, y)));
        }, new Vector4(44f, 44f, 44f, 44f));

        /// <summary>Button face: vivid body, glossy upper half, a deeper lip along the bottom.</summary>
        public static Sprite ButtonFace => Draw("ui_button_face", 128, (x, y) =>
        {
            float d = RoundedBox(x, y, 0.01f, 0.01f, 0.99f, 0.99f, 0.26f);
            if (d > 0f) return Color.clear;
            if (y < 0.14f) return Shade(0.7f);                                   // bottom lip
            float gloss = RoundedBox(x, y, 0.07f, 0.56f, 0.93f, 0.93f, 0.2f);
            if (gloss < 0f) return Shade(1f);                                    // gloss
            return Shade(Mathf.Lerp(0.86f, 0.94f, y));
        }, new Vector4(40f, 40f, 40f, 40f));

        /// <summary>Glass panel: a light hairline rim over a slightly darker body with a gentle top glow.</summary>
        public static Sprite PanelSprite => Draw("ui_panel_v2", 128, (x, y) =>
        {
            float d = RoundedBox(x, y, 0.02f, 0.02f, 0.98f, 0.98f, 0.22f);
            if (d > 0f) return Color.clear;
            if (d > -0.015f) return new Color(1f, 1f, 1f, 0.9f);
            if (d > -0.03f) return Shade(0.55f, 0.97f);
            return Shade(Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0.5f, 1f, y)), 0.96f);
        }, new Vector4(36f, 36f, 36f, 36f));

        /// <summary>Capsule chip for the top bar (coins, stars, level).</summary>
        public static Sprite Pill => Draw("ui_pill", 128, (x, y) =>
        {
            float d = RoundedBox(x, y, 0.02f, 0.02f, 0.98f, 0.98f, 0.48f);
            if (d > 0f) return Color.clear;
            if (d > -0.05f) return Shade(0.25f);
            return y > 0.62f && d < -0.12f ? Shade(1f) : Shade(0.88f);
        }, new Vector4(60f, 60f, 60f, 60f));

        /// <summary>Map pin: a teardrop with a dark outline and a light disc for the icon (tinted by the renderer).</summary>
        public static Sprite MapPin => Draw("ui_pin", 128, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            Func<float, float, bool> inPin = (u, v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.62f)) < 0.34f
                || InTriangle(new Vector2(u, v), new Vector2(0.24f, 0.5f), new Vector2(0.76f, 0.5f), new Vector2(0.5f, 0.05f));
            bool inside = inPin(x, y);
            bool rim = !inside && (inPin(x + 0.03f, y) || inPin(x - 0.03f, y) || inPin(x, y + 0.03f) || inPin(x, y - 0.03f));
            if (rim) return new Color(0.1f, 0.07f, 0.28f, 1f);
            if (!inside) return Color.clear;
            if (Vector2.Distance(p, new Vector2(0.5f, 0.64f)) < 0.24f) return new Color(1f, 1f, 1f, 1f) * 0.98f + new Color(0, 0, 0, 0.02f);
            return new Color(0.85f, 0.85f, 0.85f) + (y > 0.7f ? new Color(0.15f, 0.15f, 0.15f, 0f) : Color.clear);
        });

        /// <summary>White sticker glyphs for the menu tiles, with a dark outline.</summary>
        public static Sprite MenuIcon(string kind) => Draw("menu3d_" + kind, 192, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            Func<float, float, bool> shape;
            switch (kind)
            {
                case "bag":
                    shape = (u, v) => RoundedRect(u, v, 0.2f, 0.12f, 0.8f, 0.66f, 0.1f)
                                      || (Mathf.Abs(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.66f)) - 0.17f) < 0.045f && v > 0.66f);
                    break;
                case "house":
                    shape = (u, v) => RoundedRect(u, v, 0.24f, 0.12f, 0.76f, 0.56f, 0.06f)
                                      || InTriangle(new Vector2(u, v), new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.52f), new Vector2(0.5f, 0.88f));
                    break;
                case "list":
                    shape = (u, v) => RoundedRect(u, v, 0.2f, 0.1f, 0.8f, 0.9f, 0.08f);
                    break;
                case "gear":
                    shape = (u, v) =>
                    {
                        float r = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                        float a = Mathf.Atan2(v - 0.5f, u - 0.5f);
                        float teeth = Mathf.Cos(a * 8f) > 0.3f ? 0.4f : 0.32f;
                        return r < teeth && r > 0.13f;
                    };
                    break;
                case "trophy":
                    shape = (u, v) =>
                    {
                        bool cup = v > 0.42f && v < 0.88f && Mathf.Abs(u - 0.5f) < 0.26f - (0.88f - v) * 0.25f + 0.08f;
                        bool handles = Mathf.Abs(Vector2.Distance(new Vector2(Mathf.Abs(u - 0.5f), v), new Vector2(0.3f, 0.7f)) - 0.1f) < 0.04f;
                        bool stem = Mathf.Abs(u - 0.5f) < 0.06f && v > 0.24f && v <= 0.42f;
                        bool foot = RoundedRect(u, v, 0.28f, 0.1f, 0.72f, 0.24f, 0.04f);
                        return cup || handles || stem || foot;
                    };
                    break;
                case "plus":
                    shape = (u, v) => (Mathf.Abs(u - 0.5f) < 0.1f && Mathf.Abs(v - 0.5f) < 0.34f) || (Mathf.Abs(v - 0.5f) < 0.1f && Mathf.Abs(u - 0.5f) < 0.34f);
                    break;
                case "person":
                    shape = (u, v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.7f)) < 0.16f
                                      || RoundedRect(u, v, 0.26f, 0.1f, 0.74f, 0.5f, 0.16f);
                    break;
                default: // wheel
                    shape = (u, v) => Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) < 0.4f;
                    break;
            }

            // Chunky 3D sticker: a dark outline, an extruded side below it, a soft drop shadow, and on the face a
            // top-lit gradient with a glossy upper edge and shaded lower edge.
            const float o = 0.04f, ex = 0.05f;
            bool inside = shape(x, y);
            bool rim = !inside && (shape(x + o, y) || shape(x - o, y) || shape(x, y + o) || shape(x, y - o)
                                   || shape(x + o * 0.7f, y + o * 0.7f) || shape(x - o * 0.7f, y - o * 0.7f) || shape(x + o * 0.7f, y - o * 0.7f) || shape(x - o * 0.7f, y + o * 0.7f));
            if (!inside && !rim)
            {
                // Extrusion (the icon's thickness) then a blurred shadow.
                if (shape(x, y + ex) || shape(x + o, y + ex) || shape(x - o, y + ex)) return new Color(0.06f, 0.04f, 0.18f, 1f);
                float sh = 0f;
                for (int k = 1; k <= 3; k++) if (shape(x - 0.02f * k, y + ex + 0.025f * k)) sh += 0.14f;
                return new Color(0f, 0f, 0.05f, sh);
            }

            if (rim) return new Color(0.1f, 0.07f, 0.28f, 1f);
            Color face = FaceColor(kind, x, y, p);
            float light = Mathf.Lerp(0.78f, 1.12f, y);                      // lit from above
            bool topEdge = !shape(x, y + 0.045f);
            bool lowEdge = !shape(x, y - 0.05f);
            Color c = face * light;
            if (lowEdge) c *= 0.72f;
            if (topEdge) c = Color.Lerp(c, Color.white, 0.45f);
            // Glossy highlight streak across the upper face.
            if (shape(x, y + 0.09f) && !shape(x, y + 0.14f) && x < 0.62f) c = Color.Lerp(c, Color.white, 0.3f);
            c.a = 1f;
            return c;
        });

        private static Color FaceColor(string kind, float x, float y, Vector2 p)
        {

            Color white = new Color(0.95f, 0.77f, 0.36f);
            Color ink = new Color(0.08f, 0.1f, 0.24f);
            switch (kind)
            {
                case "bag":
                    return Vector2.Distance(p, new Vector2(0.5f, 0.38f)) < 0.09f ? new Color(1f, 0.78f, 0.2f) : white;
                case "person":
                    return white;
                case "trophy":
                    return new Color(1f, 0.8f, 0.22f);
                case "gear":
                case "plus":
                    return new Color(1f, 1f, 1f);
                case "house":
                    if (RoundedRect(x, y, 0.42f, 0.12f, 0.58f, 0.36f, 0.04f)) return ink;
                    return y > 0.55f ? new Color(1f, 0.45f, 0.4f) : white;
                case "list":
                {
                    bool line = x > 0.42f && x < 0.7f && (Mathf.Abs(y - 0.7f) < 0.03f || Mathf.Abs(y - 0.5f) < 0.03f || Mathf.Abs(y - 0.3f) < 0.03f);
                    bool tick = Vector2.Distance(p, new Vector2(0.31f, 0.7f)) < 0.05f || Vector2.Distance(p, new Vector2(0.31f, 0.5f)) < 0.05f
                                || Vector2.Distance(p, new Vector2(0.31f, 0.3f)) < 0.05f;
                    if (tick) return new Color(0.36f, 0.83f, 0.36f);
                    return line ? ink : white;
                }
                default:
                {
                    float a = Mathf.Atan2(y - 0.5f, x - 0.5f);
                    int seg = (int)Mathf.Floor((a + Mathf.PI) / (Mathf.PI / 3f));
                    if (Vector2.Distance(p, new Vector2(0.5f, 0.5f)) < 0.08f) return ink;
                    Color[] cols = { new Color(1f, 0.45f, 0.4f), white, new Color(1f, 0.8f, 0.25f), white, new Color(0.4f, 0.8f, 1f), white };
                    return cols[Mathf.Clamp(seg, 0, 5)];
                }
            }
        }

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
                case "sail":
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
                case "hull":
                {
                    // Shield over a plank hull: sturdier boat.
                    bool shield = RoundedRect(x, y, 0.28f, 0.42f, 0.72f, 0.82f, 0.06f)
                                  || InTriangle(p, new Vector2(0.28f, 0.44f), new Vector2(0.72f, 0.44f), new Vector2(0.5f, 0.16f));
                    bool stripe = shield && Mathf.Abs(x - 0.5f) < 0.05f;
                    if (stripe) return accent;
                    return shield ? white : bg;
                }
                case "lamp":
                {
                    bool glass = RoundedRect(x, y, 0.36f, 0.3f, 0.64f, 0.66f, 0.06f);
                    bool cap = RoundedRect(x, y, 0.32f, 0.66f, 0.68f, 0.74f, 0.03f) || RoundedRect(x, y, 0.32f, 0.22f, 0.68f, 0.3f, 0.03f);
                    bool handle = Mathf.Abs(Vector2.Distance(p, new Vector2(0.5f, 0.76f)) - 0.1f) < 0.025f && y > 0.76f;
                    float glow = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0.5f, 0.48f)) * 3f);
                    if (cap || handle) return ink;
                    if (glass) return Color.Lerp(accent, Color.white, glow);
                    return Color.Lerp(bg, new Color(1f, 0.9f, 0.5f), glow * 0.5f);
                }
                case "island":
                {
                    float sea = y < 0.34f ? 1f : 0f;
                    bool land = Vector2.Distance(new Vector2(p.x, p.y * 1.8f), new Vector2(0.5f, 0.52f)) < 0.3f && y > 0.26f;
                    bool trunk = Mathf.Abs(x - 0.56f - (y - 0.4f) * 0.25f) < 0.025f && y > 0.36f && y < 0.7f;
                    bool leaves = Vector2.Distance(p, new Vector2(0.52f, 0.72f)) < 0.07f || Vector2.Distance(p, new Vector2(0.66f, 0.7f)) < 0.07f
                                  || Vector2.Distance(p, new Vector2(0.6f, 0.78f)) < 0.07f;
                    bool hut = RoundedRect(x, y, 0.3f, 0.34f, 0.44f, 0.46f, 0.03f);
                    bool roof = InTriangle(p, new Vector2(0.27f, 0.45f), new Vector2(0.47f, 0.45f), new Vector2(0.37f, 0.56f));
                    if (leaves) return new Color(0.35f, 0.72f, 0.4f);
                    if (trunk) return new Color(0.55f, 0.36f, 0.2f);
                    if (roof) return new Color(0.88f, 0.48f, 0.37f);
                    if (hut) return white;
                    if (land) return new Color(0.95f, 0.85f, 0.6f);
                    return sea > 0f ? new Color(0.3f, 0.65f, 0.9f) : bg;
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
            const int ss = 2; // 2x2 supersampling for clean edges
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            sum += IconPixel((px + (sx + 0.5f) / ss) / size, (py + (sy + 0.5f) / ss) / size);
                        }
                    }

                    Color c = sum / (ss * ss);
                    c.a = 1f;
                    pixels[py * size + px] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// App icon: a night sea under a big moon, its silver path on the water, a lit lighthouse on the horizon and
        /// a small sailboat with a warm lantern riding the crest of a swell.
        /// </summary>
        private static Color IconPixel(float x, float y)
        {
            Vector2 p = new Vector2(x, y);
            Color skyTop = new Color(0.03f, 0.05f, 0.14f);
            Color skyLow = new Color(0.2f, 0.28f, 0.5f);
            float horizon = 0.44f;

            // Sky with a moon halo.
            Color c = Color.Lerp(skyLow, skyTop, Mathf.SmoothStep(horizon, 1f, y));
            Vector2 moonC = new Vector2(0.66f, 0.72f);
            float md = Vector2.Distance(p, moonC);
            c += new Color(0.55f, 0.6f, 0.8f) * (Mathf.Exp(-md * 7f) * 0.45f);
            // A few stars.
            float sx = Mathf.Floor(x * 40f), sy = Mathf.Floor(y * 40f);
            float h = Mathf.Repeat(Mathf.Sin(sx * 12.9898f + sy * 78.233f) * 43758.5453f, 1f);
            if (y > horizon + 0.08f && h > 0.965f && md > 0.2f)
            {
                Vector2 cell = new Vector2((sx + 0.5f) / 40f, (sy + 0.5f) / 40f);
                c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - Vector2.Distance(p, cell) * 220f) * 0.9f);
            }

            // Moon disc with soft maria.
            if (md < 0.17f)
            {
                float edge = Mathf.Clamp01((0.17f - md) * 300f);
                float maria = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0.62f, 0.76f)) * 14f) * 0.12f
                              + Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(0.7f, 0.68f)) * 18f) * 0.1f;
                Color moon = new Color(1f, 0.97f, 0.88f) * (1f - maria);
                c = Color.Lerp(c, moon, edge);
            }

            // Far island with a lighthouse and its beam.
            float island = horizon + 0.025f * Mathf.Exp(-Mathf.Pow((x - 0.86f) * 9f, 2f));
            if (y < island && y > horizon - 0.01f && x > 0.72f)
            {
                c = new Color(0.07f, 0.09f, 0.18f);
            }

            if (x > 0.845f && x < 0.875f && y > island - 0.005f && y < island + 0.1f)
            {
                c = Mathf.Repeat((y - island) * 40f, 1f) < 0.5f ? new Color(0.9f, 0.88f, 0.84f) : new Color(0.7f, 0.25f, 0.22f);
            }

            float lampY = island + 0.11f;
            float lamp = Vector2.Distance(p, new Vector2(0.86f, lampY));
            c += new Color(1f, 0.85f, 0.5f) * (Mathf.Exp(-lamp * 45f) * 0.9f);
            if (y > lampY - 0.02f && y < lampY + 0.02f + (0.86f - x) * 0.12f && x < 0.86f && x > 0.45f)
            {
                c += new Color(1f, 0.9f, 0.6f) * 0.08f * Mathf.Clamp01((x - 0.45f) * 3f);
            }

            // Sea: layered swells, moon path glitter, a big crest in front.
            if (y < horizon)
            {
                float depth = Mathf.Clamp01((horizon - y) / horizon);
                c = Color.Lerp(new Color(0.18f, 0.32f, 0.52f), new Color(0.03f, 0.1f, 0.22f), Mathf.Pow(depth, 0.7f));
                float band = Mathf.Sin(y * 120f + Mathf.Sin(x * 14f) * 2f);
                c += new Color(0.1f, 0.14f, 0.2f) * Mathf.Clamp01(band) * (1f - depth) * 0.5f;
                // Moon path: a column of glints under the moon that widens towards the viewer.
                float path = Mathf.Abs(x - 0.66f) / (0.05f + depth * 0.25f);
                float glint = Mathf.Clamp01(1f - path) * Mathf.Clamp01(Mathf.Sin(y * 260f + x * 30f) * 2f - 0.6f);
                c += new Color(0.9f, 0.92f, 1f) * glint * 0.55f;
            }

            float crest = 0.3f + 0.1f * Mathf.Sin(x * 5.2f + 0.6f) + 0.015f * Mathf.Sin(x * 31f);
            if (y < crest)
            {
                float d = crest - y;
                Color face = Color.Lerp(new Color(0.12f, 0.42f, 0.6f), new Color(0.02f, 0.08f, 0.18f), Mathf.Clamp01(d / 0.3f));
                c = face;
                if (d < 0.018f)
                {
                    c = Color.Lerp(new Color(0.85f, 0.93f, 1f), face, d / 0.018f); // foam lip
                }
            }

            // Boat on the crest: dark hull, cream sails, a warm lantern.
            float bx = 0.3f;
            float by = 0.3f + 0.1f * Mathf.Sin(bx * 5.2f + 0.6f) + 0.005f;
            bool hull = InTriangle(p, new Vector2(bx - 0.13f, by + 0.035f), new Vector2(bx + 0.14f, by + 0.035f), new Vector2(bx + 0.1f, by - 0.02f))
                        || InTriangle(p, new Vector2(bx - 0.13f, by + 0.035f), new Vector2(bx + 0.1f, by - 0.02f), new Vector2(bx - 0.09f, by - 0.02f));
            bool mainSail = InTriangle(p, new Vector2(bx - 0.005f, by + 0.27f), new Vector2(bx - 0.005f, by + 0.05f), new Vector2(bx + 0.12f, by + 0.05f));
            bool jib = InTriangle(p, new Vector2(bx - 0.02f, by + 0.24f), new Vector2(bx - 0.02f, by + 0.06f), new Vector2(bx - 0.11f, by + 0.06f));
            bool mast = x > bx - 0.02f && x < bx - 0.005f && y > by + 0.03f && y < by + 0.29f;
            if (mainSail || jib)
            {
                c = Color.Lerp(new Color(0.93f, 0.88f, 0.78f), new Color(0.75f, 0.72f, 0.7f), Mathf.Clamp01((x - bx) * 4f + 0.3f));
            }

            if (mast)
            {
                c = new Color(0.22f, 0.15f, 0.12f);
            }

            if (hull)
            {
                c = Color.Lerp(new Color(0.42f, 0.25f, 0.16f), new Color(0.26f, 0.15f, 0.1f), Mathf.Clamp01((by + 0.035f - y) / 0.055f));
            }

            float lantern = Vector2.Distance(p, new Vector2(bx + 0.1f, by + 0.06f));
            c += new Color(1f, 0.7f, 0.3f) * (Mathf.Exp(-lantern * 60f) * 1.2f);
            if (lantern < 0.012f)
            {
                c = new Color(1f, 0.85f, 0.5f);
            }

            // Gentle vignette.
            float v = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
            c *= 1f - Mathf.Clamp01(v - 0.45f) * 0.6f;
            return c;
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

        private static Sprite Draw(string name, int size, Func<float, float, Color> shader, Vector4 border = default)
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

            if (AssetImporter.GetAtPath(path) is TextureImporter importer
                && (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spriteBorder = border; // 9-slice insets (left, bottom, right, top) in pixels
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
