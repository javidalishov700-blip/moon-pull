using System.Collections.Generic;
using MoonPull.Meta;
using MoonPull.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.Rescue
{
    /// <summary>
    /// The village you can visit. Opening the Village screen flies the camera to the harbor island, where the people
    /// you have rescued stroll between the buildings you have built. Now and then someone needs you: a hungry
    /// villager (feed them from the Restaurant's pantry), someone without a roof (build a Shelter), someone with a
    /// small gift, or someone who just wants a chat. Tapping them helps, cheers the village up and earns Village XP.
    /// </summary>
    public sealed class VillageDirector : MonoBehaviour
    {
        private enum Need { None, Hungry, Homeless, Gift, Chat }

        private sealed class Villager
        {
            public Transform Body;
            public Vector3 Target;
            public float Wait;
            public Need Need;
            public GameObject Bubble;
            public TextMesh Mark;
            public Renderer BubbleRenderer;
        }

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private GameObject villagerPrefab;
        [SerializeField] private MetaGame meta;
        [SerializeField] private Toast toast;
        [SerializeField] private Vector2 walkArea = new Vector2(4.6f, 2.2f);
        [SerializeField] private float groundHeight = 0.45f;
        [SerializeField] private Vector3 viewOffset = new Vector3(0f, 9f, -10.5f);
        [SerializeField] private int maxVisible = 30;

        /// <summary>True while the player is visiting the village; the sea camera stands aside.</summary>
        public static bool Active { get; private set; }

        private readonly List<Villager> villagers = new List<Villager>();
        private System.Random random = new System.Random(77);
        private float nextNeedAt;
        private float nextSimulateAt;
        private Material bubbleMaterial;

        public void Enter()
        {
            Active = true;
            VillageState.Simulate();
            Populate();
            nextNeedAt = Time.unscaledTime + 1.5f;
            if (toast != null && villagers.Count > 0)
            {
                toast.Show("village.tap_hint");
            }
        }

        public void Exit()
        {
            Active = false;
        }

        private void Populate()
        {
            int wanted = Mathf.Clamp(VillageState.Population, 1, maxVisible);
            while (villagers.Count < wanted && villagerPrefab != null)
            {
                Transform body = Instantiate(villagerPrefab, transform).transform;
                body.localPosition = RandomSpot();
                body.localScale = Vector3.one * 1.25f;
                var collider = body.gameObject.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.5f, 0f);
                collider.height = 1.6f;
                collider.radius = 0.45f;
                villagers.Add(new Villager { Body = body, Target = RandomSpot(), Wait = (float)random.NextDouble() * 2f, Bubble = MakeBubble(body) });
            }
        }

        private GameObject MakeBubble(Transform body)
        {
            var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bubble.GetComponent<Collider>());
            bubble.transform.SetParent(body, false);
            bubble.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            bubble.transform.localScale = Vector3.one * 0.55f;
            var renderer = bubble.GetComponent<Renderer>();
            if (bubbleMaterial == null)
            {
                bubbleMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            renderer.sharedMaterial = bubbleMaterial;

            var markGo = new GameObject("Mark");
            markGo.transform.SetParent(bubble.transform, false);
            markGo.transform.localPosition = new Vector3(0f, 0f, -0.55f);
            var mark = markGo.AddComponent<TextMesh>();
            mark.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mark.GetComponent<MeshRenderer>().sharedMaterial = mark.font.material;
            mark.fontSize = 64;
            mark.characterSize = 0.05f;
            mark.anchor = TextAnchor.MiddleCenter;
            mark.alignment = TextAlignment.Center;
            mark.fontStyle = FontStyle.Bold;
            mark.color = new Color(0.1f, 0.1f, 0.2f);
            bubble.SetActive(false);
            return bubble;
        }

        private Vector3 RandomSpot()
        {
            float a = (float)random.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt((float)random.NextDouble());
            return new Vector3(Mathf.Cos(a) * walkArea.x * r, groundHeight, Mathf.Sin(a) * walkArea.y * r - 0.3f);
        }

        private void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Time.unscaledTime >= nextSimulateAt)
            {
                nextSimulateAt = Time.unscaledTime + 60f;
                VillageState.Simulate();
            }

            Populate();
            Walk();
            AssignNeeds();
            HandleTap();
        }

        private void Walk()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (Villager v in villagers)
            {
                if (v.Need != Need.None)
                {
                    // Someone who needs you stops and bobs, facing the camera.
                    v.Body.localRotation = Quaternion.Slerp(v.Body.localRotation, Quaternion.Euler(0f, 180f, 0f), dt * 6f);
                    v.Bubble.transform.localPosition = new Vector3(0f, 1.35f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f), 0f);
                    continue;
                }

                if (v.Wait > 0f)
                {
                    v.Wait -= dt;
                    continue;
                }

                Vector3 to = v.Target - v.Body.localPosition;
                to.y = 0f;
                if (to.magnitude < 0.1f)
                {
                    v.Target = RandomSpot();
                    v.Wait = 0.5f + (float)random.NextDouble() * 3f;
                    continue;
                }

                Vector3 step = to.normalized * 0.7f * dt;
                v.Body.localPosition += step;
                Vector3 p = v.Body.localPosition;
                p.y = groundHeight + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f + v.Body.GetInstanceID())) * 0.06f;
                v.Body.localPosition = p;
                v.Body.localRotation = Quaternion.Slerp(v.Body.localRotation, Quaternion.LookRotation(to.normalized), dt * 8f);
            }
        }

        private void AssignNeeds()
        {
            if (Time.unscaledTime < nextNeedAt || villagers.Count == 0)
            {
                return;
            }

            nextNeedAt = Time.unscaledTime + 6f + (float)random.NextDouble() * 10f;
            int open = 0;
            foreach (Villager v in villagers)
            {
                open += v.Need != Need.None ? 1 : 0;
            }

            if (open >= Mathf.Max(1, villagers.Count / 4))
            {
                return;
            }

            Villager pick = villagers[random.Next(villagers.Count)];
            if (pick.Need != Need.None)
            {
                return;
            }

            double roll = random.NextDouble();
            Need need = VillageState.Food < 3f && roll < 0.6 ? Need.Hungry
                : VillageState.Homeless > 0 && roll < 0.5 ? Need.Homeless
                : roll < 0.55 ? Need.Gift
                : Need.Chat;
            SetNeed(pick, need);
        }

        private void SetNeed(Villager v, Need need)
        {
            v.Need = need;
            v.Bubble.SetActive(need != Need.None);
            if (need == Need.None)
            {
                return;
            }

            Color color;
            string mark;
            switch (need)
            {
                case Need.Hungry: color = new Color(1f, 0.6f, 0.3f); mark = "!"; break;
                case Need.Homeless: color = new Color(0.5f, 0.7f, 1f); mark = "?"; break;
                case Need.Gift: color = new Color(1f, 0.85f, 0.3f); mark = "$"; break;
                default: color = new Color(1f, 0.6f, 0.8f); mark = "+"; break;
            }

            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            v.Bubble.GetComponent<Renderer>().SetPropertyBlock(block);
            v.Bubble.GetComponentInChildren<TextMesh>().text = mark;
        }

        private void HandleTap()
        {
            bool tapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (!tapped || cameraTransform == null)
            {
                return;
            }

            Vector2 screen = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            int pointer = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointer))
            {
                return;
            }

            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam == null || !Physics.Raycast(cam.ScreenPointToRay(screen), out RaycastHit hit, 100f))
            {
                return;
            }

            foreach (Villager v in villagers)
            {
                if (hit.collider.transform == v.Body)
                {
                    Help(v);
                    return;
                }
            }
        }

        private void Help(Villager v)
        {
            switch (v.Need)
            {
                case Need.Hungry:
                    if (VillageState.TryEat(1f))
                    {
                        VillageState.AddCheer(0.05f);
                        Reward(8, "village.fed");
                    }
                    else if (meta != null && meta.Wallet.TrySpendCoins(5, "village_food"))
                    {
                        VillageState.AddCheer(0.05f);
                        Reward(8, "village.fed_coins");
                    }
                    else
                    {
                        Say("village.tip.hungry");
                        return;
                    }

                    break;
                case Need.Homeless:
                    VillageState.AddCheer(0.02f);
                    Say("village.tip.homeless");
                    break;
                case Need.Gift:
                    int coins = 10 + VillageState.Level * 5;
                    if (meta != null)
                    {
                        meta.Wallet.AddCoins(coins, "village_gift");
                    }

                    Reward(10, "village.gift", coins);
                    break;
                case Need.Chat:
                    VillageState.AddCheer(0.08f);
                    Reward(5, "village.chat");
                    break;
                default:
                    // A friendly wave from anyone without a request.
                    v.Wait = 1f;
                    return;
            }

            SetNeed(v, Need.None);
            v.Body.localScale = Vector3.one * 1.45f;
            v.Target = RandomSpot();
        }

        private void Reward(int xp, string key, params object[] args)
        {
            int levels = VillageState.AddXp(xp);
            Say(key, args);
            if (levels > 0 && meta != null)
            {
                int reward = VillageState.LevelReward(VillageState.Level);
                meta.Wallet.AddCoins(reward, "village_level");
                Say("village.levelup", VillageState.Level, reward);
            }
        }

        private void Say(string key, params object[] args)
        {
            if (toast != null)
            {
                toast.Show(key, args);
            }
        }

        private void LateUpdate()
        {
            foreach (Villager v in villagers)
            {
                if (v.Body.localScale.x > 1.26f)
                {
                    v.Body.localScale = Vector3.MoveTowards(v.Body.localScale, Vector3.one * 1.25f, Time.unscaledDeltaTime);
                }
            }

            if (!Active || cameraTransform == null)
            {
                return;
            }

            // Fly the camera over to the island and look down on the village.
            Vector3 target = transform.position + viewOffset;
            float t = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, t);
            // Aim below the island so it sits in the top half of the screen, above the Village sheet.
            Quaternion look = Quaternion.LookRotation(transform.position + new Vector3(0f, -4.5f, 2f) - target);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, look, t);
        }
    }
}
