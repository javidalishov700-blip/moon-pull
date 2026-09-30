using System.Collections.Generic;
using MoonPull.Ads;
using MoonPull.Analytics;
using MoonPull.Audio;
using MoonPull.Boat;
using MoonPull.Boot;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Core.Pooling;
using MoonPull.Core.Simulation;
using MoonPull.Core.TimeControl;
using MoonPull.Feedback;
using MoonPull.Gameplay.CameraControl;
using MoonPull.Gameplay.Visuals;
using MoonPull.Layers.Boss;
using MoonPull.Layers.Creatures;
using MoonPull.Layers.Passengers;
using MoonPull.Layers.Weather;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Meta;
using MoonPull.Rewind;
using MoonPull.Tutorial;
using MoonPull.Water;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Builds Main.unity from nothing: systems, gameplay world, camera, lights and UI, with every reference wired.
    /// Mirrors Docs/04_EditorSetup.md, which now describes what this code produces.
    /// </summary>
    internal static class SceneFactory
    {
        public const string ScenePath = Gen.Root + "/Main.unity";

        /// <summary>Everything the UI factory needs to wire screens.</summary>
        internal sealed class World
        {
            public Content Content;
            public GameManager GameManager;
            public TimeScaleController TimeScale;
            public MetaGame Meta;
            public AdsCoordinator Ads;
            public LevelSession Session;
            public MoonPull.Rescue.NightRescue Rescue;
            public MoonPull.Rescue.VillagePins Pins;
            public LevelRunner Runner;
            public BoatController Boat;
            public FullMoonMode FullMoon;
            public PassengerSystem Passengers;
            public KrakenBoss Kraken;
            public RewindController Rewind;
            public ObstacleInteractionSystem Obstacles;
            public Camera Camera;
            public HandHint Hand;
            public Transform LoadingOverlay;
            public MoonPull.Rescue.VillageDirector Village;
        }

        public static string Build(Content content)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var w = new World { Content = content };

            // ------------------------------------------------ systems
            Transform systems = Gen.Go("[Systems]").transform;
            GameObject gm = Gen.Go("GameManager", systems);
            w.GameManager = Gen.Add<GameManager>(gm);
            Gen.Add<AppLifecycle>(gm);
            w.TimeScale = Gen.Add<TimeScaleController>(gm);
            Gen.Set(w.TimeScale, "config", content.Game);

            w.Meta = Gen.Add<MetaGame>(Gen.Go("Meta", systems));
            Gen.Wire(w.Meta, "economy", content.Economy, "generation", content.LevelGen, "regions", content.Regions,
                "boats", content.Boats, "missions", content.Missions);

            GameObject adsGo = Gen.Go("Ads", systems);
            w.Ads = Gen.Add<AdsCoordinator>(adsGo);
            MockAdOverlay mockOverlay = Gen.Add<MockAdOverlay>(adsGo);
            Gen.Wire(w.Ads, "config", content.Ads, "gameManager", w.GameManager, "timeScale", w.TimeScale);

            PoolManager pools = Gen.Add<PoolManager>(Gen.Go("Pools", systems));
            Gen.Set(pools, "warmup", content.Warmup);

            // ------------------------------------------------ camera, sky, light
            GameObject cameraGo = Gen.Go("Main Camera");
            cameraGo.tag = "MainCamera";
            w.Camera = Gen.Add<Camera>(cameraGo);
            w.Camera.fieldOfView = 60f;
            w.Camera.nearClipPlane = 0.3f;
            w.Camera.farClipPlane = 120f;
            w.Camera.clearFlags = CameraClearFlags.Skybox;
            Gen.Add<AudioListener>(cameraGo);
            // Bloom + grade + vignette on the 3D view (the overlay UI stays crisp on top).
            var post = Gen.Add<MoonPull.Gameplay.Visuals.MoonPullPost>(cameraGo);
            Gen.Set(post, "shader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/MoonPull/Art/Shaders/MoonPullPost.shader"));
            cameraGo.transform.position = content.Camera.Offset;

            GameObject lightGo = Gen.Go("Moonlight");
            var light = Gen.Add<Light>(lightGo);
            light.type = LightType.Directional;
            light.color = Gen.Hex("C9D6FF");
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            RenderSettings.skybox = Art.Sky();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Gen.Hex("5A5F8C");
            RenderSettings.fog = false;

            // ------------------------------------------------ gameplay
            Transform gameplay = Gen.Go("[Gameplay]").transform;
            SimulationLoop loop = Gen.Add<SimulationLoop>(Gen.Go("Simulation", gameplay));
            Gen.Set(loop, "config", content.Game);

            MoonController moon = Gen.Add<MoonController>(Gen.Go("Moon", gameplay));
            Gen.Set(moon, "config", content.Moon);

            TideModel tide = Gen.Add<TideModel>(Gen.Go("Tide", gameplay));
            Gen.Wire(tide, "config", content.Tide, "moon", moon);

            GameObject waterGo = Gen.Go("Water", gameplay);
            WaterSurface water = Gen.Add<WaterSurface>(waterGo);
            Seabed seabed = Gen.Add<Seabed>(waterGo);
            Gen.Set(seabed, "config", content.Seabed);
            GameObject waterMesh = Gen.Go("WaterMesh", waterGo.transform);
            Gen.Add<MeshFilter>(waterMesh).sharedMesh = Art.WaterMesh(90f, 75f, 1f, -22f);
            MeshRenderer waterRenderer = Gen.Add<MeshRenderer>(waterMesh);
            waterRenderer.sharedMaterial = Art.Water();
            waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Gen.Wire(water, "config", content.Water, "tide", tide, "follow", cameraGo.transform,
                "surfaceMesh", waterMesh.transform, "surfaceRenderer", waterRenderer);

            GameObject boatGo = Gen.Go("Boat", gameplay);
            w.Boat = Gen.Add<BoatController>(boatGo);
            BoatView boatView = Gen.Add<BoatView>(boatGo);
            Transform modelRoot = Gen.Go("ModelRoot", boatGo.transform).transform;
            Gen.Wire(w.Boat, "config", content.Boat, "water", water, "seabed", seabed);
            Gen.Wire(boatView, "controller", w.Boat, "modelRoot", modelRoot, "pools", pools, "debrisPrefab", content.Debris);

            // Foam wake streaming from the stern: sells speed and makes the boat read as sitting in the water.
            GameObject wakeGo = Gen.Go("Wake", boatGo.transform);
            wakeGo.transform.localPosition = new Vector3(-1.1f, -0.05f, 0f);
            var wake = wakeGo.AddComponent<TrailRenderer>();
            wake.time = 0.9f;
            wake.minVertexDistance = 0.12f;
            wake.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0.05f));
            wake.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.75f, 0.9f, 1f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            wake.sharedMaterial = Art.Glow(Color.white);
            wake.alignment = LineAlignment.View;
            wake.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            CameraRig rig = Gen.Add<CameraRig>(cameraGo);
            Gen.Wire(rig, "config", content.Camera, "boat", w.Boat);

            // Moon visual rides with the camera so it always hangs in the sky.
            Transform moonAnchor = Gen.Go("MoonAnchor", cameraGo.transform).transform;
            moonAnchor.localPosition = new Vector3(2.2f, 0f, 26f);
            GameObject moonVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(moonVisual.GetComponent<Collider>());
            moonVisual.name = "MoonVisual";
            moonVisual.transform.SetParent(moonAnchor, false);
            moonVisual.transform.localScale = Vector3.one * 2.6f;
            var moonRenderer = moonVisual.GetComponent<MeshRenderer>();
            moonRenderer.sharedMaterial = Art.Moon();
            var trail = Gen.Add<TrailRenderer>(moonVisual);
            trail.time = 0.6f;
            trail.startWidth = 0.9f;
            trail.endWidth = 0f;
            trail.sharedMaterial = Art.Glow(new Color(0.85f, 0.9f, 1f, 0.5f));
            trail.emitting = false;
            var moonLight = Gen.Add<Light>(moonVisual);
            moonLight.type = LightType.Point;
            moonLight.range = 30f;
            moonLight.intensity = 0.6f;
            moonLight.color = Gen.Hex("DDE6FF");
            MoonView moonView = Gen.Add<MoonView>(moonVisual);
            Gen.Wire(moonView, "config", content.Moon, "moon", moon, "moonTransform", moonVisual.transform,
                "starTrail", trail, "moonRenderer", moonRenderer, "moonLight", moonLight);

            GameObject levelGo = Gen.Go("Level", gameplay);
            w.Session = Gen.Add<LevelSession>(levelGo);
            w.Runner = Gen.Add<LevelRunner>(levelGo);
            w.Obstacles = Gen.Add<ObstacleInteractionSystem>(levelGo);
            PickupSystem pickups = Gen.Add<PickupSystem>(levelGo);
            WaveLauncher launcher = Gen.Add<WaveLauncher>(levelGo);
            w.FullMoon = Gen.Add<FullMoonMode>(levelGo);
            ScoreSystem score = Gen.Add<ScoreSystem>(levelGo);
            w.Rewind = Gen.Add<RewindController>(levelGo);

            GameObject layersGo = Gen.Go("Layers", gameplay);
            w.Passengers = Gen.Add<PassengerSystem>(layersGo);
            WeatherSystem weather = Gen.Add<WeatherSystem>(layersGo);
            CreatureSystem creatures = Gen.Add<CreatureSystem>(layersGo);
            w.Kraken = Gen.Add<KrakenBoss>(layersGo);

            Gen.Wire(w.Runner, "config", content.Runner, "pools", pools, "prefabs", content.Prefabs, "boat", w.Boat, "seabed", seabed);
            Gen.Wire(w.Obstacles, "config", content.NearMiss, "scoreConfig", content.Score, "runner", w.Runner, "boat", w.Boat, "score", score);
            Gen.Wire(pickups, "config", content.Pickup, "runner", w.Runner, "boat", w.Boat, "score", score, "fullMoon", w.FullMoon);
            Gen.Wire(launcher, "config", content.WaveLaunch, "moon", moon, "boat", w.Boat, "water", water, "score", score);
            Gen.Wire(w.FullMoon, "config", content.FullMoon, "boat", w.Boat, "score", score, "water", water, "moonView", moonView);
            Gen.Set(score, "config", content.Score);
            Gen.Wire(w.Passengers, "config", content.Passenger, "runner", w.Runner, "boat", w.Boat, "tide", tide, "score", score);
            Gen.Wire(weather, "config", content.Weather, "runner", w.Runner, "boat", w.Boat, "tide", tide, "water", water,
                "moon", moon, "moonView", moonView);
            Gen.Wire(creatures, "config", content.Creature, "runner", w.Runner, "boat", w.Boat, "tide", tide);
            Gen.Wire(w.Kraken, "config", content.Boss, "runner", w.Runner, "boat", w.Boat, "tide", tide, "score", score);
            Gen.Wire(w.Session,
                "generation", content.LevelGen, "scoreConfig", content.Score, "tideConfig", content.Tide, "boatConfig", content.Boat,
                "regions", content.Regions, "boats", content.Boats, "loop", loop, "moon", moon, "tide", tide, "water", water,
                "boat", w.Boat, "boatView", boatView, "runner", w.Runner, "obstacles", w.Obstacles, "pickups", pickups,
                "fullMoon", w.FullMoon, "launcher", launcher, "score", score, "cameraRig", rig);
            Gen.Wire(w.Rewind, "config", content.Rewind, "loop", loop, "moon", moon, "tide", tide, "water", water, "boat", w.Boat,
                "fullMoon", w.FullMoon, "kraken", w.Kraken, "runner", w.Runner, "obstacles", w.Obstacles, "cameraRig", rig);

            // Night Rescue is the game: it owns the boat, the sea swells, the camera and the run. The legacy tide systems
            // stay in the scene only for the sea surface mesh, the score and the meta flows that reference them.
            var sail = Gen.Add<MoonPull.Rescue.NightRescue>(Gen.Go("NightRescue", gameplay));
            Transform sailBoat = Gen.Go("RescueBoat", gameplay).transform;
            sailBoat.localScale = Vector3.one * 1.4f; // a chunkier, friendlier boat that reads well on a phone
            Gen.Wire(sail, "session", w.Session, "score", score, "water", water, "cameraRig", rig,
                "cameraTransform", cameraGo.transform, "moonAnchor", moonAnchor, "legacyBoat", boatGo, "boatRoot", sailBoat,
                "castawayPrefab", content.RescueCastaway, "passengerPrefab", content.RescuePassenger,
                "lanternPrefab", content.RescueLantern, "islandPrefab", content.RescueIsland, "rockPrefab", content.SailRock,
                "defaultBoatModel", AssetDatabase.LoadAssetAtPath<GameObject>(Gen.Root + "/Boats/BoatModel_dinghy.prefab"));
            Gen.Set(w.Session, "rescue", sail);
            w.Rescue = sail;

            Gen.SetArray<MonoBehaviour>(loop, "tickables", new MonoBehaviour[] { sail });

            // ------------------------------------------------ visuals, audio, feedback
            GameObject visuals = Gen.Go("Visuals", gameplay);
            RegionAmbience ambience = Gen.Add<RegionAmbience>(visuals);
            Gen.Wire(ambience, "session", w.Session, "meta", w.Meta, "water", water, "weather", weather, "gameplayCamera", w.Camera);

            GameObject island = Gen.Go("LighthouseIsland", visuals.transform);
            island.transform.position = new Vector3(8f, 0.2f, 10f); // ground above the calm sea
            Gen.SetArray<GameObject>(sail, "hideWhileSailing", new[] { island });
            var village = Gen.Add<MoonPull.Rescue.VillageView>(island);
            var houses = new List<GameObject>();
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 2.39996f; // golden-angle spiral keeps the village tidy as it grows
                float radius = 1.6f + 0.36f * Mathf.Sqrt(i) * 2.2f;
                var house = (GameObject)PrefabUtility.InstantiatePrefab(content.VillageHouse, island.transform);
                house.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius * 1.2f, 0.45f, Mathf.Sin(angle) * radius * 0.6f);
                house.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                houses.Add(house);
            }

            Gen.SetArray<GameObject>(village, "houses", houses);

            // Village buildings: round cartoon halls that grow per level (hall, annex with a lit window, flag and glow).
            Color[] tints = { Gen.Hex("81B29A"), Gen.Hex("E07A5F"), Gen.Hex("F2CC8F"), Gen.Hex("7FA7D9"), Gen.Hex("C39BD3") };
            var tiers = new List<GameObject>();
            var sites = new List<Transform>();
            for (int b = 0; b < 5; b++)
            {
                Transform site = Gen.Go("Building_" + b, island.transform).transform;
                sites.Add(site);
                site.localPosition = new Vector3(-3.2f + b * 1.6f, 0.5f, -1.4f);
                // Village halls: stone footing, timber walls, a steep coloured roof; an annex and then a lit
                // lantern tower as the building levels up.
                GameObject t1 = Gen.Go("Tier1", site);
                Gen.Prim(PrimitiveType.Cylinder, t1.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.15f, 0.08f, 1.05f), Gen.Hex("7D7A76"));
                Gen.Prim(PrimitiveType.Cylinder, t1.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.0f, 0.34f, 0.92f), Gen.Hex("E6DCC8"));
                Gen.Prim(PrimitiveType.Cylinder, t1.transform, new Vector3(0f, 0.8f, 0f), new Vector3(1.05f, 0.03f, 0.97f), Gen.Hex("5B4130"));
                Meshes.Part(Meshes.Cone(), t1.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.72f, 0.8f, 0.68f), Color.Lerp(tints[b], Gen.Hex("3A2A20"), 0.35f));
                Gen.Prim(PrimitiveType.Cube, t1.transform, new Vector3(0f, 0.32f, -0.47f), new Vector3(0.26f, 0.42f, 0.05f), Gen.Hex("5B4130"));
                GameObject t2 = Gen.Go("Tier2", site);
                Gen.Prim(PrimitiveType.Cylinder, t2.transform, new Vector3(0.66f, 0.3f, 0.22f), new Vector3(0.5f, 0.28f, 0.5f), Gen.Hex("DCCFB6"));
                Meshes.Part(Meshes.Cone(), t2.transform, new Vector3(0.66f, 0.58f, 0.22f), new Vector3(0.36f, 0.42f, 0.36f), Gen.Hex("8A6A45"));
                Gen.Prim(PrimitiveType.Cube, t2.transform, new Vector3(-0.28f, 0.5f, -0.44f), new Vector3(0.2f, 0.18f, 0.05f), Gen.Hex("FFC870"), default, 2.5f);
                GameObject t3 = Gen.Go("Tier3", site);
                Gen.Prim(PrimitiveType.Cylinder, t3.transform, new Vector3(0f, 1.75f, 0f), new Vector3(0.03f, 0.2f, 0.03f), Gen.Hex("5B4130"));
                Gen.Prim(PrimitiveType.Cube, t3.transform, new Vector3(0.16f, 1.85f, 0f), new Vector3(0.3f, 0.16f, 0.02f), tints[b], default, 0.8f); // banner
                Gen.Prim(PrimitiveType.Sphere, t3.transform, new Vector3(0f, 1.0f, -0.6f), new Vector3(2.2f, 2.2f, 2.2f), Color.white)
                    .GetComponent<MeshRenderer>().sharedMaterial = Art.Glow(new Color(1f, 0.8f, 0.45f, 0.18f));
                tiers.Add(t1);
                tiers.Add(t2);
                tiers.Add(t3);
            }

            Gen.SetArray<GameObject>(village, "buildingTiers", tiers);

            // Villagers and their requests live on the same island; the Village screen drives it.
            w.Village = Gen.Add<MoonPull.Rescue.VillageDirector>(island);
            Gen.Wire(w.Village, "cameraTransform", cameraGo.transform, "villagerPrefab", content.RescuePassenger, "meta", w.Meta);
            // A wide, flat-topped island so the village has room to grow.
            Gen.Prim(PrimitiveType.Sphere, island.transform, new Vector3(0f, -1.05f, 0f), new Vector3(13f, 3f, 8f), Gen.Hex("5E9A52"));
            Gen.Prim(PrimitiveType.Sphere, island.transform, new Vector3(0f, -1.45f, 0f), new Vector3(14.5f, 2.6f, 9.5f), Gen.Hex("E8D3A0"));
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + 0.4f;
                Palm(island.transform, new Vector3(Mathf.Cos(a) * 5.6f, 0.3f, Mathf.Sin(a) * 3.2f + 0.6f), 1f);
            }

            // Expansion islands for sale around the harbor: bought with coins in the Village screen.
            var owned = new List<GameObject>();
            var forSale = new List<GameObject>();
            var spotRoots = new List<Transform>();
            Vector3[] spots = { new Vector3(-6.2f, 0.2f, 6.5f), new Vector3(6.4f, 0.2f, 6.8f), new Vector3(-2.6f, 0.2f, 11.5f), new Vector3(3.8f, 0.2f, 12.5f) };
            Color[] grass = { Gen.Hex("6FBF73"), Gen.Hex("F2B880"), Gen.Hex("7FC8A9"), Gen.Hex("9FA8DA") };
            for (int i = 0; i < spots.Length; i++)
            {
                Transform root = Gen.Go("Expansion_" + i, island.transform).transform;
                root.localPosition = spots[i];
                spotRoots.Add(root);
                Gen.Prim(PrimitiveType.Sphere, root, new Vector3(0f, -0.9f, 0f), new Vector3(5.2f, 1.9f, 3.8f), Gen.Hex("F2D59A"));
                GameObject built = Gen.Go("Owned", root);
                Gen.Prim(PrimitiveType.Sphere, built.transform, new Vector3(0f, -0.62f, 0f), new Vector3(4.4f, 1.6f, 3.1f), grass[i]);
                for (int h = 0; h < 3; h++)
                {
                    var hut = (GameObject)PrefabUtility.InstantiatePrefab(content.VillageHouse, built.transform);
                    hut.transform.localPosition = new Vector3(-1f + h * 1f, 0.2f, h == 1 ? 0.5f : -0.2f);
                    hut.transform.localScale = Vector3.one * 0.9f;
                }

                Palm(built.transform, new Vector3(1.7f, 0.1f, 0.6f), 0.8f);
                Palm(built.transform, new Vector3(-1.8f, 0.1f, 0.4f), 0.7f);
                GameObject sale = Gen.Go("ForSale", root);
                Transform sign = Gen.Go("Sign", sale.transform).transform;
                sign.localPosition = new Vector3(0f, 0.1f, 0f);
                Gen.Prim(PrimitiveType.Cylinder, sign, new Vector3(0f, 0.4f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Gen.Hex("7A5230"));
                Gen.Prim(PrimitiveType.Sphere, sign, new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 0.9f, 0.12f), Gen.Hex("FFD95C"), default, 0.5f); // coin sign
                Gen.Prim(PrimitiveType.Sphere, sign, new Vector3(0f, 0.95f, -0.06f), new Vector3(0.55f, 0.55f, 0.04f), Gen.Hex("F5B642"), default, 0.4f);
                owned.Add(built);
                forSale.Add(sale);
            }

            Gen.SetArray<GameObject>(village, "islandsOwned", owned);
            Gen.SetArray<GameObject>(village, "islandsForSale", forSale);

            // Tycoon map pins over buildings (coins ready) and islands for sale (price).
            w.Pins = Gen.Add<MoonPull.Rescue.VillagePins>(island);
            Gen.Wire(w.Pins, "cameraTransform", cameraGo.transform, "pinSprite", Art.MapPin, "coinSprite", Art.Coin(),
                "islandSprite", Art.BuildingIcon("island", Gen.Hex("6FBF73")), "font", UiKit.Font);
            Gen.SetArray<Transform>(w.Pins, "buildingSites", sites);
            Gen.SetArray<Transform>(w.Pins, "islandSpots", spotRoots);

            Transform stageRoot = Gen.Go("StageRoot", island.transform).transform;
            stageRoot.localPosition = new Vector3(0.5f, 0.1f, 0f);
            LighthouseIslandView islandView = Gen.Add<LighthouseIslandView>(island);
            Gen.Wire(islandView, "meta", w.Meta, "stageRoot", stageRoot);

            AudioService audioService = BuildAudio(systems, content);
            DynamicWaveAudio waveAudio = Gen.Add<DynamicWaveAudio>(Gen.Go("Waves", audioService.transform));
            AudioSource waveSource = Gen.Add<AudioSource>(waveAudio.gameObject);
            waveSource.playOnAwake = false;
            Gen.Wire(waveAudio, "audioService", audioService, "moon", moon, "loop", waveSource);

            GameFeedbackDirector feedback = Gen.Add<GameFeedbackDirector>(Gen.Go("Feedback", systems));
            Gen.Wire(feedback, "config", content.Feedback, "nearMiss", content.NearMiss, "audioService", audioService,
                "timeScale", w.TimeScale, "cameraRig", rig, "boat", w.Boat, "session", w.Session);

            AnalyticsEventRouter router = Gen.Add<AnalyticsEventRouter>(Gen.Go("Analytics", systems));
            Gen.Wire(router, "session", w.Session, "meta", w.Meta);

            // ------------------------------------------------ UI (needs World)
            UiFactory.Build(w);

            TutorialController tutorial = Gen.Add<TutorialController>(Gen.Go("Tutorial", gameplay));
            Gen.Wire(tutorial, "session", w.Session, "runner", w.Runner, "obstacles", w.Obstacles, "boat", w.Boat, "hand", w.Hand);

            // ------------------------------------------------ boot
            GameObject bootGo = Gen.Go("Boot", systems);
            Bootstrapper bootstrapper = Gen.Add<Bootstrapper>(bootGo);
            FirebaseBootStep firebase = Step<FirebaseBootStep>(bootGo, 4f, false);
            Gen.Wire(firebase, "adConfig", content.Ads, "economyConfig", content.Economy, "iapConfig", content.Iap);
            SaveBootStep saveStep = Step<SaveBootStep>(bootGo, 5f, false);
            Gen.Wire(saveStep, "generation", content.LevelGen, "regions", content.Regions);
            LocalizationAudioBootStep locStep = Step<LocalizationAudioBootStep>(bootGo, 5f, false);
            Gen.Set(locStep, "audioService", audioService);
            MetaBootStep metaStep = Step<MetaBootStep>(bootGo, 3f, false);
            Gen.Set(metaStep, "meta", w.Meta);
            ConsentBootStep consentStep = Step<ConsentBootStep>(bootGo, 0f, true);
            Gen.Set(consentStep, "adConfig", content.Ads);
            AnalyticsConsentBootStep analyticsConsent = Step<AnalyticsConsentBootStep>(bootGo, 2f, false);
            AdsBootStep adsStep = Step<AdsBootStep>(bootGo, 6f, false);
            Gen.Wire(adsStep, "config", content.Ads, "coordinator", w.Ads, "mockOverlay", mockOverlay);
            IapBootStep iapStep = Step<IapBootStep>(bootGo, 8f, false);
            Gen.Wire(iapStep, "config", content.Iap, "meta", w.Meta, "adsCoordinator", w.Ads);
            AnalyticsBindStep bind = Step<AnalyticsBindStep>(bootGo, 1f, false);
            Gen.Set(bind, "router", router);
            RemoteConfigApplyStep remote = Step<RemoteConfigApplyStep>(bootGo, 1f, false);
            Gen.Wire(remote, "adConfig", content.Ads, "economyConfig", content.Economy, "iapConfig", content.Iap,
                "adsCoordinator", w.Ads, "meta", w.Meta, "levelSession", w.Session);

            Gen.Wire(bootstrapper, "config", content.Game, "gameManager", w.GameManager, "loadingOverlay", null);
            Gen.SetArray<BootStep>(bootstrapper, "steps", new BootStep[]
            {
                firebase, saveStep, locStep, metaStep, consentStep, analyticsConsent, adsStep, iapStep, bind, remote
            });

            EditorSceneManager.SaveScene(scene, ScenePath);
            return ScenePath;
        }

        private static T Step<T>(GameObject host, float timeout, bool consent) where T : BootStep
        {
            T step = host.AddComponent<T>();
            Gen.Wire(step, "timeoutSeconds", timeout, "runsInConsentState", consent);
            return step;
        }

        private static AudioService BuildAudio(Transform systems, Content content)
        {
            GameObject root = Gen.Go("Audio", systems);
            AudioService service = Gen.Add<AudioService>(root);

            GameObject music = Gen.Go("Music", root.transform);
            AudioSource musicA = Gen.Add<AudioSource>(music);
            AudioSource musicB = Gen.Add<AudioSource>(music);
            AudioLowPassFilter lowPass = Gen.Add<AudioLowPassFilter>(music);
            lowPass.cutoffFrequency = 22000f;
            foreach (AudioSource source in new[] { musicA, musicB })
            {
                source.playOnAwake = false;
                source.loop = true;
            }

            AudioSource layer = Gen.Add<AudioSource>(Gen.Go("MusicLayer", root.transform));
            layer.playOnAwake = false;
            layer.loop = true;

            GameObject sfx = Gen.Go("Sfx", root.transform);
            var voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = Gen.Add<AudioSource>(sfx);
                voices[i].playOnAwake = false;
            }

            Gen.Wire(service, "library", content.Sfx, "musicA", musicA, "musicB", musicB, "intensityLayer", layer, "musicLowPass", lowPass);
            Gen.SetArray(service, "sfxVoices", voices);
            return service;
        }

        /// <summary>A cartoon palm: a curved trunk and droopy leaf blobs.</summary>
        private static void Palm(Transform parent, Vector3 position, float scale)
        {
            Transform palm = Gen.Go("Palm", parent).transform;
            palm.localPosition = position;
            palm.localScale = Vector3.one * scale;
            Gen.Prim(PrimitiveType.Capsule, palm, new Vector3(0f, 0.8f, 0f), new Vector3(0.16f, 0.85f, 0.16f), Gen.Hex("9C6B43"), new Vector3(0f, 0f, 8f));
            for (int f = 0; f < 5; f++)
            {
                Transform leaf = Gen.Go("Leaf", palm).transform;
                leaf.localPosition = new Vector3(-0.1f, 1.62f, 0f);
                leaf.localEulerAngles = new Vector3(0f, f * 72f, 0f);
                Gen.Prim(PrimitiveType.Sphere, leaf, new Vector3(0.5f, -0.1f, 0f), new Vector3(1.0f, 0.1f, 0.34f), Gen.Hex("58B368"), new Vector3(0f, 0f, -20f));
            }

            Gen.Prim(PrimitiveType.Sphere, palm, new Vector3(-0.05f, 1.55f, -0.08f), new Vector3(0.16f, 0.16f, 0.16f), Gen.Hex("7A4E2D")); // coconut
        }
    }
}
