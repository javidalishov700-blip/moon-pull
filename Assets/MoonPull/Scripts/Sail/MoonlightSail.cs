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

namespace MoonPull.Sail
{
    /// <summary>
    /// "Sail by moonlight": the core loop. The boat always heads out to sea; the player slides a finger left or right
    /// to move the moon across the sky, and the boat steers into the moonlight path it casts on the water. Rocks drift
    /// towards you, stars and coins glitter in the light, five moonstones call a Full Moon (magnet + double score),
    /// and a lighthouse marks the end of the voyage. Gentle by design: three hearts, generous gaps, no instant loss.
    ///
    /// The world is a treadmill: the boat stays near z = 0 and everything else moves towards the camera, while the
    /// water shader scrolls its waves by the distance travelled.
    /// </summary>
    public sealed class MoonlightSail : MonoBehaviour, ISimulationTickable
    {
        private enum Kind { Rock, Star, Coin, Moonstone, Lighthouse }

        private struct Prop
        {
            public Kind Kind;
            public Transform Transform;
            public float X;
            public float Z;
            public float Radius;
            public bool Alive;
        }

        [Header("World")]
        [SerializeField] private LevelSession session;
        [SerializeField] private ScoreSystem score;
        [SerializeField] private FullMoonMode fullMoon;
        [SerializeField] private WaterSurface water;
        [SerializeField] private TideModel tide;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform moonAnchor;
        [SerializeField] private GameObject legacyBoat;
        [SerializeField] private Transform boatRoot;
        [SerializeField] private GameObject defaultBoatModel;

        [Header("Props")]
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private GameObject starPrefab;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject moonstonePrefab;
        [SerializeField] private GameObject lighthousePrefab;

        [Header("Feel")]
        [SerializeField] private float laneHalfWidth = 3.6f;
        [SerializeField] private float steerSensitivity = 11f;
        [SerializeField] private float steerSharpness = 7f;
        [SerializeField] private float spawnAhead = 70f;
        [SerializeField] private float despawnBehind = -9f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.4f, -8.2f);
        [SerializeField] private float cameraPitch = 12f;
        [SerializeField] private int maxHearts = 3;
        [SerializeField] private float invulnerableSeconds = 1.6f;
        [SerializeField] private float fullMoonSeconds = 6f;
        [SerializeField] private float magnetRadius = 4f;

        private static readonly int ScrollId = Shader.PropertyToID("_MP_ScrollZ");

        private readonly List<Prop> props = new List<Prop>();
        private readonly Dictionary<Kind, Stack<Transform>> pool = new Dictionary<Kind, Stack<Transform>>();
        private System.Random random = new System.Random(1);
        private Transform boatModel;
        private string boatModelId;

        private bool running;
        private float distance;
        private float goal;
        private float speed;
        private float nextRowAt;
        private bool lighthouseSpawned;
        private int hearts;
        private float invulnerable;
        private float boatX;
        private float targetX;
        private float steerVelocity;
        private bool dragging;
        private float lastPointerX;
        private int moonstones;
        private float fullMoonLeft;
        private int starStreak;
        private int starsCollected;
        private int coinsCollected;
        private int starsSpawned;
        private float levelTime;
        private float shake;
        private int levelIndex;

