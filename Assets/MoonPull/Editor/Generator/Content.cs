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
        public GameObject SailRock, SailStar, SailCoin, SailMoonstone, SailLighthouse;
        public GameObject RescueCastaway, RescuePassenger, RescueLantern, RescueIsland, VillageHouse;

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
            c.CreateSailProps();
            c.CreateRescueProps();
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
                "menuMusic", Synth.Music("music_menu_v2", 261.63f, false),
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

        /// <summary>
        /// A little cartoon villager: a round belly-shaped body, a big head with shiny eyes, rosy cheeks and a smile,
        /// hair or a knitted hat, stubby arms (one holding a warm lantern) and feet. Faces -Z (towards the camera).
        /// Parts named "Coat", "Hair" and "Skin" are recoloured per villager at runtime.
        /// </summary>
        private static void Person(Transform parent, Vector3 at, float scale, Color coat)
        {
            Transform p = Gen.Go("Person", parent).transform;
            p.localPosition = at;
            p.localScale = Vector3.one * scale;
            Color skin = Gen.Hex("F6CFA6");
            Color hair = Gen.Hex("5A3A2A");
            Color ink = Gen.Hex("2A2135");

            // Body: a soft pear with a lighter belly patch.
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(0f, 0.28f, 0f), new Vector3(0.36f, 0.4f, 0.32f), coat).name = "Coat";
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(0f, 0.25f, -0.1f), new Vector3(0.22f, 0.24f, 0.14f), Color.Lerp(coat, Color.white, 0.45f));
            // Feet.
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(-0.09f, 0.05f, -0.03f), new Vector3(0.13f, 0.08f, 0.17f), ink);
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(0.09f, 0.05f, -0.03f), new Vector3(0.13f, 0.08f, 0.17f), ink);
            // Arms with round hands; the right one holds up a lantern.
            Gen.Prim(PrimitiveType.Capsule, p, new Vector3(-0.2f, 0.3f, 0f), new Vector3(0.08f, 0.1f, 0.08f), coat, new Vector3(0f, 0f, -35f)).name = "Coat";
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(-0.25f, 0.21f, 0f), new Vector3(0.08f, 0.08f, 0.08f), skin).name = "Skin";
            Gen.Prim(PrimitiveType.Capsule, p, new Vector3(0.2f, 0.36f, 0f), new Vector3(0.08f, 0.1f, 0.08f), coat, new Vector3(0f, 0f, 50f)).name = "Coat";
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(0.27f, 0.42f, 0f), new Vector3(0.08f, 0.08f, 0.08f), skin).name = "Skin";
            Gen.Prim(PrimitiveType.Sphere, p, new Vector3(0.3f, 0.34f, -0.02f), new Vector3(0.12f, 0.14f, 0.12f), Gen.Hex("FFC857"), default, 2.5f);

            // Head: big and round, the cartoon proportion.
            Transform head = Gen.Go("Head", p).transform;
            head.localPosition = new Vector3(0f, 0.64f, 0f);
            head.localScale = Vector3.one * 0.8f;
            Gen.Prim(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.44f, 0.42f, 0.42f), skin).name = "Skin";
            // Eyes: dark ovals with a bright glint.
            for (int side = -1; side <= 1; side += 2)
            {
                Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0.08f * side, 0.02f, -0.19f), new Vector3(0.07f, 0.095f, 0.04f), ink);
                Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0.08f * side + 0.015f, 0.045f, -0.21f), new Vector3(0.025f, 0.025f, 0.01f), Color.white, default, 1.5f);
                Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0.14f * side, -0.06f, -0.17f), new Vector3(0.07f, 0.04f, 0.02f), Gen.Hex("F28B82")); // cheek
            }

            Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0f, -0.09f, -0.2f), new Vector3(0.08f, 0.03f, 0.02f), Gen.Hex("7A2E3A")); // smile
            // Hair: a cap over the back and top of the head, with a little tuft.
            Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0f, 0.07f, 0.03f), new Vector3(0.46f, 0.36f, 0.42f), hair).name = "Hair";
            Gen.Prim(PrimitiveType.Sphere, head, new Vector3(0.05f, 0.22f, -0.04f), new Vector3(0.12f, 0.1f, 0.12f), hair, new Vector3(0f, 0f, -30f)).name = "Hair";
        }

        /// <summary>Night Rescue props: castaway rafts, sky lanterns, lighthouse islands and village houses.</summary>
        private void CreateRescueProps()
        {
            Color wood = Gen.Hex("8A5A36");

            GameObject raft = Gen.Go("Rescue_Castaway");
            for (int i = -1; i <= 1; i++)
            {
                Gen.Prim(PrimitiveType.Cylinder, raft.transform, new Vector3(i * 0.22f, 0.05f, 0f), new Vector3(0.2f, 0.45f, 0.2f), wood * (1f - 0.08f * (i + 1)), new Vector3(90f, 0f, 0f));
            }

            Person(raft.transform, new Vector3(0f, 0.12f, 0f), 1f, Gen.Hex("E07A5F"));
            Gen.Prim(PrimitiveType.Cylinder, raft.transform, new Vector3(-0.3f, 0.6f, 0f), new Vector3(0.03f, 0.5f, 0.03f), wood);
            Gen.Prim(PrimitiveType.Cube, raft.transform, new Vector3(-0.15f, 1.0f, 0f), new Vector3(0.28f, 0.16f, 0.02f), Gen.Hex("F4F1DE"), default, 0.6f); // little flag
            Gen.Prim(PrimitiveType.Sphere, raft.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.4f, 1.4f), Color.white)
                .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.8f, 0.4f, 0.18f)); // lantern halo
            RescueCastaway = Gen.SavePrefabObject(raft, "Rescue", "Rescue_Castaway");

            GameObject rider = Gen.Go("Rescue_Passenger");
            Person(rider.transform, Vector3.zero, 0.8f, Gen.Hex("81B29A"));
            RescuePassenger = Gen.SavePrefabObject(rider, "Rescue", "Rescue_Passenger");

            // Sky lantern: a round paper lantern with red caps, a warm glowing body and a soft bloom.
            GameObject lantern = Gen.Go("Rescue_Lantern");
            Gen.Prim(PrimitiveType.Sphere, lantern.transform, Vector3.zero, new Vector3(0.5f, 0.6f, 0.5f), Gen.Hex("FFB347"), default, 1.8f);
            Gen.Prim(PrimitiveType.Sphere, lantern.transform, Vector3.zero, new Vector3(0.52f, 0.1f, 0.52f), Gen.Hex("FF8A3D"), default, 1.2f); // rib
            Gen.Prim(PrimitiveType.Cylinder, lantern.transform, new Vector3(0f, 0.31f, 0f), new Vector3(0.26f, 0.04f, 0.26f), Gen.Hex("C0392B"), default, 0.6f);
            Gen.Prim(PrimitiveType.Cylinder, lantern.transform, new Vector3(0f, -0.31f, 0f), new Vector3(0.22f, 0.04f, 0.22f), Gen.Hex("C0392B"), default, 0.6f);
            Gen.Prim(PrimitiveType.Sphere, lantern.transform, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f), Color.white)
                .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.7f, 0.3f, 0.35f));
            RescueLantern = Gen.SavePrefabObject(lantern, "Rescue", "Rescue_Lantern");

            // Island: rocky mound, a beach, a dock and a dark lighthouse whose "Lit" child switches on at rescue.
            GameObject island = Gen.Go("Rescue_Island");
            Transform t = island.transform;
            Meshes.Part(Meshes.Rock(91, 0.55f), t, new Vector3(0f, 0f, 0f), new Vector3(6.5f, 2.6f, 4.5f), Gen.Hex("4E5670"));
            Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.2f, -0.5f), new Vector3(8f, 1.4f, 5f), Gen.Hex("D9C391"));
            Meshes.Part(Meshes.Rock(92), t, new Vector3(2.3f, 1.2f, 0.6f), new Vector3(1.4f, 1.3f, 1.2f), Gen.Hex("5B6480"));
            for (int i = 0; i < 4; i++)
            {
                Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.8f, 2.0f + i * 1.0f, 0.4f), new Vector3(0.95f - i * 0.1f, 0.5f, 0.95f - i * 0.1f),
                    i % 2 == 0 ? Gen.Hex("E8E4DA") : Gen.Hex("B8423A"));
            }

            Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.8f, 6.1f, 0.4f), new Vector3(0.62f, 0.32f, 0.62f), Gen.Hex("2A2F45")); // dark lamp room
            Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.8f, 6.55f, 0.4f), new Vector3(0.8f, 0.1f, 0.8f), Gen.Hex("222233"));
            for (int i = 0; i < 4; i++)
            {
                Gen.Prim(PrimitiveType.Cube, t, new Vector3(2.8f + i * 0.5f, 0.35f, -1.8f), new Vector3(0.45f, 0.08f, 1.1f), Gen.Hex("7A5230")); // dock planks
            }

            Transform lit = Gen.Go("Lit", t).transform;
            Gen.Prim(PrimitiveType.Cylinder, lit, new Vector3(-0.8f, 6.1f, 0.4f), new Vector3(0.66f, 0.34f, 0.66f), Gen.Hex("FFE9A8"), default, 3.5f);
            Gen.Prim(PrimitiveType.Sphere, lit, new Vector3(-0.8f, 6.1f, 0.4f), new Vector3(4f, 4f, 4f), Color.white)
                .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.92f, 0.6f, 0.25f));
            Gen.Prim(PrimitiveType.Cube, lit, new Vector3(-7f, 6.1f, 0.4f), new Vector3(12f, 1.2f, 0.05f), Color.white, new Vector3(0f, 0f, 4f))
                .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.95f, 0.7f, 0.16f)); // beam
            for (int i = 0; i < 3; i++)
            {
                Person(lit, new Vector3(3.2f + i * 0.45f, 0.4f, -1.8f), 0.9f, i == 1 ? Gen.Hex("F2CC8F") : Gen.Hex("81B29A")); // waving villagers
            }

            RescueIsland = Gen.SavePrefabObject(island, "Rescue", "Rescue_Island");

            // A fisher's cottage: fieldstone footing, lime-washed timber walls, a steep thatched cone roof,
            // a chimney, a plank door and a warm lit window.
            GameObject house = Gen.Go("Village_House");
            Transform ht = house.transform;
            Gen.Prim(PrimitiveType.Cylinder, ht, new Vector3(0f, 0.06f, 0f), new Vector3(0.86f, 0.06f, 0.86f), Gen.Hex("7D7A76"));
            Gen.Prim(PrimitiveType.Cylinder, ht, new Vector3(0f, 0.34f, 0f), new Vector3(0.76f, 0.24f, 0.76f), Gen.Hex("E6DCC8"));
            Gen.Prim(PrimitiveType.Cylinder, ht, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.03f, 0.8f), Gen.Hex("5B4130")); // eave beam
            Meshes.Part(Meshes.Cone(), ht, new Vector3(0f, 0.6f, 0f), new Vector3(0.56f, 0.62f, 0.56f), Gen.Hex("9C7A4E")); // thatch
            Meshes.Part(Meshes.Cone(), ht, new Vector3(0f, 1.16f, 0f), new Vector3(0.1f, 0.12f, 0.1f), Gen.Hex("5B4130"));
            Gen.Prim(PrimitiveType.Cube, ht, new Vector3(0.2f, 0.95f, 0.1f), new Vector3(0.1f, 0.3f, 0.1f), Gen.Hex("6E6A66")); // chimney
            Gen.Prim(PrimitiveType.Cube, ht, new Vector3(0f, 0.25f, -0.37f), new Vector3(0.18f, 0.3f, 0.04f), Gen.Hex("5B4130")); // door
            Gen.Prim(PrimitiveType.Cube, ht, new Vector3(0.22f, 0.4f, -0.31f), new Vector3(0.13f, 0.12f, 0.04f), Gen.Hex("FFC870"), new Vector3(0f, -35f, 0f), 2.5f); // window
            VillageHouse = Gen.SavePrefabObject(house, "Rescue", "Village_House");
        }

        /// <summary>Props for Moonlight Sail: sea rocks, floating stars, coins, moonstones and the harbor lighthouse.</summary>
        private void CreateSailProps()
        {
            // Rock: a cluster of faceted boulders, mostly below the waterline so only the crown shows.
            GameObject rock = Gen.Go("Sail_Rock");
            Color rockColor = Gen.Hex("50545E"); // wet basalt
            Meshes.Part(Meshes.Rock(71, 1.15f), rock.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.05f, 1.2f, 1.0f), rockColor);
            Meshes.Part(Meshes.Rock(72), rock.transform, new Vector3(0.6f, -0.1f, 0.3f), new Vector3(0.6f, 0.7f, 0.6f), rockColor * 0.9f, new Vector3(0f, 40f, 0f));
            Meshes.Part(Meshes.Rock(73), rock.transform, new Vector3(-0.55f, -0.2f, -0.2f), new Vector3(0.5f, 0.55f, 0.55f), rockColor * 0.85f, new Vector3(0f, 110f, 0f));
            Gen.Prim(PrimitiveType.Cylinder, rock.transform, new Vector3(0f, -0.28f, 0f), new Vector3(2.2f, 0.02f, 2.2f), Gen.Hex("E8F4FF"), default, 0.35f); // foam ring
            SailRock = Gen.SavePrefabObject(rock, "Sail", "Sail_Rock");

            GameObject star = Gen.Go("Sail_Star");
            GameObject starBody = Gen.Prim(PrimitiveType.Cube, star.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.14f), Gen.Hex("FFD95C"), new Vector3(0f, 0f, 45f), 1.1f);
            Gen.Prim(PrimitiveType.Cube, starBody.transform, Vector3.zero, new Vector3(1f, 1f, 1f), Gen.Hex("FFE9A0"), new Vector3(0f, 0f, 45f), 1.1f);
            Gen.Add<MoonPull.Gameplay.Visuals.Spinner>(starBody); // on a child: the sail owns the root position
            SailStar = Gen.SavePrefabObject(star, "Sail", "Sail_Star");

            GameObject coin = Gen.Go("Sail_Coin");
            GameObject coinSpin = Gen.Go("Spin", coin.transform);
            Gen.Prim(PrimitiveType.Cylinder, coinSpin.transform, Vector3.zero, new Vector3(0.5f, 0.05f, 0.5f), Gen.Hex("F5B642"), new Vector3(90f, 0f, 0f), 0.8f);
            Gen.Add<MoonPull.Gameplay.Visuals.Spinner>(coinSpin);
            SailCoin = Gen.SavePrefabObject(coin, "Sail", "Sail_Coin");

            GameObject stone = Gen.Go("Sail_Moonstone");
            GameObject stoneBody = Gen.Prim(PrimitiveType.Sphere, stone.transform, Vector3.zero, new Vector3(0.5f, 0.65f, 0.5f), Gen.Hex("BDE6FF"), default, 1.6f);
            Gen.Add<MoonPull.Gameplay.Visuals.Spinner>(stoneBody);
            SailMoonstone = Gen.SavePrefabObject(stone, "Sail", "Sail_Moonstone");

            // Lighthouse on a rocky islet: the finish line of every voyage.
            GameObject house = Gen.Go("Sail_Lighthouse");
            Transform h = house.transform;
            Meshes.Part(Meshes.Rock(81, 0.6f), h, new Vector3(0f, -0.3f, 0f), new Vector3(3.2f, 1.6f, 3f), Gen.Hex("5B5E78"));
            for (int i = 0; i < 4; i++)
            {
                Gen.Prim(PrimitiveType.Cylinder, h, new Vector3(0f, 1.1f + i * 1.1f, 0f), new Vector3(1.1f - i * 0.12f, 0.55f, 1.1f - i * 0.12f),
                    i % 2 == 0 ? Gen.Hex("F4F1EA") : Gen.Hex("D9534F"));
            }

            Gen.Prim(PrimitiveType.Cylinder, h, new Vector3(0f, 5.5f, 0f), new Vector3(0.75f, 0.35f, 0.75f), Gen.Hex("FFE9A8"), default, 3f);
            Gen.Prim(PrimitiveType.Cylinder, h, new Vector3(0f, 6.0f, 0f), new Vector3(0.95f, 0.12f, 0.95f), Gen.Hex("333344"));
            Gen.Prim(PrimitiveType.Sphere, h, new Vector3(0f, 5.5f, 0f), new Vector3(2.2f, 2.2f, 2.2f), new Color(1f, 0.95f, 0.7f), default, 0.6f)
                .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.93f, 0.6f, 0.25f));
            SailLighthouse = Gen.SavePrefabObject(house, "Sail", "Sail_Lighthouse");
        }

        /// <summary>
        /// A column of stacked faceted boulders filling the space an obstacle's collider covers, growing down into the
        /// sea (direction -1) or up into the sky (+1) from the pivot. Replaces the old flat boxes.
        /// </summary>
        private static void RockColumn(Transform v, Color color, int seed, float direction)
        {
            float y = direction < 0f ? -1.5f : 1.4f;
            for (int i = 0; i < 5; i++)
            {
                float size = 1f + i * 0.12f;
                Color c = Color.Lerp(color, color * 0.8f, i / 4f);
                c.a = 1f;
                Meshes.Part(Meshes.Rock(seed * 10 + i, 1.1f), v, new Vector3(((i * 37) % 5 - 2) * 0.05f, y, 0f),
                    new Vector3(0.95f * size, 1.05f, 0.85f * size), c, new Vector3(0f, i * 47f, i % 2 == 0 ? 6f : -5f));
                y += 1.55f * direction;
            }
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
                    RockColumn(v, dark, 1, -1f);
                    Meshes.Part(Meshes.Rock(11, 1.2f), v, new Vector3(0.05f, -0.55f, 0f), new Vector3(0.62f, 0.55f, 0.7f), color, new Vector3(0f, 25f, 8f));
                    Meshes.Part(Meshes.Rock(12), v, new Vector3(-0.5f, -0.85f, 0.15f), new Vector3(0.42f, 0.4f, 0.5f), color * 0.92f, new Vector3(0f, 60f, 0f));
                    break;
                case 1: // reef spires
                    RockColumn(v, dark, 2, -1f);
                    for (int i = -1; i <= 1; i++)
                    {
                        float h = i == 0 ? 0.9f : 0.6f;
                        Gen.Prim(PrimitiveType.Cylinder, v, new Vector3(i * 0.55f, -0.6f - (0.9f - h) * 0.5f, 0f), new Vector3(0.28f, h * 0.5f, 0.28f), color);
                    }

                    break;
                default: // wreck hull
                    RockColumn(v, dark, 3, -1f);
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
                    Meshes.Part(Meshes.Rock(41, 0.45f), v, new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.8f, 1.3f), color);
                    RockColumn(v, dark, 5, 1f);
                    break;
                case 1: // cave ceiling
                    RockColumn(v, dark, 4, 1f);
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
                new RegionSpec { Id = "tropical_lagoon", Stars = 0, SkyTop = "0B1433", SkyBottom = "3A5089", Shallow = "3B93B5", Deep = "0B2A4C", Foam = "DCEAF2", Fog = "3A5089", Rock = "5E6C8C", Bridge = "A67C52", Signature = "FF8FA3", SignatureKind = PlacementKind.LowObstacle, Idle = 60, Root = 261.63f, CostScale = 1f },
                new RegionSpec { Id = "frozen_north", Stars = 35, SkyTop = "0E2140", SkyBottom = "5C7FB0", Shallow = "A8E6FF", Deep = "1E3F66", Foam = "FFFFFF", Fog = "B8C8DC", Rock = "8FA3BF", Bridge = "D8EEFF", Signature = "BFEFFF", SignatureKind = PlacementKind.HighObstacle, Idle = 90, Root = 293.66f, CostScale = 1.75f },
                new RegionSpec { Id = "volcanic_isles", Stars = 80, SkyTop = "2A0F2E", SkyBottom = "C97A6A", Shallow = "F2A279", Deep = "4A1E2E", Foam = "FFE6C7", Fog = "8A5A5A", Rock = "3E3440", Bridge = "6B4A3A", Signature = "FF6A3D", SignatureKind = PlacementKind.LowObstacle, Idle = 130, Root = 220f, CostScale = 2.5f },
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
                    "music", Synth.Music("music_v2_" + spec.Id, spec.Root, true),
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
                new BoatSpec { Id = "dinghy", Hull = "7A4A2E", Sail = "EDE3CC", Rarity = BoatRarity.Common, Price = 0, Perk = BoatPerkType.None, Shape = 0 },
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
            // Everything sits under one scaled node: the boat reads clearly on a phone without touching the hitbox.
            Transform t = Gen.Go("Body", root.transform).transform;
            t.localScale = Vector3.one * 1.35f;
            Color trim = Color.Lerp(hull, Color.white, 0.55f);
            Color wood = Gen.Hex("6B4A34");
            Color dark = hull * 0.6f;
            dark.a = 1f;

            // Hull with a raised bow, a contrasting gunwale stripe and a waterline band.
            Meshes.Part(Meshes.Hull(1.7f, 0.78f, 0.42f, 0.22f), t, new Vector3(0f, 0.1f, 0f), Vector3.one, hull);
            Meshes.Part(Meshes.Hull(1.72f, 0.8f, 0.08f, 0.22f), t, new Vector3(0f, 0.12f, 0f), Vector3.one, trim);
            Meshes.Part(Meshes.Hull(1.5f, 0.7f, 0.12f, 0.1f), t, new Vector3(0f, -0.14f, 0f), Vector3.one, dark);
            Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.05f, 0.1f, 0f), new Vector3(1.3f, 0.03f, 0.6f), wood);

            switch (shape)
            {
                case 1: // cabin boat with a warm lit window and a funnel
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.25f, 0.38f, 0f), new Vector3(0.62f, 0.52f, 0.56f), trim);
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.25f, 0.68f, 0f), new Vector3(0.72f, 0.08f, 0.64f), hull);
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(0.07f, 0.42f, 0f), new Vector3(0.02f, 0.18f, 0.36f), Gen.Hex("FFD37A"), default, 1.2f);
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.38f, 0.92f, 0f), new Vector3(0.14f, 0.2f, 0.14f), sail);
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.38f, 1.12f, 0f), new Vector3(0.16f, 0.03f, 0.16f), Gen.Hex("333344"));
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.62f, 0.32f, 0f), new Vector3(0.08f, 0.08f, 0.08f), Gen.Hex("FFE9A8"), default, 2f);
                    break;
                case 2: // rounded novelty: big friendly head and a pennant
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.5f, 0.55f, 0f), new Vector3(0.55f, 0.52f, 0.5f), sail);
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.64f, 0.62f, 0.18f), new Vector3(0.09f, 0.09f, 0.05f), Gen.Hex("1B1B2A"));
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.64f, 0.62f, -0.18f), new Vector3(0.09f, 0.09f, 0.05f), Gen.Hex("1B1B2A"));
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(0.8f, 0.5f, 0f), new Vector3(0.18f, 0.06f, 0.2f), Gen.Hex("FF9F1C"));
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.35f, 0.75f, 0f), new Vector3(0.04f, 0.6f, 0.04f), wood);
                    Meshes.Part(Meshes.Sail(0.35f, 0.4f, 0.04f), t, new Vector3(-0.33f, 1.0f, 0f), Vector3.one, sail * 0.9f + Color.white * 0.1f);
                    break;
                default: // sloop: tall mast, billowing mainsail and jib, a pennant at the top
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(0.05f, 1.05f, 0f), new Vector3(0.05f, 0.95f, 0.05f), wood);
                    Gen.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.3f, 0.32f, 0f), new Vector3(0.035f, 0.36f, 0.035f), wood, new Vector3(0f, 0f, 90f));
                    Meshes.Part(Meshes.Sail(1.55f, 0.72f, 0.16f), t, new Vector3(0.02f, 0.32f, 0f), new Vector3(-1f, 1f, 1f), sail);
                    Meshes.Part(Meshes.Sail(1.3f, 0.62f, 0.1f), t, new Vector3(0.1f, 0.3f, 0f), Vector3.one, Color.Lerp(sail, hull, 0.25f));
                    Gen.Prim(PrimitiveType.Cube, t, new Vector3(-0.07f, 2.02f, 0f), new Vector3(0.24f, 0.1f, 0.02f), Gen.Hex("FF5A5A"), default, 0.5f);
                    Gen.Prim(PrimitiveType.Sphere, t, new Vector3(0.05f, 1.98f, 0f), new Vector3(0.06f, 0.06f, 0.06f), Gen.Hex("FFE9A8"), default, 2f);
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
