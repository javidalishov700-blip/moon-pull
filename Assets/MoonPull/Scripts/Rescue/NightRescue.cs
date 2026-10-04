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
            public bool Judged;
            public GameObject Lit;
        }

        /// <summary>What happened during one night: read by the playtest bot and useful for balancing.</summary>
        public sealed class NightStats
        {
            public int Level, Rescued, Lanterns, Lighthouses, RocksHit, RocksDodged, BellyFlops, Perfects, Hops, Unsticks, PassengersLost;
            public int Coins, Supplies;
            public float Duration, Distance, MaxSpeed;
        }

        /// <summary>Stats of the night in progress (or the last one once it has ended).</summary>
        public NightStats Stats { get; private set; } = new NightStats();

        /// <summary>
        /// Context hint for the first nights (a localization key, or null): hold on the way down, let go to fly,
        /// tap before a rock, sail to the lighthouse when the boat is full.
        /// </summary>
        public string CoachKey { get; private set; }

        /// <summary>Playtest autopilot: negative = off, 0..1 = how reliably it reacts to rocks.</summary>
        public float AutoPilotSkill { get; set; } = -1f;

        private System.Random botRandom = new System.Random(99);
        private bool hopFlight;

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
        private float pressStartedAt, hopCooldown;
        private float hopQueued;

        /// <summary>JUMP button. If pressed a moment early (mid-air), the hop fires on touchdown.</summary>
        public void RequestHop()
        {
            if (running)
            {
                hopQueued = 0.35f;
            }
        }
        private float speedBonus;

        private ParticleSystem splash;
        private TrailRenderer wake;
        private Camera sceneCamera;
        private Vector3 cameraBase;
        private bool cameraBaseValid;

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

        /// <summary>A burst of spray or sparks at a world position (pickups, deliveries).</summary>
        private void SplashAt(Vector3 position, int count, Color color)
        {
            Splash(0, color); // makes sure the particle system exists
            splash.transform.position = position;
            var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = true };
            splash.Emit(emit, count);
        }

        /// <summary>Capture player / automation: overrides the touch input while set.</summary>
        public bool? ForcedHold { get; set; }

        /// <summary>DIVE button held (same as a finger on the sea).</summary>
        public bool ButtonHold { get; set; }

        /// <summary>True after a night that beat the personal best.</summary>
        public bool NewRecord { get; private set; }

        private bool recordAnnounced;
        private float recordFlash;

        /// <summary>0..1, filled by perfect landings and lanterns; BOOST spends it.</summary>
        public float BoostCharge { get; private set; }

        /// <summary>BOOST button: a burst of speed under a full moon. Needs a full charge.</summary>
        public void UseBoost()
        {
            if (!running || BoostCharge < 1f)
            {
                return;
            }

            BoostCharge = 0f;
            speed += 7f;
            shake = Mathf.Max(shake, 0.2f);
            Splash(30, new Color(1f, 0.9f, 0.55f, 0.95f));
            StartFever();
        }

        /// <summary>The longest this night can last (dawn always comes).</summary>
        private float NightCap => Mathf.Lerp(60f, 105f, Mathf.Clamp01(levelIndex / 12f)) * Mathf.Min(1.3f, VillageService.NightMultiplier);

        /// <summary>Night bar: whichever runs out first, the moonlight or the time until dawn, so the bar never lies.</summary>
        public float Moonlight => Mathf.Min(moonlight, running ? Mathf.Clamp01(1f - runTime / NightCap) : moonlight);

        /// <summary>Supplies the last night brought home (shown on the win screen).</summary>
        public static int LastSupplies { get; private set; }
        public int Aboard => aboard;

        public int Seats => seats;

        public bool Running => running;

        /// <summary>A night is being sailed right now (map visuals stay off).</summary>
        public static bool NightActive { get; private set; }

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
            if (cameraTransform != null)
            {
                sceneCamera = cameraTransform.GetComponent<Camera>();
            }

            if (boatRoot != null)
            {
                // Foam wake behind the stern; only drawn while the hull is in the water.
                var wakeGo = new GameObject("Wake");
                wakeGo.transform.SetParent(boatRoot, false);
                wakeGo.transform.localPosition = new Vector3(-0.9f, 0.02f, 0f);
                wake = wakeGo.AddComponent<TrailRenderer>();
                wake.time = 0.5f;
                wake.minVertexDistance = 0.25f;
                wake.widthMultiplier = 0.3f;
                wake.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(0.9f, 0.96f, 1f), 0f), new GradientColorKey(new Color(0.7f, 0.85f, 1f), 1f) },
                    new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0f, 1f) });
                wake.colorGradient = gradient;
                wake.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                wake.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                wake.emitting = false;
            }
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
            BoostCharge = 0f;
            ButtonHold = false;
            recordAnnounced = false;
            recordFlash = 0f;
            NewRecord = false;
            hopFlight = false;
            Stats = new NightStats { Level = levelIndex };
            if (wake != null)
            {
                wake.Clear();
            }

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
            NightActive = true;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (!running)
            {
                return;
            }

            runTime += deltaTime;
            waveTime += deltaTime;
            bool wasHolding = holding;
            hopCooldown -= deltaTime;
            if (AutoPilotSkill >= 0f)
            {
                holding = AutoHold();
                if (BoostCharge >= 1f)
                {
                    UseBoost();
                }
                if (AutoHop())
                {
                    Hop();
                }
            }
            else
            {
                // Screen = dive into the waves; the JUMP button hops (see RequestHop).
                holding = ForcedHold ?? (ReadHold() || ButtonHold);
                if (hopQueued > 0f)
                {
                    hopQueued -= deltaTime;
                    if (!airborne && hopCooldown <= 0f)
                    {
                        hopQueued = 0f;
                        Hop();
                    }
                }
            }

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

            Stats.MaxSpeed = Mathf.Max(Stats.MaxSpeed, speed);
            CoachKey = levelIndex < 3 || BoostCharge >= 1f ? Coach() : null;
            // Passing your best night mid-run gets its own moment.
            int best = MoonPull.Online.Leaderboards.PersonalBest;
            if (!recordAnnounced && best > 0 && rescued + aboard > best)
            {
                recordAnnounced = true;
                recordFlash = 2.5f;
                GameEvents.RaiseRescueLanded(1, 1f);
            }

            if (recordFlash > 0f)
            {
                recordFlash -= deltaTime;
                CoachKey = "hud.new_record";
            }
            Unstick();
            SpawnAhead();
            Interact();
            DespawnBehind();

            // Lighthouses refill the moonlight, but no night lasts forever: dawn always comes.
            // First nights stay short and snappy (about a minute); later nights may run up to about two minutes.
            if (moonlight <= 0f || runTime > NightCap)
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

            if (broken || x - progressCheckX < 2.5f || y < Height(x) - 1.5f)
            {
                if (broken)
                {
                    x = progressCheckX + 1f;
                }

                Stats.Unsticks++;
                Debug.LogWarning($"[MoonPull] Unstick at x={x:0.0} y={y:0.00} speed={speed:0.0} airborne={airborne}");
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
                // Any finger on the sea (not on a button) dives, so the thumb on JUMP doesn't block the other hand.
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        continue;
                    }

                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        return true;
                    }
                }

                return false;
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
                speed = Mathf.MoveTowards(speed, minSpeed, 8f * dt); // the wind never lets the boat stall
            }

            // A steep face slows the boat but never rolls it backwards into the trough (that used to trap it
            // there until the unstick safety net fired, which players saw as "stuck on a wave").
            speed = Mathf.Max(speed, minSpeed * 0.6f);

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
            vy -= gravity * (holding && !hopFlight ? 2.4f : 1f) * dt; // hops keep a steady, readable arc
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

            if (hopFlight)
            {
                // A rock hop is a small, controlled bounce: it always lands cleanly.
                hopFlight = false;
                speed = Mathf.Max(minSpeed, Mathf.Max(along, speed * 0.95f));
                Splash(12, new Color(0.9f, 0.96f, 1f, 0.8f));
                GameEvents.RaiseRescueLanded(3, 0.3f);
            }
            else if (diff < 0.32f && airTime > 0.35f && slopeAngle < 0.05f)
            {
                perfectStreak++;
                BoostCharge = Mathf.Min(1f, BoostCharge + 0.34f);
                Stats.Perfects++;
                speed = Mathf.Max(along, speed) * 1.1f;
                shake = Mathf.Max(shake, 0.12f);
                GameEvents.RaiseRescueLanded(1, Mathf.Clamp01(airTime / 1.5f));
                Splash(28, new Color(1f, 0.9f, 0.55f, 0.95f));
                score.AddBonus(25 * Mathf.Min(perfectStreak, 8));
                GameEvents.RaisePerfectCrest(perfectStreak);
                if (perfectStreak % 3 == 0)
                {
                    StartFever();
                }
            }
            else if (diff < 0.9f)
            {
                speed = Mathf.Max(minSpeed, along * 0.92f);
                perfectStreak = 0;
                Splash(16, new Color(0.9f, 0.96f, 1f, 0.85f));
                GameEvents.RaiseRescueLanded(0, Mathf.Clamp01(airTime / 1.5f));
            }
            else
            {
                // Belly flop: a big splash, most of the speed gone and someone falls overboard. Never a game over.
                Stats.BellyFlops++;
                GameEvents.RaiseRescueLanded(2, 1f);
                if (diff > 1.15f)
                {
                    HitHazard(); // a really hard slap: someone goes overboard
                }
                else
                {
                    perfectStreak = 0;
                    shake = 0.25f;
                    Splash(40, new Color(0.9f, 0.96f, 1f, 0.9f));
                }

                speed = Mathf.Max(minSpeed, along * 0.6f);
                GameEvents.RaiseNearMissChainBroken();
            }
        }

        private string Coach()
        {
            float reach = Mathf.Max(speed, minSpeed) * 0.9f + 2.5f; // same window as the hop
            foreach (Thing t in things)
            {
                if (t.Kind == Kind.Rock && !t.Done && t.X > x)
                {
                    float dx = t.X - x;
                    if (dx < reach)
                    {
                        return "hud.coach_hop";
                    }

                    if (dx < 16f)
                    {
                        return "hud.coach_rock";
                    }
                }
            }

            if (BoostCharge >= 1f)
            {
                return "hud.coach_boost";
            }

            if (aboard >= seats)
            {
                return "hud.coach_full";
            }

            if (runTime < 10f && !airborne)
            {
                return Slope(x) < 0f ? "hud.coach_hold" : "hud.coach_release";
            }

            return null;
        }

        // ------------------------------------------------------------------ playtest autopilot

        /// <summary>Dive on the way down, let go on the way up so the boat flies off crests.</summary>
        private bool AutoHold() => !airborne && Slope(x) < -0.08f;

        /// <summary>Hop a rock that is about to be reached; misses some, like a real player.</summary>
        private bool AutoHop()
        {
            if (airborne || hopCooldown > 0f)
            {
                return false;
            }

            float reach = Mathf.Max(speed, minSpeed) * 0.35f + 0.6f;
            foreach (Thing t in things)
            {
                if (t.Kind != Kind.Rock || t.Done || t.Judged)
                {
                    continue;
                }

                float dx = t.X - x;
                if (dx > 0f && dx < reach)
                {
                    t.Judged = true;
                    return botRandom.NextDouble() < AutoPilotSkill;
                }
            }

            return false;
        }

        private bool RockAhead(float reach)
        {
            foreach (Thing t in things)
            {
                if (t.Kind == Kind.Rock && !t.Done && t.X > x && t.X - x < reach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Quick tap: a short hop off the water, enough to clear a rock if timed right.</summary>
        private void Hop()
        {
            airborne = true;
            airTime = 0f;
            vx = Mathf.Max(speed, minSpeed);
            vy = 7.4f;
            y += 0.05f;
            hopCooldown = 0.8f;
            hopFlight = true;
            Stats.Hops++;
            Splash(14, new Color(0.9f, 0.96f, 1f, 0.8f));
            GameEvents.RaiseWaveLaunched(0.5f);
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
                Stats.PassengersLost++;
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
                nextCastawayAt += 30f + (float)random.NextDouble() * 30f;
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

                nextRockAt += Mathf.Max(32f, 70f - levelIndex * 2f) + (float)random.NextDouble() * 30f;
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
                            SplashAt(t.Transform.position + Vector3.up * 0.6f, 18, new Color(1f, 0.86f, 0.5f, 0.95f));
                            GameEvents.RaisePassengerBoarded(aboard);
                        }

                        break;
                    case Kind.Lantern:
                        if (dx < 1f + BoatUpgrades.ReachBonus && Mathf.Abs(y - (Height(t.X) + t.Height)) < 1.2f + BoatUpgrades.ReachBonus)
                        {
                            t.Done = true;
                            t.Transform.gameObject.SetActive(false);
                            lanternsCaught++;
                            BoostCharge = Mathf.Min(1f, BoostCharge + 0.06f);
                            moonlight = Mathf.Min(1f, moonlight + 0.035f);
                            score.AddBonus(20);
                            SplashAt(t.Transform.position, 20, new Color(1f, 0.72f, 0.32f, 0.95f));
                            GameEvents.RaiseStarCollected(lanternsCaught);
                        }

                        break;
                    case Kind.Rock:
                        if (dx < 0.6f && y - Height(t.X) < 0.6f) // forgiving: only a clear hit counts
                        {
                            t.Done = true;
                            Stats.RocksHit++;
                            HitHazard();
                        }
                        else if (t.X < x - 1f)
                        {
                            t.Done = true;
                            Stats.RocksDodged++;
                            GameEvents.RaiseRockDodged();
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
            SplashAt(island.Transform.position + new Vector3(-0.8f, 6.1f, 0.4f), 45, new Color(1f, 0.93f, 0.62f, 1f));
            shake = Mathf.Max(shake, 0.15f);
            moonlight = Mathf.Min(1f, moonlight + 0.14f + 0.04f * count);
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
            NightActive = false;
            CoachKey = null;
            // Everyone still aboard makes it home at dawn too.
            rescued += aboard;
            SetAboard(0);

            NewRecord = MoonPull.Online.Leaderboards.ReportNight(rescued);
            VillageState.Simulate();
            int levelBefore = VillageState.Level;
            int settled = VillageState.AddPeople(rescued);
            // A full village cannot take everyone in: those who sail on to other harbors leave a thank-you fee.
            int sailedOn = rescued - settled;
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
            int coins = Mathf.RoundToInt((rescued * 7 + lanternsCaught * 2) * VillageService.CoinMultiplier) + VillageService.DawnCoins(total) + levelReward
                        + sailedOn * 6;
            Stats.Rescued = rescued;
            Stats.Lanterns = lanternsCaught;
            Stats.Lighthouses = lighthousesLit;
            Stats.Coins = coins;
            Stats.Supplies = LastSupplies;
            Stats.Duration = runTime;
            Stats.Distance = x - startX;
            var result = new LevelResult(levelIndex, stars, score.Score, runTime, lanternsCaught, coins,
                0, rescued, lighthousesLit, false, false);
            GameEvents.RaiseLevelCompleted(result);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu || to == GameState.Shop)
            {
                running = false;
                NightActive = false;
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

            if (wake != null)
            {
                wake.emitting = running && !airborne;
            }

            if (cameraTransform != null && !VillageDirector.Active && !VillageDirector.MenuView)
            {
                if (!cameraBaseValid)
                {
                    cameraBase = cameraTransform.position;
                    cameraBaseValid = true;
                }

                // Side view that pulls back as the boat climbs and leads a little with speed, keeping the wave
                // ahead in frame. High enough that the nearest swells never rise above the lens.
                float altitude = Mathf.Max(0f, y - Height(x));
                // Taller-than-16:9 phones see less sea sideways: pull back so the view ahead stays the same.
                float aspectFit = sceneCamera != null ? Mathf.Pow(Mathf.Max(1f, 0.5625f / sceneCamera.aspect), 0.85f) : 1f;
                float back = (15f + altitude * 0.8f + (fever > 0f ? 1.5f : 0f)) * aspectFit;
                float lead = running ? Mathf.Clamp(speed - minSpeed, 0f, 12f) * 0.12f : 0f;
                Vector3 target = new Vector3(x + 1.8f + lead, 7f + Mathf.Max(0f, y) * 0.6f, -back);
                cameraBase = Vector3.Lerp(cameraBase, target, 1f - Mathf.Exp(-7f * Time.deltaTime));

                // Shake is applied on top of the smoothed position so impacts stay crisp.
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
                Vector3 jitter = shake > 0f ? Random.insideUnitSphere * shake * 0.35f : Vector3.zero;
                cameraTransform.position = cameraBase + jitter;
                cameraTransform.rotation = Quaternion.Euler(17f, 0f, 0f);

                if (sceneCamera != null)
                {
                    // Speed reads as a wider lens: up to +9 degrees at full tilt, +3 more during Full Moon.
                    float fov = 60f + (running ? Mathf.Clamp((speed - 11f) * 0.7f, 0f, 9f) + (fever > 0f ? 3f : 0f) : 0f);
                    sceneCamera.fieldOfView = Mathf.Lerp(sceneCamera.fieldOfView, fov, 1f - Mathf.Exp(-3f * Time.deltaTime));
                }
            }
            else
            {
                cameraBaseValid = false;
                if (sceneCamera != null)
                {
                    sceneCamera.fieldOfView = 60f;
                }
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