        public bool IsRunning => running;
        public float Progress => goal > 0f ? Mathf.Clamp01(distance / goal) : 0f;
        public int Hearts => hearts;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

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
        }

        /// <summary>Steers as if the player dragged (capture player / automation).</summary>
        public void SetSteerTarget(float x) => targetX = Mathf.Clamp(x, -laneHalfWidth, laneHalfWidth);

        /// <summary>Called by LevelSession when a level starts.</summary>
        public void Begin(LevelStartArgs args, BoatDefinition boat)
        {
            levelIndex = args.LevelIndex;
            random = new System.Random(9173 + levelIndex * 131);
            ClearProps();
            SetBoat(boat);

            // Gentle ramp: longer and a little faster every level, capped so it never becomes a reflex test.
            goal = 220f + Mathf.Min(levelIndex, 40) * 9f;
            speed = 8.5f + Mathf.Min(levelIndex, 40) * 0.09f;
            distance = 0f;
            nextRowAt = 18f;
            lighthouseSpawned = false;
            hearts = maxHearts;
            invulnerable = 0f;
            boatX = targetX = steerVelocity = 0f;
            dragging = false;
            moonstones = 0;
            fullMoonLeft = 0f;
            starStreak = starsCollected = coinsCollected = starsSpawned = 0;
            levelTime = 0f;
            score.SetFullMoon(false);
            water.SetFullMoonGlow(0f);
            running = true;
        }

        public void SimulationTick(float deltaTime, float time)
        {
            if (!running)
            {
                return;
            }

            levelTime += deltaTime;
            ReadInput();

            float step = speed * deltaTime;
            distance += step;
            Shader.SetGlobalFloat(ScrollId, distance);

            boatX = Mathf.SmoothDamp(boatX, targetX, ref steerVelocity, 1f / steerSharpness, Mathf.Infinity, deltaTime);
            invulnerable = Mathf.Max(0f, invulnerable - deltaTime);

            if (fullMoonLeft > 0f)
            {
                fullMoonLeft -= deltaTime;
                if (fullMoonLeft <= 0f)
                {
                    score.SetFullMoon(false);
                    water.SetFullMoonGlow(0f);
                    GameEvents.RaiseFullMoonEnded();
                }
            }

            while (distance + spawnAhead >= nextRowAt && !lighthouseSpawned)
            {
                SpawnRow(nextRowAt - distance);
            }

            MoveAndCollide(step);

            if (distance >= goal)
            {
                Complete();
            }
        }

        // ------------------------------------------------------------------ input

        private void ReadInput()
        {
            bool down;
            float x;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                down = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                x = touch.position.x;
                if (touch.phase == TouchPhase.Began && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    down = false;
                }
            }
            else
            {
                down = Input.GetMouseButton(0);
                x = Input.mousePosition.x;
                if (Input.GetMouseButtonDown(0) && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    down = false;
                }
            }

            if (!down)
            {
                dragging = false;
                return;
            }

            if (!dragging)
            {
                dragging = true;
                lastPointerX = x;
                return;
            }

            // Relative drag: the finger can start anywhere; moving it slides the moon (and the boat follows).
            targetX = Mathf.Clamp(targetX + (x - lastPointerX) / Screen.width * steerSensitivity, -laneHalfWidth, laneHalfWidth);
            lastPointerX = x;
        }

        // ------------------------------------------------------------------ spawning

        private void SpawnRow(float z)
        {
            float remaining = goal - nextRowAt;
            if (remaining < 25f)
            {
                Spawn(Kind.Lighthouse, 0.1f, goal - distance + 8f, 6.5f);
                lighthouseSpawned = true;
                return;
            }

            // Difficulty grows slowly with level and with how far into the voyage we are.
            float t = Mathf.Clamp01(levelIndex / 30f) * 0.6f + Mathf.Clamp01(nextRowAt / goal) * 0.4f;
            float rockChance = Mathf.Lerp(0.55f, 0.85f, t);
            int rocks = random.NextDouble() < rockChance ? (random.NextDouble() < t * 0.6f ? 2 : 1) : 0;

            // Five lanes; rocks never close them all, and there is always a free lane next to the last safe one.
            const int lanes = 5;
            float laneWidth = laneHalfWidth * 2f / (lanes - 1);
            var blocked = new bool[lanes];
            for (int i = 0; i < rocks; i++)
            {
                int lane = random.Next(lanes);
                blocked[lane] = true;
            }

            int free = 0;
            for (int i = 0; i < lanes; i++)
            {
                free += blocked[i] ? 0 : 1;
            }

            if (free < 3)
            {
                blocked[random.Next(lanes)] = false;
            }

            for (int i = 0; i < lanes; i++)
            {
                if (blocked[i])
                {
                    Spawn(Kind.Rock, 1.05f, z, -laneHalfWidth + i * laneWidth + (float)(random.NextDouble() - 0.5) * 0.5f);
                }
            }

            // Rewards live in the free lanes: a short star trail, sometimes coins or a moonstone.
            double roll = random.NextDouble();
            int rewardLane;
            do
            {
                rewardLane = random.Next(lanes);
            }
            while (blocked[rewardLane]);

            float rx = -laneHalfWidth + rewardLane * laneWidth;
            if (roll < 0.1)
            {
                Spawn(Kind.Moonstone, 0.9f, z, rx);
            }
            else if (roll < 0.35)
            {
                for (int k = 0; k < 3; k++)
                {
                    Spawn(Kind.Coin, 0.8f, z + k * 1.6f, rx);
                }
            }
            else
            {
                for (int k = 0; k < 4; k++)
                {
                    Spawn(Kind.Star, 0.85f, z + k * 1.5f, rx);
                    starsSpawned++;
                }
            }

            nextRowAt += Mathf.Lerp(10.5f, 7.5f, t);
        }

        private void Spawn(Kind kind, float radius, float z, float x)
        {
            Transform t = Take(kind);
            if (t == null)
            {
                return;
            }

            t.localRotation = kind == Kind.Rock ? Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f) : Quaternion.identity;
            float scale = kind == Kind.Rock ? 0.85f + (float)random.NextDouble() * 0.4f : 1f;
            t.localScale = Vector3.one * scale;
            props.Add(new Prop { Kind = kind, Transform = t, X = x, Z = z, Radius = radius * scale, Alive = true });
            Place(props[props.Count - 1]);
        }

        private Transform Take(Kind kind)
        {
            if (pool.TryGetValue(kind, out Stack<Transform> stack) && stack.Count > 0)
            {
                Transform reused = stack.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            GameObject prefab = PrefabFor(kind);
            return prefab != null ? Instantiate(prefab, transform).transform : null;
        }

        private void Release(Prop prop)
        {
            prop.Transform.gameObject.SetActive(false);
            if (!pool.TryGetValue(prop.Kind, out Stack<Transform> stack))
            {
                pool[prop.Kind] = stack = new Stack<Transform>();
            }

            stack.Push(prop.Transform);
        }

        private GameObject PrefabFor(Kind kind)
        {
            switch (kind)
            {
                case Kind.Rock: return rockPrefab;
                case Kind.Star: return starPrefab;
                case Kind.Coin: return coinPrefab;
                case Kind.Moonstone: return moonstonePrefab;
                default: return lighthousePrefab;
            }
        }

        private void ClearProps()
        {
            foreach (Prop prop in props)
            {
                Release(prop);
            }

            props.Clear();
        }

        // ------------------------------------------------------------------ movement & collisions

        private void MoveAndCollide(float step)
        {
            bool magnet = fullMoonLeft > 0f;
            for (int i = props.Count - 1; i >= 0; i--)
            {
                Prop p = props[i];
                p.Z -= step;

                bool pickup = p.Kind == Kind.Star || p.Kind == Kind.Coin || p.Kind == Kind.Moonstone;
                if (pickup && magnet && p.Z < 14f)
                {
                    float dist = Mathf.Abs(p.X - boatX) + Mathf.Abs(p.Z);
                    if (dist < magnetRadius * 2f)
                    {
                        p.X = Mathf.MoveTowards(p.X, boatX, step * 1.6f);
                    }
                }

                float dx = Mathf.Abs(p.X - boatX);
                if (p.Alive && Mathf.Abs(p.Z) < p.Radius + 0.7f && dx < p.Radius + 0.45f)
                {
                    if (p.Kind == Kind.Rock)
                    {
                        HitRock(ref p);
                    }
                    else if (pickup)
                    {
                        Collect(p.Kind);
                        p.Alive = false;
                        Release(p);
                        props.RemoveAt(i);
                        continue;
                    }
                }

                if (p.Z < despawnBehind)
                {
                    if (p.Kind == Kind.Star && p.Alive)
                    {
                        starStreak = 0;
                    }

                    Release(p);
                    props.RemoveAt(i);
                    continue;
                }

                props[i] = p;
                Place(p);
            }
        }

        private void Place(Prop p)
        {
            float y = SeaLevel();
            switch (p.Kind)
            {
                case Kind.Rock:
                    y -= 0.25f;
                    break;
                case Kind.Lighthouse:
                    break;
                default:
                    y += 0.85f + 0.12f * Mathf.Sin(Time.time * 3f + p.Z);
                    break;
            }

            p.Transform.localPosition = new Vector3(p.X, y, p.Z);
        }

        private void HitRock(ref Prop rock)
        {
            if (invulnerable > 0f)
            {
                return;
            }

            rock.Alive = false;
            hearts--;
            invulnerable = invulnerableSeconds;
            shake = 0.35f;
            starStreak = 0;
            // Nudge the boat away from the rock so the next frame is not a second hit.
            targetX = Mathf.Clamp(boatX + (boatX >= rock.X ? 1.6f : -1.6f), -laneHalfWidth, laneHalfWidth);

            if (hearts <= 0)
            {
                running = false;
                GameEvents.RaiseRunFailed(FailReason.Rock);
                return;
            }

            GameEvents.RaiseBoatBumped(hearts);
        }

        private void Collect(Kind kind)
        {
            switch (kind)
            {
                case Kind.Star:
                    starStreak++;
                    starsCollected++;
                    score.AddBonus(100);
                    GameEvents.RaiseStarCollected(starStreak);
                    break;
                case Kind.Coin:
                    coinsCollected++;
                    score.AddBonus(20);
                    GameEvents.RaiseCoinCollected(1);
                    break;
                case Kind.Moonstone:
                    moonstones++;
                    score.AddBonus(50);
                    GameEvents.RaiseMoonstoneCollected(Mathf.Min(moonstones, fullMoon.Required));
                    if (moonstones >= fullMoon.Required)
                    {
                        moonstones = 0;
                        fullMoonLeft = fullMoonSeconds;
                        score.SetFullMoon(true);
                        water.SetFullMoonGlow(1f);
                        GameEvents.RaiseFullMoonStarted(fullMoonSeconds);
                    }

                    break;
            }
        }

        private void Complete()
        {
            running = false;
            int two = Mathf.RoundToInt(starsSpawned * 100 * 0.45f);
            int three = Mathf.RoundToInt(starsSpawned * 100 * 0.75f);
            int stars = ScoreRules.StarRating(score.Score, two, three);
            var result = new LevelResult(levelIndex, stars, score.Score, levelTime, starsCollected, coinsCollected,
                0, 0, 0, false, hearts < maxHearts);
            GameEvents.RaiseLevelCompleted(result);
        }

        // ------------------------------------------------------------------ revive (rewarded "continue")

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Rewinding && from == GameState.Fail)
            {
                // Rewarded continue: one heart back and a clear stretch of sea ahead.
                hearts = 1;
                invulnerable = 2.5f;
                for (int i = props.Count - 1; i >= 0; i--)
                {
                    if (props[i].Kind == Kind.Rock && props[i].Z < 20f)
                    {
                        Release(props[i]);
                        props.RemoveAt(i);
                    }
                }

                running = true;
            }
            else if (to == GameState.Menu || to == GameState.Shop)
            {
                running = false;
                ClearProps();
                Shader.SetGlobalFloat(ScrollId, 0f);
            }
        }

        // ------------------------------------------------------------------ visuals

        private float SeaLevel() => tide != null ? tide.Level : 0f;

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
            // Models are authored facing +X for the old side view; turn them to face the open sea (+Z).
            boatModel.localRotation = Quaternion.Euler(0f, -90f, 0f);
            boatModel.localPosition = Vector3.zero;
            boatModelId = boat.Id;
        }

        private void LateUpdate()
        {
            if (boatModel == null)
            {
                if (session != null && session.Boat != null)
                {
                    SetBoat(session.Boat);
                }
                else if (defaultBoatModel != null && boatRoot != null)
                {
                    // Menu backdrop before any level has been played.
                    boatModel = Instantiate(defaultBoatModel, boatRoot).transform;
                    boatModel.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    boatModelId = null;
                }
            }

            float t = Time.time;
            float sea = SeaLevel();

            // Boat: bob on the swell, bank into turns, blink while invulnerable.
            if (boatRoot != null)
            {
                float bank = Mathf.Clamp(-steerVelocity * 6f, -22f, 22f);
                boatRoot.position = new Vector3(boatX, sea + 0.05f + 0.12f * Mathf.Sin(t * 1.9f), 0f);
                boatRoot.rotation = Quaternion.Euler(3f * Mathf.Sin(t * 1.3f), steerVelocity * 4f, bank + 2.5f * Mathf.Sin(t * 1.7f));
                if (boatModel != null)
                {
                    bool visible = invulnerable <= 0f || Mathf.Repeat(t * 10f, 1f) > 0.35f;
                    if (boatModel.gameObject.activeSelf != visible)
                    {
                        boatModel.gameObject.SetActive(visible);
                    }
                }
            }

            // The moon slides across the sky with the steering, so its light path always leads the boat.
            if (moonAnchor != null)
            {
                Vector3 local = moonAnchor.localPosition;
                local.x = Mathf.Lerp(local.x, boatX * 1.6f, 1f - Mathf.Exp(-6f * Time.deltaTime));
                local.z = 30f;
                moonAnchor.localPosition = local;
            }

            if (cameraTransform != null)
            {
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
                Vector3 jitter = shake > 0f ? Random.insideUnitSphere * shake * 0.4f : Vector3.zero;
                cameraTransform.position = new Vector3(boatX * 0.55f, sea, 0f) + cameraOffset + jitter;
                cameraTransform.rotation = Quaternion.Euler(cameraPitch, 0f, -steerVelocity * 0.6f);
            }
        }
    }
}
