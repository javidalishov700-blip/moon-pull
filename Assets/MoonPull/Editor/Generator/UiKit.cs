using MoonPull.Ads;
using MoonPull.Localization;
using MoonPull.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Small uGUI construction kit. Layout reference is a 1080 × 1920 portrait canvas; positions are in canvas units
    /// relative to an anchor, so the UI scales cleanly from 16:9 to 20:9 phones.
    /// </summary>
    internal static class UiKit
    {
        public static readonly Color Ink = Gen.Hex("0B1026");
        public static readonly Color Panel = Gen.Hex("1E2552");
        public static readonly Color PanelLight = Gen.Hex("2C3570");
        public static readonly Color Primary = Gen.Hex("FFB547");
        public static readonly Color Accent = Gen.Hex("7FE3C4");
        public static readonly Color RewardedColor = Gen.Hex("8E7CFF");
        public static readonly Color Muted = Gen.Hex("A3ABDB");
        public static readonly Color TextLight = Gen.Hex("F4F1FF");
        public static readonly Color Gold = Gen.Hex("FFD95C");
        public static readonly Color Danger = Gen.Hex("FF6B6B");

        private static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = Center;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, Sprite sprite = null)
        {
            RectTransform rt = Rect(name, parent, anchor, position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : Art.Rounded;
            image.type = image.sprite == Art.Rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            return image;
        }

        public static Image Fill(Image image, UnityEngine.UI.Image.FillMethod method, float amount)
        {
            if (image.sprite == null || image.sprite == Art.Rounded)
            {
                image.sprite = Art.Square;
            }

            image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = method;
            image.fillOrigin = 0;
            image.fillAmount = amount;
            return image;
        }

        public static Image Backdrop(Transform parent, Color color)
        {
            RectTransform rt = Stretch("Backdrop", parent);
            // Bleed past the safe area and banner inset so backdrops always reach the physical screen edges.
            rt.offsetMin = new Vector2(-400f, -700f);
            rt.offsetMax = new Vector2(400f, 700f);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Text(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position,
            Vector2 box, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
        {
            RectTransform rt = Rect(name, parent, anchor, position, box);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return text;
        }

        /// <summary>Localized label: the key shows in the Editor until boot loads the table.</summary>
        public static LocalizedText Loc(Transform parent, string name, string key, int size, Vector2 anchor, Vector2 position,
            Vector2 box, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
        {
            Text text = Text(parent, name, key, size, anchor, position, box, color, align, bold);
            var loc = text.gameObject.AddComponent<LocalizedText>();
            Gen.Set(loc, "key", key);
            return loc;
        }

        public static Button Button(Transform parent, string name, string key, Vector2 anchor, Vector2 position, Vector2 size,
            Color color, int fontSize = 52, bool localized = true)
        {
            Image image = Image(parent, name, anchor, position, size, color);
            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
            button.colors = colors;
            Color label = color.grayscale > 0.6f ? Ink : TextLight;
            if (localized)
            {
                Loc(image.transform, "Label", key, fontSize, Center, Vector2.zero, size - new Vector2(24f, 8f), label, TextAnchor.MiddleCenter, true);
            }
            else
            {
                Text(image.transform, "Label", key, fontSize, Center, Vector2.zero, size - new Vector2(24f, 8f), label, TextAnchor.MiddleCenter, true);
            }

            return button;
        }

        public static RewardedButton Rewarded(Transform parent, string name, string key, AdPlacement placement, Vector2 anchor,
            Vector2 position, Vector2 size, AdsCoordinator ads, bool pulse)
        {
            Button button = Button(parent, name, key, anchor, position, size, RewardedColor, 48);
            // Small "ad" play badge so the reward is clearly an opt-in video.
            Image badge = Image(button.transform, "AdBadge", new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(64f, 44f), Gold);
            Text(badge.transform, "Play", "AD", 24, Center, Vector2.zero, new Vector2(64f, 44f), Ink, TextAnchor.MiddleCenter, true);
            var group = button.gameObject.AddComponent<CanvasGroup>();
            var rewarded = button.gameObject.AddComponent<RewardedButton>();
            Gen.Wire(rewarded, "coordinator", ads, "placement", placement, "visual", group, "pulse", pulse);
            return rewarded;
        }

        public static Badge Badge(Transform parent, Vector2 position)
        {
            Image dot = Image(parent, "Badge", new Vector2(1f, 1f), position, new Vector2(56f, 56f), Danger, Art.Circle);
            Text count = Text(dot.transform, "Count", "", 30, Center, Vector2.zero, new Vector2(56f, 56f), Color.white, TextAnchor.MiddleCenter, true);
            var badge = dot.gameObject.AddComponent<Badge>();
            Gen.Wire(badge, "root", dot.gameObject, "count", count);
            return badge;
        }

        public static RectTransform CoinIcon(Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            Image image = Image(parent, "Coin", anchor, position, new Vector2(size, size), Color.white, Art.Coin());
            image.raycastTarget = false;
            return image.rectTransform;
        }

        public static CanvasGroup Group(GameObject go)
        {
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }
    }
}
