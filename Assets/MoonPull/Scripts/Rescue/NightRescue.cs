using System.Collections.Generic;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Simulation;
using MoonPull.Gameplay.CameraControl;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Water;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.Rescue
{
    /// <summary>
    /// "Night Rescuer" — the core loop.
    ///
    /// The boat surfs big moonlit swells seen from the side. Hold anywhere and the boat grows heavy and dives into the
    /// trough; release and it rides up the next face and flies. Landing along the slope of a wave is a Perfect landing
    /// (speed); three in a row start a Moon Fever. Castaways drift on rafts with little lanterns: sail through them to
    /// take them aboard, then pass a dark lighthouse island to bring them home. Every lighthouse you light pushes the
    /// night back; when the moonlight runs out the night is over, and everyone you saved moves into your village.
    ///
    /// The sea surface is a sum of three swells along X. The same function is pushed to the water shader, so the boat
    /// rides exactly the waves the player sees.
    /// </summary>
    [DefaultExecutionOrder(1000)] // after WaterSurface.LateUpdate, so our wave globals win
    public sealed class NightRescue : MonoBehaviour, ISimulationTickable
    {
        private enum Kind { Castaway, Lantern, Island, Rock }

        private sealed class Thing
        {
            public Kind Kind;
            public Transform Transform;
            public float X;
            public float Height;
            public bool Done;
            public GameObject Lit;
        }

        [Header("World")]
        [SerializeField] private LevelSession session;
        [SerializeField] private ScoreSystem score;
        [SerializeField] private WaterSurface water;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform moonAnchor;
        [SerializeField] private GameObject legacyBoat;
        [SerializeField] private Transform boatRoot;
        [SerializeField] private GameObject defaultBoatModel;
        [SerializeField] private GameObject[] hideWhileSailing = new GameObject[0];

        [Header("Prefabs")]
        [SerializeField] private GameObject castawayPrefab;
        [SerializeField] private GameObject passengerPrefab;
        [Tooltip("Sea rocks: jump over them or lose speed and a passenger.")]
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private GameObject lanternPrefab;
        [SerializeField] private GameObject islandPrefab;

        [Header("Waves")]
        [SerializeField] private Vector3 amplitudes = new Vector3(1.35f, 0.7f, 0.3f);
        [SerializeField] private Vector3 wavelengths = new Vector3(27f, 12.5f, 5.7f);
        [SerializeField] private Vector3 drift = new Vector3(0.18f, 0.32f, 0.55f);

        [Header("Feel")]
        [SerializeField] private float gravity = 19f;
        [SerializeField] private float diveMultiplier = 3.1f;
        [SerializeField] private float startSpeed = 8f;
        [SerializeField] private float minSpeed = 5f;
        [SerializeField] private float maxSpeed = 24f;
        [SerializeField] private float feverSeconds = 5f;
        [SerializeField] private int capacity = 3;
        [SerializeField] private float nightSeconds = 42f;

        private static readonly int LevelId = Shader.PropertyToID("_MP_WaterLevel");
        private static readonly int TimeId = Shader.PropertyToID("_MP_WaveTime");
        private static readonly int AmpId = Shader.PropertyToID("_MP_WaveAmp");
        private static readonly int KId = Shader.PropertyToID("_MP_WaveK");
        private static readonly int OmegaId = Shader.PropertyToID("_MP_WaveOmega");
        private static readonly int DirXId = Shader.PropertyToID("_MP_WaveDirX");
        private static readonly int DirZId = Shader.PropertyToID("_MP_WaveDirZ");
        private static readonly int PhaseId = Shader.PropertyToID("_MP_WavePhase");
        private static readonly int PulseId = Shader.PropertyToID("_MP_Pulse");
        private static readonly int ScrollId = Shader.PropertyToID("_MP_ScrollZ");
        private static readonly int DarkId = Shader.PropertyToID("_MP_Dark");
        private static readonly int FullMoonId = Shader.PropertyToID("_MP_FullMoon");
        public const string VillageKey = "mp_village_population";

        private readonly List<Thing> things = new List<Thing>();
        private readonly List<GameObject> aboardFigures = new List<GameObject>();
        private System.Random random = new System.Random(1);
        private Vector3 k;
        private Vector3 phase;
        private float ampScale = 1f;
        private float waveTime;

        private Transform boatModel;
        private string boatModelId;

        private bool running;
        private float x, y, vx, vy, speed;
        private bool airborne;
        private float airTime;
        private bool holding;
        private int perfectStreak;
        private float fever;
        private float moonlight;
        private int aboard;
        private int rescued;
        private int lanternsCaught;
        private int lighthousesLit;
        private float nextCastawayAt, nextLanternAt, nextIslandAt, nextRockAt;
        private float startX;
        private float shake;
        private int levelIndex;
        private float runTime;
        private int seats;
        private float nightLength;
        private float progressCheckAt, progressCheckX;
        private float speedBonus;

        private ParticleSystem splash;

        /// <summary>Spray thrown up where the boat lands: bigger for belly flops, golden for Perfect landings.</summary>
        private static Texture2D DropletTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float d = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    texture.SetPixel(px, py, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 3f)));
                }
            }

            texture.Apply();
            return texture;
        }

        private void Splash(int count, Color color)
        {
            if (splash == null)
            {
                var go = new GameObject("Splash");
                go.transform.SetParent(transform, false);
                splash = go.AddComponent<ParticleSystem>();
                splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = splash.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.26f);
                main.gravityModifier = 1.6f;
                main.maxParticles = 400;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                ParticleSystem.EmissionModule emission = splash.emission;
                emission.rateOverTime = 0f;
                ParticleSystem.ShapeModule shape = splash.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = 0.4f;
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                // Round droplets: a soft disc texture instead of the default square particles.
                var material = new Material(Shader.Find("Sprites/Default"));
                material.mainTexture = DropletTexture();
                renderer.sharedMaterial = material;
            }

            splash.transform.position = new Vector3(x, y, -0.3f);
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = true };
            splash.Emit(emit, count);
        }

        /// <summary>Capture player / automation: overrides the touch input while set.</summary>
        public bool? ForcedHold { get; set; }

        public float Moonlight => moonlight;

        /// <summary>Supplies the last night brought home (shown on the win screen).</summary>
        public static int LastSupplies { get; private set; }
        public int Aboard => aboard;

        private void Awake()
        {
            k = new Vector3(2f * Mathf.PI / wavelengths.x, 2f * Mathf.PI / wavelengths.y, 2f * Mathf.PI / wavelengths.z);
            phase = new Vector3(0.3f, 1.7f, 4.1f);
        }

        private void OnEnable() => GameEvents.StateChanged += OnStateChanged;

        private void OnDisable() => GameEvents.StateChanged -= OnStateChanged;

        private void Start()
        {
            if (cameraRig != null)
            {
                cameraRig.enabled = false;
            }

            if (legacyBoat != null)
            {
                legacyBoat.SetActive(false);
            }

            x = 0f;
            y = Height(0f);
        }

        // ------------------------------------------------------------------ the sea

        private float Height(float px)
        {
            float t = waveTime;
            return ampScale * (amplitudes.x * Mathf.Sin(k.x * px - drift.x * t + phase.x)
                             + amplitudes.y * Mathf.Sin(k.y * px - drift.y * t + phase.y)
                             + amplitudes.z * Mathf.Sin(k.z * px - drift.z * t + phase.z));
        }

        private float Slope(float px)
        {
            float t = waveTime;
            return ampScale * (amplitudes.x * k.x * Mathf.Cos(k.x * px - drift.x * t + phase.x)
                             + amplitudes.y * k.y * Mathf.Cos(k.y * px - drift.y * t + phase.y)
                             + amplitudes.z * k.z * Mathf.Cos(k.z * px - drift.z * t + phase.z));
        }

        private void PushWaves()
        {
            Shader.SetGlobalFloat(LevelId, 0f);
            Shader.SetGlobalFloat(TimeId, waveTime);
            Shader.SetGlobalVector(AmpId, new Vector4(amplitudes.x * ampScale, amplitudes.y * ampScale, amplitudes.z * ampScale, 0f));
            Shader.SetGlobalVector(KId, new Vector4(k.x, k.y, k.z, 0f));
            Shader.SetGlobalVector(OmegaId, new Vector4(drift.x, drift.y, drift.z, 0f));
            Shader.SetGlobalVector(DirXId, new Vector4(1f, 1f, 1f, 0f));
            // Crests lean at different angles away from the boat's line (z = 0 is unchanged), so the sea reads as
            // crossing swells instead of straight ridges.
            Shader.SetGlobalVector(DirZId, new Vector4(0.22f, -0.35f, 0.5f, 0f));
            Shader.SetGlobalVector(PhaseId, new Vector4(phase.x, phase.y, phase.z, 0f));
            Shader.SetGlobalVector(PulseId, Vector4.zero);
            Shader.SetGlobalFloat(ScrollId, 0f);
            float dark = running ? Mathf.SmoothStep(0f, 0.72f, Mathf.InverseLerp(0.55f, 0f, moonlight)) : 0f;
            Shader.SetGlobalFloat(DarkId, dark);
            Shader.SetGlobalFloat(FullMoonId, fever > 0f ? 1f : 0f);
        }

        // ------------------------------------------------------------------ run lifecycle

        /// <summary>Called by LevelSession when a night (level) starts.</summary>
        public void Begin(LevelStartArgs args, BoatDefinition boat)
        {
            levelIndex = args.LevelIndex;
            random = new System.Random(4211 + levelIndex * 977);
            ClearThings();
            SetBoat(boat);

            // What the village has built so far shapes tonight's run.
            seats = capacity + VillageService.ExtraSeats;
            nightLength = nightSeconds * VillageService.NightMultiplier * Mathf.Lerp(1f, 0.8f, Mathf.Clamp01(args.LevelIndex / 30f));
            speedBonus = VillageService.SpeedBonus;
            SetAboard(0);

            startX = x = 0f;
            waveTime = 0f;
            ampScale = 1f;
            y = Height(x);
            speed = startSpeed + speedBonus;
            vx = speed;
            vy = 0f;
            airborne = false;
            airTime = 0f;
            perfectStreak = 0;
            fever = 0f;
            moonlight = 1f;
            aboard = rescued = lanternsCaught = lighthousesLit = 0;
            runTime = 0f;
            progressCheckAt = 2f;
            progressCheckX = x;
            nextCastawayAt = 30f;
            nextLanternAt = 45f;
            nextIslandAt = 115f;
            nextRockAt = 70f;
            score.SetFullMoon(false);

            foreach (GameObject go in hideWhileSailing)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }

            running = true;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (!running)
            {
                return;
            }

            runTime += deltaTime;
            waveTime += deltaTime;
            holding = ForcedHold ?? ReadHold();

            // Swells grow the further out you sail: more air, more speed, more to master.
            ampScale = Mathf.Lerp(1f, 1.4f, Mathf.Clamp01((x - startX) / 1400f));

            if (airborne)
            {
                Fly(deltaTime);
            }
            else
            {
                Ride(deltaTime);
            }

            if (fever > 0f)
            {
                fever -= deltaTime;
                if (fever <= 0f)
                {
                    score.SetFullMoon(false);
                    GameEvents.RaiseFullMoonEnded();
                }
            }
            else
            {
                // The night gets a little shorter each level, but lighthouses always buy it back.
                moonlight -= deltaTime / Mathf.Max(5f, nightLength);
            }

            Unstick();
            SpawnAhead();
            Interact();
            DespawnBehind();

            // Lighthouses refill the moonlight, but no night lasts forever: dawn always comes.
            if (moonlight <= 0f || runTime > Mathf.Max(90f, nightLength * 4f))
            {
                EndNight();
            }
        }

        /// <summary>Ends the night now (pause menu): everyone aboard gets home and the run is scored.</summary>
        public void EndNightNow()
        {
            if (running)
            {
                EndNight();
            }
        }

        /// <summary>
        /// Safety net: if the boat has barely moved for a while (wedged against a steep face, or a bad float), lift it
        /// onto the surface and give it a push so a run can never get stuck.
        /// </summary>
        private void Unstick()
        {
            bool broken = float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(speed) || float.IsInfinity(vy);
            if (!broken && y > Height(x) - 1.5f && runTime < progressCheckAt)
            {
                return;
            }

            if (broken || x - progressCheckX < 4f || y < Height(x) - 1.5f)
            {
                if (broken)
                {
                    x = progressCheckX + 1f;
                }

                airborne = false;
                y = Height(x);
                speed = Mathf.Max(startSpeed, speed) + 4f;
                vx = speed;
                vy = 0f;
                Splash(20, new Color(0.9f, 0.96f, 1f, 0.85f));
            }

            progressCheckAt = runTime + 2f;
            progressCheckX = x;
        }

        private bool ReadHold()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return false;
                }

                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }

            return Input.GetMouseButton(0) && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
        }

        // ------------------------------------------------------------------ surfing physics

        private void Ride(float dt)
        {
            float s = Slope(x);
            float inv = 1f / Mathf.Sqrt(1f + s * s);
            float sin = s * inv;
            float g = gravity * (holding ? diveMultiplier : 1f);

            // Along the surface: gravity pulls down the slope; holding makes the boat heavy on the way down,
            // but going uphill a heavy boat only loses speed, so the skill is releasing at the bottom.
            float accel = -g * sin;
            if (holding && sin > 0f)
            {
                accel = -gravity * 1.25f * sin;
            }

            speed += accel * dt;
            if (speed < minSpeed)
            {
                speed = Mathf.MoveTowards(speed, minSpeed, 6f * dt); // the wind never lets the boat stall
            }

            // Holding is a dive, not an accelerator: above cruising speed the sea drags the boat back down, so
            // speed comes from well-timed releases and perfect landings, not from keeping a finger on the screen.
            float cruise = 11f + speedBonus + BoatUpgrades.CruiseBonus;
            if (speed > cruise && fever <= 0f)
            {
                speed -= (speed - cruise) * (holding ? 0.9f : 0.35f) * dt;
            }

            float cap = maxSpeed + speedBonus;
            speed = Mathf.Min(speed, fever > 0f ? cap * 1.2f : cap);

            float nx = x + speed * inv * dt;
            float ny = Height(nx);
            float vyAlong = speed * sin;

            // Leave the water when the ballistic path clears the surface ahead (a crest), unless diving.
            float ballistic = y + vyAlong * dt;
            if (!holding && vyAlong > 2f && ballistic > ny + 0.015f)
            {
                airborne = true;
                airTime = 0f;
                GameEvents.RaiseWaveLaunched(Mathf.Clamp01(vyAlong / 10f));
                vx = speed * inv;
                vy = vyAlong;
                x = nx;
                y = ballistic;
                return;
            }

            vx = speed * inv;
            vy = (ny - y) / Mathf.Max(dt, 0.0001f);
            x = nx;
            y = ny;
        }

        private void Fly(float dt)
        {
            airTime += dt;
            vy -= gravity * (holding ? 2.4f : 1f) * dt;
            x += vx * dt;
            y += vy * dt;

            float surface = Height(x);
            if (y > surface)
            {
                return;
            }

            // Landing: compare the fall angle with the slope of the wave under the boat.
            float slopeAngle = Mathf.Atan(Slope(x));
            float fallAngle = Mathf.Atan2(vy, vx);
            float diff = Mathf.Abs(fallAngle - slopeAngle);
            float along = Mathf.Sqrt(vx * vx + vy * vy) * Mathf.Cos(diff);
            airborne = false;
            y = surface;

            if (diff < 0.32f && airTime > 0.35f && slopeAngle < 0.05f)
            {
                perfectStreak++;
                speed = Mathf.Max(along, speed) * 1.1f;
                Splash(28, new Color(1f, 0.9f, 0.55f, 0.95f));
                score.AddBonus(25 * Mathf.Min(perfectStreak, 8));
                GameEvents.RaisePerfectCrest(perfectStreak);
                if (perfectStreak % 3 == 0)
                {
                    StartFever();
                }
            }
            else if (diff < 0.75f)
            {
                speed = Mathf.Max(minSpeed, along * 0.92f);
                perfectStreak = 0;
                Splash(16, new Color(0.9f, 0.96f, 1f, 0.85f));
            }
            else
            {
                // Belly flop: a big splash, most of the speed gone and someone falls overboard. Never a game over.
                HitHazard();
                speed = Mathf.Max(minSpeed, along * 0.5f);
                GameEvents.RaiseNearMissChainBroken();
            }
        }

        /// <summary>A rock or a belly flop: most of the speed gone and one passenger falls overboard.</summary>
        private void HitHazard()
        {
            speed = Mathf.Max(minSpeed, speed * BoatUpgrades.HitSpeedKept);
            perfectStreak = 0;
            shake = 0.45f;
            Splash(45, new Color(0.9f, 0.96f, 1f, 0.9f));
            if (aboard > 0 && random.NextDouble() >= BoatUpgrades.HoldOnChance)
            {
                SetAboard(aboard - 1);
            }

            GameEvents.RaiseBoatBumped(aboard);
        }

        private void StartFever()
        {
            fever = feverSeconds;
            moonlight = Mathf.Min(1f, moonlight + 0.08f);
            score.SetFullMoon(true);
            GameEvents.RaiseFullMoonStarted(feverSeconds);
        }

        // ------------------------------------------------------------------ castaways, lanterns, islands

        private void SpawnAhead()
        {
            float horizon = x + 55f;
            while (nextCastawayAt < horizon)
            {
                Add(Kind.Castaway, castawayPrefab, nextCastawayAt, 0f);
                nextCastawayAt += 22f + (float)random.NextDouble() * 26f;
            }

            while (nextLanternAt < horizon)
            {
                // Sky lanterns float high: only a real jump reaches them. They feed the moonlight.
                float h = 4f + (float)random.NextDouble() * 4.5f * ampScale;
                for (int i = 0; i < 3; i++)
                {
                    Add(Kind.Lantern, lanternPrefab, nextLanternAt + i * 1.8f, h + i * 0.5f);
                }

                nextLanternAt += 40f + (float)random.NextDouble() * 35f;
            }

            while (nextRockAt < horizon)
            {
                // Rocks: never right on top of a castaway, closer together on later nights.
                if (Mathf.Abs(nextRockAt - nextCastawayAt) > 5f)
                {
                    Add(Kind.Rock, rockPrefab, nextRockAt, 0f);
                }

                nextRockAt += Mathf.Max(22f, 70f - levelIndex * 2f) + (float)random.NextDouble() * 30f;
            }

            while (nextIslandAt < horizon)
            {
                Thing island = Add(Kind.Island, islandPrefab, nextIslandAt, 0f);
                if (island != null && island.Transform != null)
                {
                    Transform lit = island.Transform.Find("Lit");
                    island.Lit = lit != null ? lit.gameObject : null;
                    if (island.Lit != null)
                    {
                        island.Lit.SetActive(false);
                    }
                }

                nextIslandAt += 120f + (float)random.NextDouble() * 60f + levelIndex * 1.5f;
            }
        }

        private Thing Add(Kind kind, GameObject prefab, float px, float height)
        {
            if (prefab == null)
            {
                return null;
            }

            var thing = new Thing { Kind = kind, Transform = Instantiate(prefab, transform).transform, X = px, Height = height };
            things.Add(thing);
            if (kind == Kind.Castaway)
            {
                VillageDirector.Dress(thing.Transform, random);
            }

            Place(thing);
            return thing;
        }

        private void Interact()
        {
            foreach (Thing t in things)
            {
                if (t.Done)
                {
                    continue;
                }

                float dx = Mathf.Abs(t.X - x);
                switch (t.Kind)
                {
                    case Kind.Castaway:
                        if (dx < 1.1f + BoatUpgrades.ReachBonus && y - Height(t.X) < 1.4f + BoatUpgrades.ReachBonus && aboard < seats)
                        {
                            t.Done = true;
                            t.Transform.gameObject.SetActive(false);
                            SetAboard(aboard + 1);
                            score.AddBonus(50);
                            GameEvents.RaisePassengerBoarded(aboard);
                        }

                        break;
                    case Kind.Lantern:
                        if (dx < 1f + BoatUpgrades.ReachBonus && Mathf.Abs(y - (Height(t.X) + t.Height)) < 1.2f + BoatUpgrades.ReachBonus)
                        {
                            t.Done = true;
                            t.Transform.gameObject.SetActive(false);
                            lanternsCaught++;
                            moonlight = Mathf.Min(1f, moonlight + 0.035f);
                            score.AddBonus(20);
                            GameEvents.RaiseStarCollected(lanternsCaught);
                        }

                        break;
                    case Kind.Rock:
                        if (dx < 0.9f && y - Height(t.X) < 1.0f)
                        {
                            t.Done = true;
                            HitHazard();
                        }

                        break;
                    case Kind.Island:
                        if (dx < 3f)
                        {
                            if (aboard > 0)
                            {
                                Deliver(t);
                            }
                        }

                        break;
                }
            }
        }

        private void Deliver(Thing island)
        {
            int count = aboard;
            island.Done = true;
            if (island.Lit != null)
            {
                island.Lit.SetActive(true);
            }

            rescued += count;
            lighthousesLit++;
            moonlight = Mathf.Min(1f, moonlight + 0.22f + 0.06f * count);
            score.AddBonus(150 * count);
            SetAboard(0);
            GameEvents.RaisePassengersDelivered(count);
            GameEvents.RaiseLighthouseLit(count);
        }

        private void DespawnBehind()
        {
            for (int i = things.Count - 1; i >= 0; i--)
            {
                if (things[i].X < x - 30f)
                {
                    Destroy(things[i].Transform.gameObject);
                    things.RemoveAt(i);
                }
            }
        }

        private void ClearThings()
        {
            foreach (Thing t in things)
            {
                if (t.Transform != null)
                {
                    Destroy(t.Transform.gameObject);
                }
            }

            things.Clear();
        }

        private void EndNight()
        {
            running = false;
            // Everyone still aboard makes it home at dawn too.
            rescued += aboard;
            SetAboard(0);

            VillageState.Simulate();
            int levelBefore = VillageState.Level;
            VillageState.AddPeople(rescued);
            LastSupplies = TycoonState.SuppliesFromNight(rescued, lanternsCaught);
            TycoonState.AddSupplies(LastSupplies);
            int levelReward = 0;
            for (int l = levelBefore + 1; l <= VillageState.Level; l++)
            {
                levelReward += VillageState.LevelReward(l);
            }
            int total = VillageState.Population;

            int target = 3 + Mathf.Min(levelIndex, 30) / 2;
            int stars = rescued >= target * 2 ? 3 : rescued >= target ? 2 : 1;
            int coins = Mathf.RoundToInt((rescued * 10 + lanternsCaught * 2) * VillageService.CoinMultiplier) + VillageService.DawnCoins(total) + levelReward;
            var result = new LevelResult(levelIndex, stars, score.Score, runTime, lanternsCaught, coins,
                0, rescued, lighthousesLit, false, false);
            GameEvents.RaiseLevelCompleted(result);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu || to == GameState.Shop)
            {
                running = false;
                ClearThings();
                SetAboard(0);
                foreach (GameObject go in hideWhileSailing)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ visuals

        private void SetAboard(int count)
        {
            aboard = count;
            while (aboardFigures.Count < Mathf.Max(seats, capacity) && passengerPrefab != null && boatRoot != null)
            {
                GameObject figure = Instantiate(passengerPrefab, boatRoot);
                figure.transform.localPosition = new Vector3(-0.7f + aboardFigures.Count * 0.34f, 0.28f, 0f);
                VillageDirector.Dress(figure.transform, new System.Random(31 + aboardFigures.Count * 7));
                aboardFigures.Add(figure);
            }

            for (int i = 0; i < aboardFigures.Count; i++)
            {
                aboardFigures[i].SetActive(i < count);
            }
        }

        private void Place(Thing t)
        {
            float surface = Height(t.X);
            switch (t.Kind)
            {
                case Kind.Castaway:
                    t.Transform.position = new Vector3(t.X, surface, 0.4f);
                    t.Transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan(Slope(t.X)) * Mathf.Rad2Deg);
                    break;
                case Kind.Rock:
                    t.Transform.position = new Vector3(t.X, surface - 0.15f, 0f);
                    t.Transform.localScale = Vector3.one * 1.25f;
                    break;
                case Kind.Lantern:
                    t.Transform.position = new Vector3(t.X, surface + t.Height + 0.2f * Mathf.Sin(Time.time * 2f + t.X), 0f);
                    break;
                default:
                    t.Transform.position = new Vector3(t.X, -0.4f, 7f);
                    break;
            }
        }

        private void SetBoat(BoatDefinition boat)
        {
            if (boat == null || boat.ModelPrefab == null || boatRoot == null || (boatModel != null && boatModelId == boat.Id))
            {
                return;
            }

            if (boatModel != null)
            {
                Destroy(boatModel.gameObject);
            }

            boatModel = Instantiate(boat.ModelPrefab, boatRoot).transform;
            boatModel.localPosition = Vector3.zero;
            boatModel.localRotation = Quaternion.identity;
            boatModelId = boat.Id;
        }

        private void LateUpdate()
        {
            if (!running)
            {
                // Menu backdrop: a calm sea so the village island stays dry; the boat rocks gently.
                waveTime += Time.deltaTime;
                ampScale = Mathf.MoveTowards(ampScale, 0.3f, Time.deltaTime * 0.5f);
                y = Height(x);
                vx = 3f;
                vy = Slope(x) * 3f;
            }

            PushWaves();

            if (boatModel == null)
            {
                if (session != null && session.Boat != null)
                {
                    SetBoat(session.Boat);
                }
                else if (defaultBoatModel != null && boatRoot != null)
                {
                    boatModel = Instantiate(defaultBoatModel, boatRoot).transform;
                    boatModelId = null;
                }
            }

            if (running)
            {
                foreach (Thing t in things)
                {
                    if (!t.Done && t.Kind != Kind.Island)
                    {
                        Place(t);
                    }
                }
            }

            if (boatRoot != null)
            {
                float pitch = Mathf.Atan2(vy, Mathf.Max(vx, 0.1f)) * Mathf.Rad2Deg;
                boatRoot.position = new Vector3(x, y + 0.05f, 0f);
                boatRoot.rotation = Quaternion.Slerp(boatRoot.rotation, Quaternion.Euler(0f, 0f, Mathf.Clamp(pitch * 0.7f, -30f, 30f)),
                    1f - Mathf.Exp(-12f * Time.deltaTime));
            }

            if (cameraTransform != null && !VillageDirector.Active)
            {
                // Side view that pulls back as the boat climbs, keeping the wave ahead in frame.
                float altitude = Mathf.Max(0f, y - Height(x));
                float back = 15f + altitude * 0.8f + (fever > 0f ? 1.5f : 0f);
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
                Vector3 jitter = shake > 0f ? Random.insideUnitSphere * shake * 0.35f : Vector3.zero;
                // High enough that the nearest swells never rise above the lens, tilted down onto the sea.
                Vector3 target = new Vector3(x + 1.8f, 7f + Mathf.Max(0f, y) * 0.6f, -back) + jitter;
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
                cameraTransform.rotation = Quaternion.Euler(17f, 0f, 0f);
            }

            if (moonAnchor != null)
            {
                // The moon sinks towards the horizon as the night runs out.
                float night = running ? moonlight : 1f;
                moonAnchor.localPosition = new Vector3(4f, Mathf.Lerp(4f, 13f, night), 30f);
            }
        }
    }
}
