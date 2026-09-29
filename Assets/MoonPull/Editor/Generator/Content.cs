using System.Collections.Generic;
using MoonPull.Audio;
using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Pooling;
using MoonPull.Gameplay.Visuals;
using MoonPull.Layers.Creatures;
using MoonPull.Layers.Passengers;
using MoonPull.Layers.Weather;
using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.EditorTools
{
    /// <summary>Configs, prefabs and catalogs. Values not set here keep the designed defaults from the config classes.</summary>
    internal sealed class Content
    {
        public GameConfig Game;
        public MoonConfig Moon;
        public TideConfig Tide;
        public WaterConfig Water;
        public SeabedConfig Seabed;
        public BoatConfig Boat;
        public CameraConfig Camera;
        public LevelGenConfig LevelGen;
        public ScoreConfig Score;
        public NearMissConfig NearMiss;
        public WaveLaunchConfig WaveLaunch;
        public FullMoonConfig FullMoon;
        public PickupConfig Pickup;
        public LevelRunnerConfig Runner;
        public PassengerConfig Passenger;
        public WeatherConfig Weather;
        public CreatureConfig Creature;
        public BossConfig Boss;
        public RewindConfig Rewind;
        public EconomyConfig Economy;
        public AdConfig Ads;
        public IapConfig Iap;
        public FeedbackConfig Feedback;
        public PoolWarmupConfig Warmup;
        public SfxLibrary Sfx;
        public PlacementPrefabSet Prefabs;
        public RegionCatalog Regions;
        public BoatCatalog Boats;
        public MissionCatalog Missions;
        public BoatDebris Debris;

        private readonly List<(PooledObject prefab, int count)> warm = new List<(PooledObject, int)>();

        private static readonly Color Rock = Gen.Hex("5B5E7A");
        private static readonly Color Chevron = Gen.Hex("FFD37A");

        public static Content Build()
        {
            var c = new Content();
            c.CreateConfigs();
            c.CreateAudio();
            c.CreatePlacementPrefabs();
            c.CreateRegions();
            c.CreateBoats();
            c.CreateMissions();
            c.CreateWarmup();
            return c;
        }

        private void CreateConfigs()
        {
            Game = Gen.So<GameConfig>("Config", "GameConfig");
            Moon = Gen.So<MoonConfig>("Config", "MoonConfig");
            Tide = Gen.So<TideConfig>("Config", "TideConfig");
            Water = Gen.So<WaterConfig>("Config", "WaterConfig");
            Seabed = Gen.So<SeabedConfig>("Config", "SeabedConfig");
            Boat = Gen.So<BoatConfig>("Config", "BoatConfig");
            Camera = Gen.So<CameraConfig>("Config", "CameraConfig");
            LevelGen = Gen.So<LevelGenConfig>("Config", "LevelGenConfig");
            Score = Gen.So<ScoreConfig>("Config", "ScoreConfig");
            NearMiss = Gen.So<NearMissConfig>("Config", "NearMissConfig");
            WaveLaunch = Gen.So<WaveLaunchConfig>("Config", "WaveLaunchConfig");
            FullMoon = Gen.So<FullMoonConfig>("Config", "FullMoonConfig");
            Pickup = Gen.So<PickupConfig>("Config", "PickupConfig");
            Runner = Gen.So<LevelRunnerConfig>("Config", "LevelRunnerConfig");
            Passenger = Gen.So<PassengerConfig>("Config", "PassengerConfig");
            Weather = Gen.So<WeatherConfig>("Config", "WeatherConfig");
            Creature = Gen.So<CreatureConfig>("Config", "CreatureConfig");
            Boss = Gen.So<BossConfig>("Config", "BossConfig");
            Rewind = Gen.So<RewindConfig>("Config", "RewindConfig");
            Economy = Gen.So<EconomyConfig>("Config", "EconomyConfig");
            Ads = Gen.So<AdConfig>("Config", "AdConfig");
            Iap = Gen.So<IapConfig>("Config", "IapConfig");
            Feedback = Gen.So<FeedbackConfig>("Config", "FeedbackConfig");
            Warmup = Gen.So<PoolWarmupConfig>("Config", "PoolWarmupConfig");
            Sfx = Gen.So<SfxLibrary>("Config", "SfxLibrary");
            Prefabs = Gen.So<PlacementPrefabSet>("Catalogs", "PlacementPrefabSet");
            Regions = Gen.So<RegionCatalog>("Catalogs", "RegionCatalog");
            Boats = Gen.So<BoatCatalog>("Catalogs", "BoatCatalog");
            Missions = Gen.So<MissionCatalog>("Catalogs", "MissionCatalog");
            Gen.Set(Iap, "starterPackBoatId", "boat_moonrunner");
        }

        private void CreateAudio()
        {
            var ids = (SfxId[])System.Enum.GetValues(typeof(SfxId));
            Gen.SetStructArray(Sfx, "entries", ids.Length, (element, i) =>
            {
                Gen.Child(element, "Id", ids[i]);
                SerializedClips(element, Synth.Sfx(ids[i]));
                Gen.Child(element, "Volume", ids[i] == SfxId.Splash ? 0.5f : 0.8f);
                Gen.Child(element, "PitchVariance", ids[i] == SfxId.StarPickup || ids[i] == SfxId.UiTap ? 0f : 0.05f);
            });
            Gen.Wire(Sfx,
                "menuMusic", Synth.Music("music_menu", 196f, false),
                "fullMoonLayer", Synth.FullMoonLayer(),
                "waveLoop", Synth.WaveLoop());
        }

        private static void SerializedClips(UnityEditor.SerializedProperty element, AudioClip clip)
        {
            UnityEditor.SerializedProperty clips = element.FindPropertyRelative("Clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        }

        // ---------------------------------------------------------------- placement prefabs

        private GameObject Placement(string name, out Transform visual)
        {
            GameObject root = Gen.Go(name);
            visual = Gen.Go("Visual", root.transform).transform;
            Gen.Add<PooledObject>(root);
            return root;
        }

        private T Finish<T>(GameObject root, string folder, int warmCount) where T : Component
        {
            T prefab = Gen.SavePrefab<T>(root, folder, root.name);
            if (prefab != null && warmCount > 0)
            {
                warm.Add((prefab.GetComponent<PooledObject>(), warmCount));
            }

            return prefab;
        }

        private static void WireView(PlacementView view, Transform visual, float authoredWidth)
        {
            Gen.Wire(view, "visualRoot", visual, "authoredWidth", authoredWidth);
        }

        /// <summary>Low obstacle: pointed, upward-facing (readable by shape, not color). Pivot at its top edge.</summary>
        private PlacementView LowObstacle(string name, Color color, int style)
        {
            GameObject root = Placement(name, out Transform v);
            Color dark = color * 0.75f;
            dark.a = 1f;
            switch (style)
            {
                case 0: // jagged rock
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, -3.4f, 0f), new Vector3(1.6f, 6f, 1.4f), dark);
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, -0.55f, 0f), new Vector3(0.8f, 0.8f, 1.2f), color, new Vector3(0f, 0f, 45f));
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(-0.45f, -0.8f, 0.1f), new Vector3(0.55f, 0.55f, 1f), color, new Vector3(0f, 0f, 45f));
                    break;
                case 1: // reef spires
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, -3.6f, 0f), new Vector3(1.8f, 6f, 1.3f), dark);
                    for (int i = -1; i <= 1; i++)
                    {
                        float h = i == 0 ? 0.9f : 0.6f;
                        Gen.Prim(PrimitiveType.Cylinder, v, new Vector3(i * 0.55f, -0.6f - (0.9f - h) * 0.5f, 0f), new Vector3(0.28f, h * 0.5f, 0.28f), color);
                    }

                    break;
                default: // wreck hull
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, -3.4f, 0f), new Vector3(1.8f, 6f, 1.2f), dark);
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0.2f, -0.45f, 0f), new Vector3(0.2f, 0.9f, 0.2f), color, new Vector3(0f, 0f, -18f));
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(-0.3f, -0.55f, 0f), new Vector3(1.2f, 0.25f, 1.1f), color, new Vector3(0f, 0f, 12f));
                    break;
            }

            // Tide hint: an up chevron means "raise the water here".
            Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, -1.5f, -0.75f), new Vector3(0.35f, 0.08f, 0.05f), Chevron, new Vector3(0f, 0f, 35f), 0.6f);
            Gen.Prim(PrimitiveType.Cube, v, new Vector3(0.2f, -1.5f, -0.75f), new Vector3(0.35f, 0.08f, 0.05f), Chevron, new Vector3(0f, 0f, -35f), 0.6f);
            var view = Gen.Add<PlacementView>(root);
            WireView(view, v, 1.8f);
            return Finish<PlacementView>(root, "Obstacles", 5);
        }

        /// <summary>High obstacle: flat-bottomed and hanging. Pivot at its bottom edge.</summary>
        private PlacementView HighObstacle(string name, Color color, int style)
        {
            GameObject root = Placement(name, out Transform v);
            Color dark = color * 0.7f;
            dark.a = 1f;
            switch (style)
            {
                case 0: // stone bridge
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, 0.35f, 0f), new Vector3(1.8f, 0.7f, 2.2f), color);
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, 4.5f, 1.3f), new Vector3(1.8f, 8f, 0.4f), dark);
                    break;
                case 1: // cave ceiling
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, 4f, 0f), new Vector3(1.8f, 8f, 2.4f), dark);
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(-0.4f, 0.25f, -0.6f), new Vector3(0.35f, 0.35f, 0.35f), color, new Vector3(0f, 0f, 45f));
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0.45f, 0.3f, -0.6f), new Vector3(0.3f, 0.3f, 0.3f), color, new Vector3(0f, 0f, 45f));
                    break;
                default: // rope bridge
                    Gen.Prim(PrimitiveType.Cube, v, new Vector3(0f, 0.12f, 0f), new Vector3(1.8f, 0.24f, 1.6f), color);
                    Gen.Prim(PrimitiveType.Cylinder, v, new Vector3(-0.8f, 4f, 0.9f), new Vector3(0.15f, 4f, 0.15f), dark);
                    Gen.Prim(PrimitiveType.Cylinder, v, new Vector3(0.8f, 4f, 0.9f), new Vector3(0.15f, 4f, 0.15f), dark);
                    break;
            }

            // Tide hint: a down chevron under the deck means "lower the water here".
            Gen.Prim(PrimitiveType.Cube, v, new Vector3(-0.1f, 0.35f, -1.15f), new Vector3(0.35f, 0.08f, 0.05f), Chevron, new Vector3(0f, 0f, -35f), 0.6f);
            Gen.Prim(PrimitiveType.Cube, v, new Vector3(0.1f, 0.35f, -1.15f), new Vector3(0.35f, 0.08f, 0.05f), Chevron, new Vector3(0f, 0f, 35f), 0.6f);
            var view = Gen.Add<PlacementView>(root);
            WireView(view, v, 1.8f);
            return Finish<PlacementView>(root, "Obstacles", 5);
        }

        private PlacementView MakePickup(string name, PrimitiveType type, Vector3 scale, Color color, Vector3 euler, int warmCount)
        {
            GameObject root = Placement(name, out Transform v);
            GameObject body = Gen.Prim(type, v, Vector3.zero, scale, color, euler, 0.8f);
            Gen.Add<Spinner>(body);
            var view = Gen.Add<PlacementView>(root);
            WireView(view, v, 0f);
            return Finish<PlacementView>(root, "Pickups", warmCount);
        }

        private void CreatePlacementPrefabs()
        {
            var star = MakePickup("Star", PrimitiveType.Cube, new Vector3(0.45f, 0.45f, 0.12f), Gen.Hex("FFD95C"), new Vector3(0f, 0f, 45f), 20);
            var coin = MakePickup("Coin", PrimitiveType.Cylinder, new Vector3(0.45f, 0.05f, 0.45f), Gen.Hex("F5B642"), new Vector3(90f, 0f, 0f), 24);
            var moonstone = MakePickup("Moonstone", PrimitiveType.Sphere, new Vector3(0.5f, 0.6f, 0.5f), Gen.Hex("BDE6FF"), Vector3.zero, 7);

            // Chest on the seabed, sparkling when the tide is low enough to reach it.
            GameObject chestRoot = Placement("Chest", out Transform cv);
            Gen.Prim(PrimitiveType.Cube, cv, new Vector3(0f, 0.25f, 0f), new Vector3(0.8f, 0.5f, 0.55f), Gen.Hex("8A5A2B"));
            Gen.Prim(PrimitiveType.Cube, cv, new Vector3(0f, 0.55f, 0f), new Vector3(0.84f, 0.16f, 0.6f), Gen.Hex("C8932E"));
            GameObject sparkle = Gen.Prim(PrimitiveType.Sphere, chestRoot.transform, new Vector3(0f, 0.9f, -0.3f), Vector3.one * 0.25f, Gen.Hex("FFF3B0"), default, 1.5f);
            Gen.Add<Spinner>(sparkle);
            var chest = Gen.Add<ChestView>(chestRoot);
            Gen.Wire(chest, "visualRoot", cv, "authoredWidth", 0f, "revealSparkle", sparkle);
            chest = Finish<ChestView>(chestRoot, "Pickups", 2);

            GameObject sandRoot = Placement("Sandbar", out Transform sv);
            Gen.Prim(PrimitiveType.Cube, sv, new Vector3(0f, -1.5f, 0f), new Vector3(5f, 3f, 3f), Gen.Hex("E8D3A0"));
            var sandbar = Gen.Add<PlacementView>(sandRoot);
            WireView(sandbar, sv, 5f);
            sandbar = Finish<PlacementView>(sandRoot, "Pickups", 2);

            // Dock: deck surface at the pivot, three waiting passengers.
            GameObject dockRoot = Placement("Dock", out Transform dv);
            Gen.Prim(PrimitiveType.Cube, dv, new Vector3(0f, -0.08f, 1.2f), new Vector3(3.5f, 0.16f, 1.2f), Gen.Hex("B07A4A"));
            for (int i = -1; i <= 1; i += 2)
            {
                Gen.Prim(PrimitiveType.Cylinder, dv, new Vector3(i * 1.5f, -2f, 1.2f), new Vector3(0.16f, 2f, 0.16f), Gen.Hex("7A5230"));
            }

            var figures = new GameObject[3];
            Color[] shirts = { Gen.Hex("FF8C8C"), Gen.Hex("8CC8FF"), Gen.Hex("B8FF8C") };
            for (int i = 0; i < 3; i++)
            {
                figures[i] = Gen.Prim(PrimitiveType.Capsule, dv, new Vector3(-0.8f + i * 0.8f, 0.45f, 1.2f), new Vector3(0.3f, 0.35f, 0.3f), shirts[i]);
            }

            var dock = Gen.Add<DockView>(dockRoot);
            Gen.Wire(dock, "visualRoot", dv, "authoredWidth", 0f);
            Gen.SetArray(dock, "passengerFigures", figures);
            dock = Finish<DockView>(dockRoot, "Layers", 2);

            var dolphin = MakeCreature("Dolphin", Gen.Hex("7FA7D9"), new Vector3(1.4f, 0.35f, 0.35f), new Vector3(0f, -0.9f, 0f), new Vector3(0f, 0.1f, 0f), true);
            var whale = MakeCreature("Whale", Gen.Hex("4A5E8A"), new Vector3(2.5f, 0.7f, 1.2f), new Vector3(0f, -1.6f, 0f), new Vector3(0f, -0.25f, 0f), false);
            var shark = SharkPrefab();
            var kraken = KrakenPrefab();

            GameObject lighthouseRoot = Placement("Lighthouse", out Transform lv);
            Gen.Prim(PrimitiveType.Cylinder, lv, new Vector3(0f, 1.5f, 3f), new Vector3(0.6f, 1.5f, 0.6f), Gen.Hex("F2F2F2"));
            Gen.Prim(PrimitiveType.Cylinder, lv, new Vector3(0f, 1.4f, 3f), new Vector3(0.62f, 0.2f, 0.62f), Gen.Hex("E05A5A"));
            Gen.Prim(PrimitiveType.Sphere, lv, new Vector3(0f, 3.2f, 3f), Vector3.one * 0.5f, Gen.Hex("FFF4C2"), default, 2f);
            Transform pivot = Gen.Go("BeamPivot", lighthouseRoot.transform).transform;
            pivot.localPosition = new Vector3(0f, 3.2f, 3f);
            GameObject beamGo = Gen.Go("Beam", pivot);
            beamGo.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            var beam = beamGo.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.range = 18f;
            beam.spotAngle = 30f;
            beam.color = Gen.Hex("FFF1C9");
            var lighthouse = Gen.Add<LighthouseBeamView>(lighthouseRoot);
            Gen.Wire(lighthouse, "visualRoot", lv, "authoredWidth", 0f, "beamPivot", pivot, "beamLight", beam);
            lighthouse = Finish<LighthouseBeamView>(lighthouseRoot, "Layers", 3);

            GameObject harborRoot = Placement("Harbor", out Transform hv);
            Gen.Prim(PrimitiveType.Cube, hv, new Vector3(2f, 0.2f, 1.5f), new Vector3(4f, 0.3f, 2f), Gen.Hex("B07A4A"));
            Gen.Prim(PrimitiveType.Cube, hv, new Vector3(4.5f, 1.5f, 2.5f), new Vector3(2f, 3f, 2f), Gen.Hex("F4E3C1"));
            Gen.Prim(PrimitiveType.Cube, hv, new Vector3(4.5f, 3.3f, 2.5f), new Vector3(2.3f, 0.6f, 2.3f), Gen.Hex("D9534F"), new Vector3(0f, 0f, 0f));
            GameObject flagPole = Gen.Prim(PrimitiveType.Cylinder, hv, new Vector3(0.2f, 1.6f, 1.5f), new Vector3(0.08f, 1.5f, 0.08f), Gen.Hex("DDDDDD"));
            Gen.Prim(PrimitiveType.Cube, flagPole.transform.parent, new Vector3(0.55f, 2.8f, 1.5f), new Vector3(0.7f, 0.4f, 0.04f), Gen.Hex("FFD95C"), default, 0.4f);
            var harbor = Gen.Add<PlacementView>(harborRoot);
            WireView(harbor, hv, 0f);
            harbor = Finish<PlacementView>(harborRoot, "Layers", 1);

            // Fallback obstacles (used if a region has none).
            var fallbackLow = new[] { LowObstacle("Low_Fallback", Rock, 0) };
            var fallbackHigh = new[] { HighObstacle("High_Fallback", Gen.Hex("8C7A6B"), 0) };

            Gen.SetArray(Prefabs, "fallbackLowObstacles", fallbackLow);
            Gen.SetArray(Prefabs, "fallbackHighObstacles", fallbackHigh);
            Gen.Wire(Prefabs,
                "star", star, "coin", coin, "moonstone", moonstone, "chest", chest, "sandbar", sandbar, "dock", dock,
                "dolphin", dolphin, "whale", whale, "shark", shark, "kraken", kraken, "lighthouse", lighthouse, "harbor", harbor);

            Debris = DebrisPrefab();
        }

        private CreatureView MakeCreature(string name, Color color, Vector3 size, Vector3 hidden, Vector3 engaged, bool always)
        {
            GameObject root = Placement(name, out Transform v);
            Transform body = Gen.Go("Body", v).transform;
            Gen.Prim(PrimitiveType.Sphere, body, Vector3.zero, size, color);
            Gen.Prim(PrimitiveType.Cube, body, new Vector3(-size.x * 0.5f, size.y * 0.1f, 0f), new Vector3(0.3f, size.y * 0.8f, 0.08f), color, new Vector3(0f, 0f, 30f));
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(size.x * 0.35f, size.y * 0.15f, -size.z * 0.45f), Vector3.one * 0.1f, Color.black);
            var view = Gen.Add<CreatureView>(root);
            Gen.Wire(view, "visualRoot", v, "authoredWidth", 0f, "body", body, "hiddenOffset", hidden, "engagedOffset", engaged, "alwaysEngaged", always);
            return Finish<CreatureView>(root, "Layers", 2);
        }

        private CreatureView SharkPrefab()
        {
            GameObject root = Placement("Shark", out Transform v);
            Transform body = Gen.Go("Body", v).transform;
            Gen.Prim(PrimitiveType.Cube, body, new Vector3(0f, 0.35f, -0.2f), new Vector3(0.5f, 0.5f, 0.08f), Gen.Hex("5F6E80"), new Vector3(0f, 0f, 45f));
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(0f, -0.1f, -0.2f), new Vector3(1.6f, 0.35f, 0.5f), Gen.Hex("5F6E80"));
            var view = Gen.Add<CreatureView>(root);
            Gen.Wire(view, "visualRoot", v, "authoredWidth", 0f, "body", body,
                "hiddenOffset", new Vector3(0f, -0.9f, 0f), "engagedOffset", new Vector3(0f, 0f, 0f), "alwaysEngaged", false);
            return Finish<CreatureView>(root, "Layers", 2);
        }

        private CreatureView KrakenPrefab()
        {
            GameObject root = Placement("Kraken", out Transform v);
            Transform body = Gen.Go("Body", v).transform;
            Color purple = Gen.Hex("7B4FA0");
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 0.3f, 1f), new Vector3(2.4f, 2f, 2f), purple);
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(-0.5f, 0.6f, 0.05f), Vector3.one * 0.45f, Color.white, default, 0.3f);
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(0.5f, 0.6f, 0.05f), Vector3.one * 0.45f, Color.white, default, 0.3f);
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(-0.5f, 0.6f, -0.18f), Vector3.one * 0.2f, Color.black);
            Gen.Prim(PrimitiveType.Sphere, body, new Vector3(0.5f, 0.6f, -0.18f), Vector3.one * 0.2f, Color.black);
            for (int i = 0; i < 4; i++)
            {
                float x = -1.5f + i;
                Gen.Prim(PrimitiveType.Capsule, body, new Vector3(x, -0.6f, 0.6f), new Vector3(0.35f, 0.9f, 0.35f), purple * 0.85f, new Vector3(0f, 0f, (i - 1.5f) * 18f));
            }

            var view = Gen.Add<CreatureView>(root);
            Gen.Wire(view, "visualRoot", v, "authoredWidth", 0f, "body", body,
                "hiddenOffset", new Vector3(0f, -3f, 0f), "engagedOffset", new Vector3(0f, -0.4f, 0f), "alwaysEngaged", false);
            return Finish<CreatureView>(root, "Layers", 3);
        }

        private BoatDebris DebrisPrefab()
        {
            GameObject root = Gen.Go("BoatDebris");
            Gen.Add<PooledObject>(root);
            var pieces = new List<Rigidbody>();
            for (int i = 0; i < 8; i++)
            {
                GameObject plank = Gen.Prim(PrimitiveType.Cube, root.transform,
                    new Vector3(-0.7f + (i % 4) * 0.45f, (i / 4) * 0.3f, 0f), new Vector3(0.42f, 0.12f, 0.35f), Gen.Hex("C98B53"));
                var body = plank.AddComponent<Rigidbody>();
                body.mass = 0.2f;
                body.linearDamping = 0.5f;
                body.isKinematic = true;
                pieces.Add(body);
            }

            var debris = Gen.Add<BoatDebris>(root);
            Gen.SetArray(debris, "pieces", pieces);
            BoatDebris saved = Gen.SavePrefab<BoatDebris>(root, "Boat", "BoatDebris");
            if (saved != null)
            {
                warm.Add((saved.GetComponent<PooledObject>(), 1));
            }

            return saved;
        }

        // ---------------------------------------------------------------- regions

        private struct RegionSpec
        {
            public string Id;
            public int Stars;
            public string SkyTop, SkyBottom, Shallow, Deep, Foam, Fog, Rock, Bridge, Signature;
            public PlacementKind SignatureKind;
            public int Idle;
            public float Root;
            public float CostScale;
        }

        private void CreateRegions()
        {
            RegionSpec[] specs =
            {
                new RegionSpec { Id = "tropical_lagoon", Stars = 0, SkyTop = "1B1F4B", SkyBottom = "7A5C9E", Shallow = "6FE0D8", Deep = "1D4E7A", Foam = "F4FBFF", Fog = "8C8FB8", Rock = "5E6C8C", Bridge = "A67C52", Signature = "FF8FA3", SignatureKind = PlacementKind.LowObstacle, Idle = 60, Root = 261.63f, CostScale = 1f },
                new RegionSpec { Id = "frozen_north", Stars = 35, SkyTop = "0E2140", SkyBottom = "5C7FB0", Shallow = "A8E6FF", Deep = "1E3F66", Foam = "FFFFFF", Fog = "B8C8DC", Rock = "8FA3BF", Bridge = "D8EEFF", Signature = "BFEFFF", SignatureKind = PlacementKind.HighObstacle, Idle = 90, Root = 293.66f, CostScale = 1.75f },
                new RegionSpec { Id = "volcanic_isles", Stars = 80, SkyTop = "2A0F2E", SkyBottom = "B0554A", Shallow = "F2A279", Deep = "4A1E2E", Foam = "FFE6C7", Fog = "8A5A5A", Rock = "3E3440", Bridge = "6B4A3A", Signature = "FF6A3D", SignatureKind = PlacementKind.LowObstacle, Idle = 130, Root = 220f, CostScale = 2.5f },
                new RegionSpec { Id = "sunken_city", Stars = 130, SkyTop = "0F2A33", SkyBottom = "3E8C8A", Shallow = "7FE3C4", Deep = "114A52", Foam = "E8FFF6", Fog = "6E9C9A", Rock = "8C8A70", Bridge = "C2B48A", Signature = "E8D9A8", SignatureKind = PlacementKind.HighObstacle, Idle = 180, Root = 246.94f, CostScale = 3.25f },
                new RegionSpec { Id = "midnight_sea", Stars = 185, SkyTop = "05060F", SkyBottom = "2A2359", Shallow = "7F8CFF", Deep = "0C0F2E", Foam = "D6DBFF", Fog = "3C3A66", Rock = "3A3F5C", Bridge = "5A4E7A", Signature = "9DFFE5", SignatureKind = PlacementKind.LowObstacle, Idle = 240, Root = 196f, CostScale = 4f }
            };

            var regions = new List<RegionDefinition>();
            int[] baseCosts = { 200, 350, 550, 800, 1100, 1500, 2000, 2600 };
            foreach (RegionSpec spec in specs)
            {
                RegionDefinition region = Gen.So<RegionDefinition>("Regions", "Region_" + spec.Id);
                var lows = new List<PlacementView>();
                var highs = new List<PlacementView>();
                for (int i = 0; i < 3; i++)
                {
                    lows.Add(LowObstacle("Low_" + spec.Id + "_" + i, Gen.Hex(spec.Rock), i));
                    highs.Add(HighObstacle("High_" + spec.Id + "_" + i, Gen.Hex(spec.Bridge), i));
                }

                PlacementView signature = spec.SignatureKind == PlacementKind.LowObstacle
                    ? LowObstacle("Signature_" + spec.Id, Gen.Hex(spec.Signature), 1)
                    : HighObstacle("Signature_" + spec.Id, Gen.Hex(spec.Signature), 1);

                var costs = new int[baseCosts.Length];
                for (int i = 0; i < costs.Length; i++)
                {
                    costs[i] = Mathf.RoundToInt(baseCosts[i] * spec.CostScale / 10f) * 10;
                }

                Gen.Wire(region,
                    "id", spec.Id, "nameKey", "region." + spec.Id, "starsToUnlock", spec.Stars,
                    "skyTop", Gen.Hex(spec.SkyTop), "skyBottom", Gen.Hex(spec.SkyBottom), "waterShallow", Gen.Hex(spec.Shallow),
                    "waterDeep", Gen.Hex(spec.Deep), "foam", Gen.Hex(spec.Foam), "fog", Gen.Hex(spec.Fog),
                    "music", Synth.Music("music_" + spec.Id, spec.Root, spec.Stars > 0),
                    "signatureObstacle", signature, "signatureKind", spec.SignatureKind,
                    "lighthouseStageCosts", costs, "idleCoinsPerHour", spec.Idle);
                Gen.SetArray(region, "lowObstacles", lows);
                Gen.SetArray(region, "highObstacles", highs);
                Gen.SetArray(region, "lighthouseStageVisuals", LighthouseStages(spec.Id, Gen.Hex(spec.Signature)));
                regions.Add(region);
            }

            Gen.SetArray(Regions, "regions", regions);
        }

        /// <summary>Eight stacked construction stages for the menu island, enabled cumulatively.</summary>
        private static List<GameObject> LighthouseStages(string id, Color accent)
        {
            var stages = new List<GameObject>();
            for (int i = 0; i < 8; i++)
            {
                GameObject root = Gen.Go("Stage" + i);
                bool top = i >= 6;
                Color color = i % 2 == 0 ? Gen.Hex("F4F1EA") : accent;
                if (i < 6)
                {
                    float radius = 0.9f - i * 0.07f;
                    Gen.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.3f + i * 0.6f, 0f), new Vector3(radius, 0.3f, radius), color);
                }
                else if (i == 6)
                {
                    Gen.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 3.8f, 0f), new Vector3(0.7f, 0.25f, 0.7f), Gen.Hex("3A3F5C"));
                    Gen.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 4.2f, 0f), Vector3.one * 0.55f, Gen.Hex("FFF1B8"), default, 2f);
                }
                else
                {
                    Gen.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 4.6f, 0f), new Vector3(0.75f, 0.15f, 0.75f), accent);
                    Gen.Prim(PrimitiveType.Cube, root.transform, new Vector3(0f, 4.9f, 0f), new Vector3(0.45f, 0.45f, 0.45f), accent, new Vector3(0f, 45f, 45f));
                }

                GameObject prefab = Gen.SavePrefabObject(root, "Lighthouses/" + id, "Stage" + i + (top ? "_Top" : string.Empty));
                stages.Add(prefab);
            }

            return stages;
        }

        // ---------------------------------------------------------------- boats

        private struct BoatSpec
        {
            public string Id, Hull, Sail;
            public BoatRarity Rarity;
            public int Price;
            public BoatPerkType Perk;
            public float Value;
            public bool Iap;
            public int Shape;
        }

        private void CreateBoats()
        {
            BoatSpec[] specs =
            {
                new BoatSpec { Id = "dinghy", Hull = "E07A5F", Sail = "F4F1DE", Rarity = BoatRarity.Common, Price = 0, Perk = BoatPerkType.None, Shape = 0 },
                new BoatSpec { Id = "skiff", Hull = "3D85C6", Sail = "FFFFFF", Rarity = BoatRarity.Common, Price = 500, Perk = BoatPerkType.CoinBonusPercent, Value = 10, Shape = 0 },
                new BoatSpec { Id = "fisher", Hull = "6AA84F", Sail = "FFE599", Rarity = BoatRarity.Common, Price = 800, Perk = BoatPerkType.PassengerBonusPercent, Value = 15, Shape = 1 },
                new BoatSpec { Id = "ducky", Hull = "FFD966", Sail = "FF9900", Rarity = BoatRarity.Common, Price = 1200, Perk = BoatPerkType.PickupRadiusBonus, Value = 0.3f, Shape = 2 },
                new BoatSpec { Id = "lantern", Hull = "990000", Sail = "FFD966", Rarity = BoatRarity.Common, Price = 1600, Perk = BoatPerkType.FullMoonBonusSeconds, Value = 1, Shape = 1 },
                new BoatSpec { Id = "catamaran", Hull = "00A3C4", Sail = "E6F7FF", Rarity = BoatRarity.Rare, Price = 2500, Perk = BoatPerkType.LaunchBonusPercent, Value = 10, Shape = 0 },
                new BoatSpec { Id = "tug", Hull = "CC4125", Sail = "333333", Rarity = BoatRarity.Rare, Price = 3500, Perk = BoatPerkType.CrashShields, Value = 1, Shape = 1 },
                new BoatSpec { Id = "moonrunner", Hull = "2B2D6E", Sail = "C9D6FF", Rarity = BoatRarity.Rare, Price = 0, Perk = BoatPerkType.StartingMoonstones, Value = 1, Iap = true, Shape = 0 },
                new BoatSpec { Id = "pearl", Hull = "F3E5F5", Sail = "F48FB1", Rarity = BoatRarity.Rare, Price = 5000, Perk = BoatPerkType.CoinBonusPercent, Value = 20, Shape = 0 },
                new BoatSpec { Id = "viking", Hull = "7F6000", Sail = "CC0000", Rarity = BoatRarity.Rare, Price = 6500, Perk = BoatPerkType.NearMissWindowPercent, Value = 15, Shape = 0 },
                new BoatSpec { Id = "gondola", Hull = "1C1C1C", Sail = "FFFFFF", Rarity = BoatRarity.Epic, Price = 8000, Perk = BoatPerkType.PassengerBonusPercent, Value = 25, Shape = 2 },
                new BoatSpec { Id = "icebreaker", Hull = "CFE2F3", Sail = "0B5394", Rarity = BoatRarity.Epic, Price = 10000, Perk = BoatPerkType.LaunchBonusPercent, Value = 20, Shape = 1 },
                new BoatSpec { Id = "ember", Hull = "7F1D1D", Sail = "FF7A30", Rarity = BoatRarity.Epic, Price = 12000, Perk = BoatPerkType.FullMoonBonusSeconds, Value = 2, Shape = 2 },
                new BoatSpec { Id = "sub", Hull = "F1C232", Sail = "38761D", Rarity = BoatRarity.Epic, Price = 15000, Perk = BoatPerkType.IdleIncomeBonusPercent, Value = 25, Shape = 2 },
                new BoatSpec { Id = "pirate", Hull = "4E342E", Sail = "212121", Rarity = BoatRarity.Epic, Price = 18000, Perk = BoatPerkType.CoinBonusPercent, Value = 30, Shape = 0 },
                new BoatSpec { Id = "swan", Hull = "FFFFFF", Sail = "FFCCD5", Rarity = BoatRarity.Epic, Price = 21000, Perk = BoatPerkType.NearMissWindowPercent, Value = 25, Shape = 2 },
                new BoatSpec { Id = "steamer", Hull = "274E13", Sail = "E06666", Rarity = BoatRarity.Legendary, Price = 25000, Perk = BoatPerkType.IdleIncomeBonusPercent, Value = 50, Shape = 1 },
                new BoatSpec { Id = "ghost", Hull = "B4C7DC", Sail = "E8F5FF", Rarity = BoatRarity.Legendary, Price = 30000, Perk = BoatPerkType.NearMissWindowPercent, Value = 35, Shape = 0 },
                new BoatSpec { Id = "starliner", Hull = "20124D", Sail = "FFD966", Rarity = BoatRarity.Legendary, Price = 35000, Perk = BoatPerkType.StartingMoonstones, Value = 2, Shape = 1 },
                new BoatSpec { Id = "leviathan", Hull = "0C343D", Sail = "76A5AF", Rarity = BoatRarity.Legendary, Price = 45000, Perk = BoatPerkType.CrashShields, Value = 2, Shape = 1 }
            };

            var boats = new List<BoatDefinition>();
            foreach (BoatSpec spec in specs)
            {
                Color hull = Gen.Hex(spec.Hull);
                Color sail = Gen.Hex(spec.Sail);
                BoatDefinition boat = Gen.So<BoatDefinition>("Boats", "Boat_" + spec.Id);
                Gen.Wire(boat,
                    "id", "boat_" + spec.Id, "nameKey", "boat." + spec.Id + ".name", "modelPrefab", BoatModel(spec.Id, hull, sail, spec.Shape),
                    "icon", Art.BoatIcon(spec.Id, hull, sail), "rarity", spec.Rarity, "price", spec.Price, "iapOnly", spec.Iap,
                    "perk", spec.Perk, "perkValue", spec.Value, "debrisTint", hull);
                boats.Add(boat);
            }

            Gen.SetArray(Boats, "boats", boats);
            Gen.Set(Boats, "defaultBoat", boats[0]);
        }

        /// <summary>Boat model: pivot at the waterline, bow toward +X, mast top near +1.8 to match BoatConfig's boxes.</summary>
        private static GameObject BoatModel(string id, Color hull, Color sail, int shape)
        {
            GameObject root = Gen.Go("BoatModel_" + id);
            Transform t = root.transform;
            Color trim = Color.Lerp(hull, Color.white, 0.5f);
            Gen.Prim(PrimitiveType.Cube, t, new Vector3(0f, -0.08f, 0f), new Vector3(1.5f, 0.42f, 0.7f), hull);
            Gen.Prim(PrimitiveType.Cube, t, new Vector3(0.78f, 0.02f, 0f), new Vector3(0.4f, 0.4f, 0.7f), hull, new Vector3(0f, 0f, 45f));
            Gen.Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), new Vector3(1.55f, 0.06f, 0.74f), trim);
            switch (shape)
            {
                case 1: // cabin boat
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.25f, 0.45f, 0f), new Vector3(0.6f, 0.5f, 0.55f), trim);
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.25f, 1.1f, 0f), new Vector3(0.12f, 0.45f, 0.12f), sail);
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.25f, 1.6f, 0f), new Vector3(0.3f, 0.15f, 0.05f), sail, default, 0.3f);
                    break;
                case 2: // rounded novelty
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.45f, 0.55f, 0f), new Vector3(0.55f, 0.55f, 0.5f), sail);
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.2f, 0.95f, 0f), new Vector3(0.05f, 0.8f, 0.05f), Gen.Hex("5B4636"));
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.05f, 1.55f, 0f), new Vector3(0.3f, 0.2f, 0.03f), sail);
                    break;
                default: // sailboat
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.95f, 0f), new Vector3(0.06f, 0.85f, 0.06f), Gen.Hex("5B4636"));
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(0.28f, 1.05f, 0f), new Vector3(0.5f, 1.1f, 0.03f), sail, new Vector3(0f, 0f, -8f));
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.82f, 0f), new Vector3(0.2f, 0.1f, 0.02f), Gen.Hex("FF5A5A"), default, 0.4f);
                    break;
            }

            return Gen.SavePrefabObject(root, "Boats", "BoatModel_" + id);
        }

        // ---------------------------------------------------------------- missions & pools

        private void CreateMissions()
        {
            (string id, MissionType type, int target, int reward, int min, string key)[] specs =
            {
                ("m_near_5", MissionType.NearMisses, 5, 80, 0, "mission.near_misses"),
                ("m_near_12", MissionType.NearMisses, 12, 150, 5, "mission.near_misses"),
                ("m_levels_3", MissionType.CompleteLevels, 3, 120, 0, "mission.complete_levels"),
                ("m_stars_15", MissionType.CollectStars, 15, 100, 0, "mission.collect_stars"),
                ("m_fullmoon_2", MissionType.FullMoons, 2, 120, 1, "mission.full_moons"),
                ("m_launch_5", MissionType.WaveLaunches, 5, 100, 2, "mission.wave_launches"),
                ("m_chests_1", MissionType.FindChests, 1, 100, 3, "mission.find_chests"),
                ("m_passengers_2", MissionType.DeliverPassengers, 2, 100, 6, "mission.deliver_passengers"),
                ("m_threestar_2", MissionType.ThreeStarLevels, 2, 150, 2, "mission.three_star")
            };

            var missions = new List<MissionDefinition>();
            foreach (var spec in specs)
            {
                MissionDefinition mission = Gen.So<MissionDefinition>("Missions", spec.id);
                Gen.Wire(mission, "id", spec.id, "descriptionKey", spec.key, "type", spec.type, "target", spec.target,
                    "rewardCoins", spec.reward, "minLevelIndex", spec.min);
                missions.Add(mission);
            }

            Gen.SetArray(Missions, "missions", missions);
        }

        private void CreateWarmup()
        {
            Gen.SetStructArray(Warmup, "entries", warm.Count, (element, i) =>
            {
                Gen.Child(element, "Prefab", warm[i].prefab);
                Gen.Child(element, "Count", warm[i].count);
            });
        }
    }
}
