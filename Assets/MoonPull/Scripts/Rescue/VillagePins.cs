using System.Collections.Generic;
using MoonPull.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.Rescue
{
    /// <summary>
    /// Tycoon map pins floating over the village: a gold coin pin over working buildings when the Treasury has coins
    /// (tap to collect) and a price pin over every island still for sale (tap to open the Islands tab). They bob,
    /// face the camera, and only show while the player is visiting the village.
    /// </summary>
    public sealed class VillagePins : MonoBehaviour
    {
        private sealed class Pin
        {
            public Transform Root;
            public SpriteRenderer Body;
            public TextMesh Label;
            public bool IsIsland;
            public int Index;
        }

        [SerializeField] private Transform cameraTransform;
        [SerializeField] private VillagePopup popup;
        [SerializeField] private Sprite pinSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite islandSprite;
        [SerializeField] private Font font;
        [SerializeField] private Transform[] buildingSites = new Transform[0];
        [SerializeField] private Transform[] islandSpots = new Transform[0];

        private readonly List<Pin> pins = new List<Pin>();
        private float nextRefresh;

        private void Start()
        {
            for (int i = 0; i < buildingSites.Length; i++)
            {
                pins.Add(Make(buildingSites[i], new Vector3(0f, 2.4f, 0f), coinSprite, false, i));
            }

            for (int i = 0; i < islandSpots.Length; i++)
            {
                pins.Add(Make(islandSpots[i], new Vector3(0f, 2.2f, 0f), islandSprite, true, i));
            }
        }

        private Pin Make(Transform anchor, Vector3 offset, Sprite icon, bool island, int index)
        {
            var root = new GameObject(island ? "IslandPin" : "CoinPin").transform;
            root.SetParent(anchor, false);
            root.localPosition = offset;
            root.localScale = Vector3.one * (island ? 1.3f : 1f);
            var body = root.gameObject.AddComponent<SpriteRenderer>();
            body.sprite = pinSprite;
            body.sortingOrder = 10;
            var iconGo = new GameObject("Icon").transform;
            iconGo.SetParent(root, false);
            iconGo.localPosition = new Vector3(0f, 0.16f, -0.01f);
            iconGo.localScale = Vector3.one * 0.5f;
            var iconRenderer = iconGo.gameObject.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = icon;
            iconRenderer.sortingOrder = 11;
            var collider = root.gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.6f;
            collider.center = new Vector3(0f, 0.1f, 0f);

            TextMesh label = null;
            if (island)
            {
                var labelGo = new GameObject("Price").transform;
                labelGo.SetParent(root, false);
                labelGo.localPosition = new Vector3(0f, -0.72f, -0.02f);
                label = labelGo.gameObject.AddComponent<TextMesh>();
                label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
                label.GetComponent<MeshRenderer>().sortingOrder = 12;
                label.fontSize = 64;
                label.characterSize = 0.045f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = new Color(0.1f, 0.07f, 0.28f);
            }

            root.gameObject.SetActive(false);
            return new Pin { Root = root, Body = body, Label = label, IsIsland = island, Index = index };
        }

        private void Update()
        {
            bool active = VillageDirector.Active;
            if (Time.unscaledTime >= nextRefresh || !active)
            {
                nextRefresh = Time.unscaledTime + 0.5f;
                int treasury = active ? TycoonState.Treasury : 0;
                foreach (Pin pin in pins)
                {
                    bool show;
                    if (pin.IsIsland)
                    {
                        show = active && !TycoonState.Owns(pin.Index);
                        bool ready = TycoonState.CanBuyNext(pin.Index);
                        pin.Body.color = ready ? new Color(0.36f, 0.83f, 0.36f) : new Color(0.55f, 0.58f, 0.75f);
                        pin.Label.text = TycoonState.IslandCost(pin.Index).ToString();
                    }
                    else
                    {
                        show = active && treasury >= 10 && VillageService.Level((VillageBuilding)pin.Index) > 0;
                        pin.Body.color = new Color(1f, 0.75f, 0.2f);
                    }

                    if (pin.Root.gameObject.activeSelf != show)
                    {
                        pin.Root.gameObject.SetActive(show);
                    }
                }
            }

            if (!active || cameraTransform == null)
            {
                return;
            }

            float t = Time.unscaledTime;
            foreach (Pin pin in pins)
            {
                if (!pin.Root.gameObject.activeSelf)
                {
                    continue;
                }

                pin.Root.rotation = cameraTransform.rotation;
                Vector3 p = pin.Root.localPosition;
                p.y = (pin.IsIsland ? 2.2f : 2.4f) + Mathf.Sin(t * 2.5f + pin.Index) * 0.12f;
                pin.Root.localPosition = p;
            }

            HandleTap();
        }

        private void HandleTap()
        {
            bool tapped = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (!tapped || popup == null)
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
            if (cam == null || !Physics.Raycast(cam.ScreenPointToRay(screen), out RaycastHit hit, 200f))
            {
                return;
            }

            foreach (Pin pin in pins)
            {
                if (hit.collider.transform == pin.Root)
                {
                    if (pin.IsIsland)
                    {
                        popup.ShowIslands();
                    }
                    else
                    {
                        popup.CollectFromWorld();
                    }

                    return;
                }
            }
        }
    }
}
