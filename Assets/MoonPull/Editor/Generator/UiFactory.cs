using System.Collections.Generic;
using MoonPull.Ads;
using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Tutorial;
using MoonPull.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MoonPull.EditorTools.UiKit;

namespace MoonPull.EditorTools
{
    /// <summary>Builds the whole UI: screens, popups, HUD widgets, tutorial hand, toast and coin fly.</summary>
    internal static class UiFactory
    {
        private static SceneFactory.World w;
        private static PopupManager popups;
        private static CoinFlyEffect coinFly;
        private static Toast toast;
        private static Canvas canvas;

        public static void Build(SceneFactory.World world)
        {
            w = world;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            GameObject canvasGo = new GameObject("UI", typeof(RectTransform));
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Portrait game: match width, so tall phones (19.5:9) gain vertical room instead of clipping the sides.
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            popups = canvasGo.AddComponent<PopupManager>();
            var router = canvasGo.AddComponent<UiRouter>();

            RectTransform safe = Stretch("SafeArea", canvasGo.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            RectTransform bannerSafe = Stretch("BannerSafe", safe);
            var bannerArea = bannerSafe.gameObject.AddComponent<BannerSafeArea>();
            Gen.Wire(bannerArea, "coordinator", w.Ads, "canvas", canvas);

            // Global overlays first so screens can reference them; re-ordered to the top at the end.
            CoinCounter counter = BuildTopBar(safe);
            coinFly = BuildCoinFly(canvasGo.transform, counter);
            toast = BuildToast(canvasGo.transform);

            UIScreen loading = BuildLoading(bannerSafe);
            HudScreen hud = BuildHud(bannerSafe);
            FailScreen fail = BuildFail(bannerSafe);
            WinScreen win = BuildWin(bannerSafe);
            ShopScreen shop = BuildShop(bannerSafe);

            IdleIncomePopup idle = BuildIdle(bannerSafe);
            LoginStreakPopup streak = BuildStreak(bannerSafe);
            DailySpinPopup spin = BuildSpin(bannerSafe);
            MissionsPopup missions = BuildMissions(bannerSafe);
            SettingsPopup settings = BuildSettings(bannerSafe);
            LighthousePopup lighthouse = BuildLighthouse(bannerSafe, idle);
            BossChestPopup chest = BuildBossChest(bannerSafe);
            PausePopup pause = BuildPause(bannerSafe);
            VillagePopup village = BuildVillage(bannerSafe);
            MenuScreen menu = BuildMenu(bannerSafe, idle, streak, spin, missions, settings, village, chest);
            Gen.Set(village, "hideWhileOpen", menu.GetComponent<CanvasGroup>());
            Gen.Wire(hud, "pausePopup", pause);

            // Popups draw above every full screen (the menu is built last because it references them).
            foreach (Component popup in new Component[] { idle, streak, spin, missions, settings, lighthouse, village, chest, pause })
            {
                popup.transform.SetAsLastSibling();
            }

            w.Hand = BuildHand(canvasGo.transform);

            var debug = canvasGo.AddComponent<DebugPanel>();
            Gen.Wire(debug, "meta", w.Meta, "session", w.Session, "gameManager", w.GameManager);

            Gen.Wire(router, "loading", loading, "menu", menu, "hud", hud, "fail", fail, "win", win, "shop", shop, "popups", popups);

            counter.transform.parent.SetAsLastSibling();
            w.Hand.transform.SetAsLastSibling();
            toast.transform.SetAsLastSibling();
            coinFly.transform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------- framework

        private static T Screen<T>(Transform parent, string name) where T : UIScreen
        {
            RectTransform root = Stretch(name, parent);
            Group(root.gameObject);
            RectTransform content = Stretch("Content", root);
            var screen = root.gameObject.AddComponent<T>();
            Gen.Set(screen, "content", content);
            return screen;
        }

        private static RectTransform ContentOf(UIScreen screen) => (RectTransform)screen.transform.Find("Content");

        private static T Popup<T>(Transform parent, string name, string titleKey, Vector2 size, out RectTransform panel) where T : UIPopup
        {
            RectTransform root = Stretch(name, parent);
            Group(root.gameObject);
            Backdrop(root, new Color(0.02f, 0.03f, 0.1f, 0.72f));
            panel = Image(root, "Content", Center, Vector2.zero, size, Panel).rectTransform;
            // Title ribbon riding on the top edge of the panel.
            Image ribbon = Image(panel, "Header", Top, new Vector2(0f, -40f), new Vector2(Mathf.Min(size.x - 160f, 720f), 120f), Primary, Art.ChunkyButton);
            ribbon.raycastTarget = false;
            Loc(panel, "Title", titleKey, 58, Top, new Vector2(0f, -34f), new Vector2(Mathf.Min(size.x - 200f, 680f), 100f), TextLight, TextAnchor.MiddleCenter, true);
            Button close = Button(panel, "Close", "X", TopRight, new Vector2(-40f, -40f), new Vector2(100f, 100f), Danger, 50, false);
            var popup = root.gameObject.AddComponent<T>();
            Gen.Wire(popup, "content", panel, "popups", popups, "closeButton", close);
            return popup;
        }

        // ---------------------------------------------------------------- global

        private static CoinCounter BuildTopBar(Transform safe)
        {
            RectTransform bar = Rect("TopBar", safe, TopLeft, new Vector2(210f, -90f), new Vector2(340f, 100f));
            Group(bar.gameObject);
            var visibility = bar.gameObject.AddComponent<StateVisibility>();
            Image pill = Image(bar, "Pill", Center, new Vector2(20f, 0f), new Vector2(320f, 84f), Gen.Hex("10163A"), Art.Pill);
            RectTransform icon = CoinIcon(pill.transform, new Vector2(0f, 0.5f), new Vector2(10f, 2f), 104f);
            Text label = Text(pill.transform, "Coins", "0", 48, new Vector2(0f, 0.5f), new Vector2(190f, 2f), new Vector2(210f, 80f), TextLight, TextAnchor.MiddleLeft, true);
            // Green "+" at the end of the coin pill: straight to the shop.
            Button add = Button(pill.transform, "Add", "", new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(76f, 76f), Go, 30, false);
            Image plus = Image(add.transform, "Icon", Center, new Vector2(0f, 3f), new Vector2(44f, 44f), Color.white, Art.MenuIcon("plus"));
            plus.raycastTarget = false;
            add.gameObject.AddComponent<ShopShortcut>();
            var counter = pill.gameObject.AddComponent<CoinCounter>();
            Gen.Wire(counter, "meta", w.Meta, "label", label, "icon", icon);
            _ = visibility;
            return counter;
        }

        private static CoinFlyEffect BuildCoinFly(Transform root, CoinCounter counter)
        {
            RectTransform layer = Stretch("CoinFly", root);
            var icons = new List<RectTransform>();
            for (int i = 0; i < 12; i++)
            {
                Image coin = Image(layer, "Coin" + i, Center, Vector2.zero, new Vector2(72f, 72f), Color.white, Art.Coin());
                coin.raycastTarget = false;
                icons.Add(coin.rectTransform);
            }

            var effect = layer.gameObject.AddComponent<CoinFlyEffect>();
            Gen.Wire(effect, "counter", counter, "worldCamera", w.Camera);
            Gen.SetArray(effect, "icons", icons);
            return effect;
        }

        private static Toast BuildToast(Transform root)
        {
            Image bg = Image(root, "Toast", Top, new Vector2(0f, -520f), new Vector2(940f, 130f), new Color(0.05f, 0.06f, 0.15f, 0.92f));
            bg.raycastTarget = false;
            var group = Group(bg.gameObject);
            Text label = Text(bg.transform, "Label", "", 40, Center, Vector2.zero, new Vector2(880f, 120f), TextLight);
            var t = bg.gameObject.AddComponent<Toast>();
            Gen.Wire(t, "coordinator", w.Ads, "group", group, "label", label);
            return t;
        }

        private static HandHint BuildHand(Transform root)
        {
            RectTransform layer = Rect("HandHint", root, Center, Vector2.zero, new Vector2(1080f, 1920f));
            var group = Group(layer.gameObject);
            group.blocksRaycasts = false;
            Image hand = Image(layer, "Hand", Center, new Vector2(300f, 250f), new Vector2(170f, 170f), Color.white, Art.Hand());
            hand.raycastTarget = false;
            var hint = layer.gameObject.AddComponent<HandHint>();
            Gen.Wire(hint, "hand", hand.rectTransform, "group", group,
                "low", new Vector2(300f, 20f), "mid", new Vector2(300f, 250f), "high", new Vector2(300f, 480f));
            return hint;
        }

        // ---------------------------------------------------------------- screens

        private static UIScreen BuildLoading(Transform parent)
        {
            var screen = Screen<LoadingScreen>(parent, "Loading");
            RectTransform c = ContentOf(screen);
            Backdrop(c, Ink);
            Image(c, "Moon", Center, new Vector2(0f, 420f), new Vector2(300f, 300f), Gen.Hex("FFF6DC"), Art.Circle);
            Text(c, "Title", "MOON PULL", 120, Center, new Vector2(0f, 120f), new Vector2(1000f, 160f), TextLight, TextAnchor.MiddleCenter, true);
            Loc(c, "Loading", LocKeys.CommonLoading, 44, Center, new Vector2(0f, -200f), new Vector2(800f, 80f), Muted);
            return screen;
        }

        /// <summary>Square menu tile: a drawn icon over a small label, on a chunky coloured button.</summary>
        private static Button IconTile(Transform parent, string name, string key, Vector2 position, Color color, string icon)
        {
            Button button = Button(parent, name, key, Bottom, position, new Vector2(236f, 200f), color, 32);
            var label = (RectTransform)button.transform.Find("Label");
            label.anchoredPosition = new Vector2(0f, -52f);
            label.sizeDelta = new Vector2(226f, 50f);
            Image glyph = Image(button.transform, "Icon", Center, new Vector2(0f, 26f), new Vector2(128f, 128f), Color.white, Art.MenuIcon(icon));
            glyph.raycastTarget = false;
            return button;
        }

        private static MenuScreen BuildMenu(Transform parent, UIScreen idle, UIScreen streak, UIScreen spin, UIScreen missions,
            UIScreen settings, UIScreen lighthouse, UIScreen chest)
        {
            var screen = Screen<MenuScreen>(parent, "Menu");
            RectTransform c = ContentOf(screen);

            Text(c, "Title", "MOON PULL", 132, Top, new Vector2(0f, -280f), new Vector2(1000f, 170f), Gold, TextAnchor.MiddleCenter, true);
            Text(c, "Subtitle", "Night Rescue", 50, Top, new Vector2(0f, -370f), new Vector2(800f, 70f), TextLight, TextAnchor.MiddleCenter, true);
            LocalizedText region = Loc(c, "Region", "region.tropical_lagoon", 42, Top, new Vector2(0f, -430f), new Vector2(900f, 70f), Accent);
            LocalizedText level = Loc(c, "Level", LocKeys.MenuLevel, 66, Top, new Vector2(0f, -490f), new Vector2(900f, 90f), TextLight, TextAnchor.MiddleCenter, true);
            Image starIcon = Image(c, "StarIcon", Top, new Vector2(-50f, -580f), new Vector2(64f, 64f), Color.white, Art.Star(true));
            starIcon.raycastTarget = false;
            Text stars = Text(c, "Stars", "0", 48, Top, new Vector2(72f, -580f), new Vector2(160f, 70f), Gold, TextAnchor.MiddleLeft, true);
            LocalizedText locked = Loc(c, "Locked", LocKeys.LighthouseRegionLocked, 38, Top, new Vector2(0f, -660f), new Vector2(900f, 60f), Primary);

            Button play = Button(c, "Play", LocKeys.MenuPlay, Bottom, new Vector2(0f, 560f), new Vector2(680f, 220f), Go, 96);
            // Record pill: your best night and the next player to beat; tap for the Game Center leaderboard.
            Image recordPill = Image(c, "Record", Bottom, new Vector2(0f, 710f), new Vector2(780f, 86f), new Color(0.05f, 0.07f, 0.18f, 0.82f), Art.Rounded);
            Image trophy = Image(recordPill.transform, "Trophy", new Vector2(0f, 0.5f), new Vector2(52f, 2f), new Vector2(72f, 72f), Color.white, Art.MenuIcon("trophy"));
            trophy.raycastTarget = false;
            LocalizedText recordText = Loc(recordPill.transform, "Text", "menu.record_none", 34, Center, new Vector2(36f, 0f), new Vector2(680f, 76f), Gold, TextAnchor.MiddleCenter, true);
            recordText.GetComponent<Text>().raycastTarget = false;
            Gen.Wire(recordPill.gameObject.AddComponent<RecordLabel>(), "label", recordText);
            recordPill.gameObject.AddComponent<LeaderboardButton>();
            Button settingsButton = Button(c, "Settings", "", TopRight, new Vector2(-90f, -90f), new Vector2(116f, 116f), Gen.Hex("2F7BE0"), 34, false);
            Image gear = Image(settingsButton.transform, "Icon", Center, new Vector2(0f, 4f), new Vector2(76f, 76f), Color.white, Art.MenuIcon("gear"));
            gear.raycastTarget = false;
            Button chestButton = Button(c, "BossChest", LocKeys.ChestTitle, TopLeft, new Vector2(210f, -210f), new Vector2(340f, 96f), Gen.Hex("B8742E"), 34);
            Badge chestBadge = Badge(chestButton.transform, new Vector2(-10f, -10f));
            Text goal = Text(c, "Goal", "", 34, Top, new Vector2(0f, -725f), new Vector2(960f, 60f), Gold, TextAnchor.MiddleCenter, true);

            float y = 320f;
            Button shop = IconTile(c, "Shop", LocKeys.MenuShop, new Vector2(-390f, y), PanelLight, "bag");
            Button lighthouseButton = IconTile(c, "Lighthouse", LocKeys.MenuLighthouse, new Vector2(-130f, y), PanelLight, "house");
            Button missionsButton = IconTile(c, "Missions", LocKeys.MenuMissions, new Vector2(130f, y), PanelLight, "list");
            Button spinButton = IconTile(c, "Spin", LocKeys.MenuSpin, new Vector2(390f, y), PanelLight, "wheel");
            Badge missionsBadge = Badge(missionsButton.transform, new Vector2(-12f, -12f));
            Badge villageBadge = Badge(lighthouseButton.transform, new Vector2(-12f, -12f));
            Badge spinBadge = Badge(spinButton.transform, new Vector2(-12f, -12f));

            Gen.Wire(screen, "meta", w.Meta, "popups", popups, "levelLabel", level, "regionLabel", region, "starsLabel", stars,
                "lockedLabel", locked, "playButton", play, "shopButton", shop, "lighthouseButton", lighthouseButton,
                "missionsButton", missionsButton, "spinButton", spinButton, "settingsButton", settingsButton, "bossChestButton", chestButton,
                "missionsBadge", missionsBadge, "spinBadge", spinBadge, "chestBadge", chestBadge, "villageBadge", villageBadge, "goalLabel", goal,
                "idlePopup", idle, "streakPopup", streak, "spinPopup", spin, "missionsPopup", missions, "settingsPopup", settings,
                "lighthousePopup", lighthouse, "bossChestPopup", chest);
            return screen;
        }

        private static HudScreen BuildHud(Transform parent)
        {
            var screen = Screen<HudScreen>(parent, "Hud");
            RectTransform c = ContentOf(screen);

            GameObject sliderGo = DefaultControls.CreateSlider(Resources());
            sliderGo.name = "Progress";
            var sliderRt = (RectTransform)sliderGo.transform;
            sliderRt.SetParent(c, false);
            sliderRt.anchorMin = sliderRt.anchorMax = Top;
            sliderRt.anchoredPosition = new Vector2(0f, -90f);
            sliderRt.sizeDelta = new Vector2(640f, 30f);
            var slider = sliderGo.GetComponent<Slider>();
            slider.interactable = false;
            if (slider.handleRect != null)
            {
                slider.handleRect.gameObject.SetActive(false);
            }

            if (slider.fillRect != null && slider.fillRect.GetComponent<Image>() is Image fill)
            {
                fill.color = Accent;
            }

            Button pause = Button(c, "Pause", "II", TopLeft, new Vector2(90f, -90f), new Vector2(120f, 120f), new Color(0f, 0f, 0f, 0.4f), 52, false);
            Text score = Text(c, "Score", "0", 80, Top, new Vector2(0f, -170f), new Vector2(600f, 100f), TextLight, TextAnchor.MiddleCenter, true);
            score.gameObject.SetActive(false); // abstract points only cluttered the screen; people, lanterns and coins tell the story
            Text multiplier = Text(c, "Multiplier", "x2", 50, Top, new Vector2(0f, -245f), new Vector2(300f, 70f), Gold, TextAnchor.MiddleCenter, true);

            Image meter = Image(c, "Moonstones", TopRight, new Vector2(-150f, -90f), new Vector2(240f, 70f), new Color(0f, 0f, 0f, 0.4f));
            Image meterFill = Fill(Image(meter.transform, "Fill", Center, Vector2.zero, new Vector2(228f, 58f), Gen.Hex("BDE6FF")), UnityEngine.UI.Image.FillMethod.Horizontal, 0f);
            Image(meter.transform, "Gem", new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(70f, 70f), Color.white, Art.Moonstone());
            Text meterLabel = Text(meter.transform, "Label", "0/5", 40, Center, new Vector2(20f, 0f), new Vector2(200f, 60f), Ink, TextAnchor.MiddleCenter, true);

            Image fullMoon = Image(c, "FullMoonBanner", Top, new Vector2(0f, -330f), new Vector2(640f, 90f), new Color(0.85f, 0.9f, 1f, 0.9f));
            Loc(fullMoon.transform, "Label", LocKeys.HudFullMoon, 50, Center, new Vector2(0f, 8f), new Vector2(620f, 70f), Ink, TextAnchor.MiddleCenter, true);
            Image fullMoonTimer = Fill(Image(fullMoon.transform, "Timer", Bottom, new Vector2(0f, 8f), new Vector2(600f, 10f), Gen.Hex("8E7CFF")), UnityEngine.UI.Image.FillMethod.Horizontal, 1f);

            RectTransform calloutRt = Rect("Callout", c, Center, new Vector2(0f, 300f), new Vector2(1000f, 260f));
            var callout = Group(calloutRt.gameObject);
            callout.blocksRaycasts = false;
            LocalizedText calloutText = Loc(calloutRt, "Text", LocKeys.HudNearMiss, 86, Center, new Vector2(0f, 40f), new Vector2(1000f, 120f), Gold, TextAnchor.MiddleCenter, true);
            Text calloutMultiplier = Text(calloutRt, "Multiplier", "x2", 64, Center, new Vector2(0f, -60f), new Vector2(400f, 90f), TextLight, TextAnchor.MiddleCenter, true);

            Image gauge = Image(c, "PassengerGauge", Bottom, new Vector2(0f, 460f), new Vector2(440f, 60f), new Color(0f, 0f, 0f, 0.45f));
            Image zone = Image(gauge.transform, "Zone", Center, Vector2.zero, new Vector2(110f, 52f), Accent);
            Image marker = Image(gauge.transform, "Marker", Center, Vector2.zero, new Vector2(18f, 86f), Color.white);
            Loc(gauge.transform, "Hint", LocKeys.HudAlign, 34, Center, new Vector2(0f, 70f), new Vector2(440f, 50f), TextLight);

            Image weatherBg = Image(c, "Weather", Top, new Vector2(0f, -430f), new Vector2(760f, 100f), new Color(0f, 0f, 0f, 0.5f));
            var weatherGroup = Group(weatherBg.gameObject);
            weatherGroup.blocksRaycasts = false;
            Image weatherIcon = Image(weatherBg.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(84f, 84f), Color.white, Art.WeatherIcon("storm"));
            LocalizedText weatherText = Loc(weatherBg.transform, "Text", LocKeys.HudStorm, 42, Center, new Vector2(40f, 0f), new Vector2(620f, 90f), TextLight, TextAnchor.MiddleCenter, true);

            Image boss = Image(c, "Boss", Top, new Vector2(0f, -520f), new Vector2(620f, 70f), new Color(0.3f, 0.1f, 0.35f, 0.85f));
            Image bossFill = Fill(Image(boss.transform, "Fill", Center, Vector2.zero, new Vector2(608f, 58f), Gen.Hex("B784FF")), UnityEngine.UI.Image.FillMethod.Horizontal, 0f);
            Text bossLabel = Text(boss.transform, "Label", "0/3", 40, Center, Vector2.zero, new Vector2(600f, 60f), TextLight, TextAnchor.MiddleCenter, true);

            RectTransform rewind = Stretch("Rewind", c);
            var rewindTint = rewind.gameObject.AddComponent<Image>();
            rewindTint.color = new Color(0.35f, 0.45f, 1f, 0.22f);
            rewindTint.raycastTarget = false;
            Text(rewind, "Icon", "<<", 200, Center, Vector2.zero, new Vector2(500f, 260f), new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, true);

            // Coach pill for the first nights: sits low, above the thumb, never over the boat.
            Image coachPill = Image(c, "Coach", Bottom, new Vector2(0f, 360f), new Vector2(860f, 110f), new Color(0.04f, 0.06f, 0.16f, 0.82f));
            coachPill.raycastTarget = false;
            var coachGroup = coachPill.gameObject.AddComponent<CanvasGroup>();
            coachGroup.alpha = 0f;
            coachGroup.blocksRaycasts = false;
            LocalizedText coachText = Loc(coachPill.transform, "Text", "hud.coach_hold", 40, Center, Vector2.zero, new Vector2(820f, 100f), Gold, TextAnchor.MiddleCenter, true);
            // Seats: who is aboard right now (full = gold).
            Image seatsPill = Image(c, "Aboard", TopRight, new Vector2(-140f, -200f), new Vector2(220f, 92f), new Color(0.04f, 0.06f, 0.16f, 0.82f));
            seatsPill.raycastTarget = false;
            Image seatsIcon = Image(seatsPill.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(64f, 64f), Color.white, Art.MenuIcon("person"));
            seatsIcon.raycastTarget = false;
            Text seats = Text(seatsPill.transform, "Count", "0/3", 46, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(140f, 80f), TextLight, TextAnchor.MiddleCenter, true);
            Gen.Wire(screen, "rescue", w.Rescue, "coach", coachGroup, "coachText", coachText, "aboardLabel", seats);

            // JUMP: a big thumb button bottom-right; the rest of the screen dives into the waves.
            Button jump = Button(c, "Jump", "hud.jump", new Vector2(1f, 0f), new Vector2(-170f, 190f), new Vector2(260f, 200f), Gen.Hex("4CC631"), 54);
            var jumpFeel = jump.GetComponent<ButtonFeel>();
            if (jumpFeel != null) Object.DestroyImmediate(jumpFeel); // JumpButton does its own instant punch
            Gen.Wire(jump.gameObject.AddComponent<JumpButton>(), "rescue", w.Rescue, "face", (RectTransform)jump.transform);

            // DIVE (hold) bottom-left: the boat's controls are buttons, so thumbs know exactly what to press.
            Button dive = Button(c, "Dive", "hud.dive", new Vector2(0f, 0f), new Vector2(170f, 190f), new Vector2(260f, 200f), Gen.Hex("2F7FD6"), 54);
            if (dive.GetComponent<ButtonFeel>() is ButtonFeel diveFeel) Object.DestroyImmediate(diveFeel);
            Gen.Wire(dive.gameObject.AddComponent<HoldButton>(), "rescue", w.Rescue, "face", (RectTransform)dive.transform);

            // BOOST in the middle: fills from perfect landings and lanterns, then launches a full-moon speed burst.
            Button boost = Button(c, "Boost", "hud.boost", Bottom, new Vector2(0f, 170f), new Vector2(230f, 170f), Gen.Hex("6B4FA8"), 46);
            if (boost.GetComponent<ButtonFeel>() is ButtonFeel boostFeel) Object.DestroyImmediate(boostFeel);
            Image boostFill = Fill(Image(boost.transform, "Charge", Center, Vector2.zero, new Vector2(210f, 150f), new Color(1f, 0.8f, 0.25f, 0.55f)),
                UnityEngine.UI.Image.FillMethod.Vertical, 0f);
            boostFill.raycastTarget = false;
            boostFill.transform.SetSiblingIndex(1); // under the label
            var boostGroup = boost.gameObject.AddComponent<CanvasGroup>();
            Gen.Wire(boost.gameObject.AddComponent<HoldButton>(), "rescue", w.Rescue, "face", (RectTransform)boost.transform, "boost", true,
                "fill", boostFill, "group", boostGroup);

            Gen.Wire(screen, "session", w.Session, "fullMoon", w.FullMoon, "passengers", w.Passengers, "boss", w.Kraken,
                "gameManager", w.GameManager, "timeScale", w.TimeScale, "popups", popups,
                "progressBar", slider, "scoreLabel", score, "multiplierLabel", multiplier, "pauseButton", pause,
                "callout", callout, "calloutText", calloutText, "calloutMultiplier", calloutMultiplier,
                "moonstoneMeter", meter.gameObject, "moonstoneFill", meterFill, "moonstoneLabel", meterLabel,
                "fullMoonBanner", fullMoon.gameObject, "fullMoonTimer", fullMoonTimer,
                "passengerGauge", gauge.gameObject, "gaugeMarker", marker.rectTransform, "gaugeZone", zone, "gaugeHalfWidth", 200f,
                "weatherBanner", weatherGroup, "weatherText", weatherText, "weatherIcon", weatherIcon,
                "stormIcon", Art.WeatherIcon("storm"), "fogIcon", Art.WeatherIcon("fog"), "eclipseIcon", Art.WeatherIcon("eclipse"),
                "bossPanel", boss.gameObject, "bossFill", bossFill, "bossLabel", bossLabel, "rewindOverlay", rewind.gameObject);
            return screen;
        }

        private static FailScreen BuildFail(Transform parent)
        {
            var screen = Screen<FailScreen>(parent, "Fail");
            Gen.Set(screen, "inputLockSeconds", 0.7f);
            RectTransform c = ContentOf(screen);
            Backdrop(c, new Color(0.03f, 0.03f, 0.1f, 0.6f));
            Loc(c, "Title", LocKeys.FailTitle, 100, Center, new Vector2(0f, 560f), new Vector2(1000f, 140f), Danger, TextAnchor.MiddleCenter, true);
            LocalizedText reason = Loc(c, "Reason", LocKeys.FailRock, 52, Center, new Vector2(0f, 430f), new Vector2(1000f, 80f), TextLight);

            RectTransform rewindGroup = Rect("RewindGroup", c, Center, new Vector2(0f, 120f), new Vector2(900f, 360f));
            RewardedButton rewind = Rewarded(rewindGroup, "Rewind", LocKeys.FailRewind, AdPlacement.RewindTide, Center, new Vector2(0f, 60f), new Vector2(680f, 190f), w.Ads, true);
            Loc(rewindGroup, "Desc", LocKeys.FailRewindDesc, 36, Center, new Vector2(0f, -100f), new Vector2(880f, 70f), Muted);

            Button retry = Button(c, "Retry", LocKeys.CommonRetry, Center, new Vector2(0f, -330f), new Vector2(560f, 160f), Primary, 60);
            Button home = Button(c, "Home", LocKeys.CommonHome, Center, new Vector2(0f, -520f), new Vector2(380f, 120f), PanelLight, 44);
            Gen.Wire(screen, "gameManager", w.GameManager, "rewind", w.Rewind, "reasonLabel", reason, "rewindGroup", rewindGroup.gameObject,
                "rewindButton", rewind, "retryButton", retry, "homeButton", home);
            return screen;
        }

        private static WinScreen BuildWin(Transform parent)
        {
            var screen = Screen<WinScreen>(parent, "Win");
            Gen.Set(screen, "inputLockSeconds", 0.6f);
            RectTransform c = ContentOf(screen);
            Backdrop(c, new Color(0.03f, 0.03f, 0.1f, 0.55f));
            Loc(c, "Title", LocKeys.WinTitle, 92, Center, new Vector2(0f, 680f), new Vector2(1000f, 130f), Gold, TextAnchor.MiddleCenter, true);

            RectTransform starsRt = Rect("Stars", c, Center, new Vector2(0f, 500f), new Vector2(700f, 220f));
            var starImages = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                starImages.Add(Image(starsRt, "Star" + i, Center, new Vector2((i - 1) * 210f, i == 1 ? 30f : 0f), new Vector2(180f, 180f), Color.white, Art.Star(false)));
            }

            var starView = starsRt.gameObject.AddComponent<StarRatingView>();
            Gen.Wire(starView, "filled", Art.Star(true), "empty", Art.Star(false));
            Gen.SetArray(starView, "stars", starImages);

            LocalizedText newBest = Loc(c, "NewBest", LocKeys.HudNewBest, 40, Center, new Vector2(0f, 370f), new Vector2(600f, 60f), Accent, TextAnchor.MiddleCenter, true);
            Text score = Text(c, "Score", "0", 72, Center, new Vector2(0f, 290f), new Vector2(700f, 100f), TextLight, TextAnchor.MiddleCenter, true);
            score.gameObject.SetActive(false); // abstract points only cluttered the screen; people, lanterns and coins tell the story
            RectTransform rewardAnchor = Rect("Reward", c, Center, new Vector2(0f, 170f), new Vector2(500f, 110f));
            CoinIcon(rewardAnchor, new Vector2(0f, 0.5f), new Vector2(80f, 0f), 96f);
            Text reward = Text(rewardAnchor, "Amount", "+0", 72, Center, new Vector2(50f, 0f), new Vector2(360f, 100f), Gold, TextAnchor.MiddleLeft, true);
            LocalizedText chestNote = Loc(c, "BossChest", LocKeys.WinBossChest, 38, Center, new Vector2(0f, 70f), new Vector2(900f, 60f), Primary);
            LocalizedText suppliesNote = Loc(c, "Supplies", LocKeys.WinSupplies, 38, Center, new Vector2(0f, 70f), new Vector2(900f, 60f), Accent, TextAnchor.MiddleCenter, true);
            Text goalNote = Text(c, "Goal", "", 34, Center, new Vector2(0f, -770f), new Vector2(1000f, 60f), Gold, TextAnchor.MiddleCenter, true);

            RectTransform triple = Rect("Triple", c, Center, new Vector2(0f, -110f), new Vector2(900f, 300f));
            RewardedButton tripleButton = Rewarded(triple, "Triple", LocKeys.WinTriple, AdPlacement.TripleReward, Center, new Vector2(0f, 40f), new Vector2(660f, 180f), w.Ads, true);
            LocalizedText tripleDesc = Loc(triple, "Desc", LocKeys.WinTripleDesc, 36, Center, new Vector2(0f, -100f), new Vector2(880f, 60f), TextLight);

            Button next = Button(c, "Continue", LocKeys.CommonContinue, Center, new Vector2(0f, -470f), new Vector2(560f, 160f), Primary, 60);
            Button home = Button(c, "Home", LocKeys.CommonHome, Center, new Vector2(0f, -660f), new Vector2(360f, 110f), PanelLight, 42);

            Gen.Wire(screen, "gameManager", w.GameManager, "meta", w.Meta, "ads", w.Ads, "coinFly", coinFly, "stars", starView,
                "scoreLabel", score, "rewardLabel", reward, "rewardAnchor", rewardAnchor, "newBestBadge", newBest.gameObject,
                "bossChestNote", chestNote.gameObject, "suppliesNote", suppliesNote, "goalNote", goalNote, "tripleGroup", triple.gameObject, "tripleButton", tripleButton,
                "tripleDescription", tripleDesc, "continueButton", next, "homeButton", home);
            return screen;
        }

        private static ShopScreen BuildShop(Transform parent)
        {
            var screen = Screen<ShopScreen>(parent, "Shop");
            RectTransform c = ContentOf(screen);
            Backdrop(c, Gen.Hex("141A3A"));
            Loc(c, "Title", LocKeys.ShopTitle, 76, Top, new Vector2(0f, -200f), new Vector2(800f, 110f), TextLight, TextAnchor.MiddleCenter, true);
            Button back = Button(c, "Back", "<", TopLeft, new Vector2(90f, -200f), new Vector2(120f, 110f), PanelLight, 60, false);
            Button boatsTabButton = Button(c, "BoatsTab", LocKeys.BoatsTitle, Top, new Vector2(-200f, -330f), new Vector2(380f, 100f), PanelLight, 44);
            Button storeTabButton = Button(c, "StoreTab", LocKeys.ShopCoinPacks, Top, new Vector2(200f, -330f), new Vector2(380f, 100f), PanelLight, 44);

            // Boats tab: scrolling list of runtime-instantiated cards.
            RectTransform boatsTab = Stretch("BoatsTab", c);
            boatsTab.offsetMax = new Vector2(0f, -400f);
            boatsTab.offsetMin = new Vector2(0f, 40f);
            GameObject scroll = DefaultControls.CreateScrollView(Resources());
            var scrollRt = (RectTransform)scroll.transform;
            scrollRt.SetParent(boatsTab, false);
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(40f, 0f);
            scrollRt.offsetMax = new Vector2(-40f, 0f);
            scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            if (scrollRect.horizontalScrollbar != null)
            {
                Object.DestroyImmediate(scrollRect.horizontalScrollbar.gameObject);
            }

            RectTransform list = scrollRect.content;
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Store tab.
            RectTransform storeTab = Stretch("StoreTab", c);
            storeTab.offsetMax = new Vector2(0f, -400f);
            IapProductCard removeAds = ProductCard(storeTab, "RemoveAds", LocKeys.ShopRemoveAds, LocKeys.ShopRemoveAdsDesc, 480f, out _);
            IapProductCard starter = ProductCard(storeTab, "Starter", LocKeys.ShopStarter, LocKeys.ShopStarterDesc, 260f, out LocalizedText starterDesc);
            LocalizedText starterTimer = Loc(starter.transform, "Timer", LocKeys.ShopOfferEnds, 30, Bottom, new Vector2(-120f, 30f), new Vector2(560f, 44f), Primary);
            var packs = new List<IapProductCard>();
            int[] packCoins = { 1200, 3500, 9000 };
            for (int i = 0; i < 3; i++)
            {
                IapProductCard pack = ProductCard(storeTab, "Pack" + i, LocKeys.RewardCoins, LocKeys.ShopCoinPacks, 40f - i * 210f, out _, packCoins[i]);
                packs.Add(pack);
            }

            Button restore = Button(storeTab, "Restore", LocKeys.ShopRestore, Bottom, new Vector2(0f, 220f), new Vector2(520f, 100f), PanelLight, 38);

            BoatCard cardPrefab = BuildBoatCardPrefab();
            Gen.Wire(screen, "meta", w.Meta, "iapConfig", w.Content.Iap, "ads", w.Ads, "toast", toast, "boatsTab", boatsTab.gameObject,
                "storeTab", storeTab.gameObject, "boatsTabButton", boatsTabButton, "storeTabButton", storeTabButton, "backButton", back,
                "boatCardPrefab", cardPrefab, "boatList", list, "removeAdsCard", removeAds, "starterCard", starter,
                "starterDescription", starterDesc, "starterTimer", starterTimer, "restoreButton", restore);
            Gen.SetArray(screen, "coinPackCards", packs);
            return screen;
        }

        private static IapProductCard ProductCard(Transform parent, string name, string titleKey, string descKey, float y,
            out LocalizedText description, int titleArg = -1)
        {
            Image card = Image(parent, name, Center, new Vector2(0f, y), new Vector2(960f, 190f), Panel);
            LocalizedText title = Loc(card.transform, "Title", titleKey, 48, new Vector2(0f, 0.5f), new Vector2(330f, 40f), new Vector2(600f, 70f), TextLight, TextAnchor.MiddleLeft, true);
            if (titleArg >= 0)
            {
                Gen.Set(title, "key", titleKey);
            }

            description = Loc(card.transform, "Desc", descKey, 30, new Vector2(0f, 0.5f), new Vector2(330f, -30f), new Vector2(600f, 80f), Muted, TextAnchor.UpperLeft);
            Button buy = Button(card.transform, "Buy", "", new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(240f, 110f), Accent, 40, false);
            Text price = buy.GetComponentInChildren<Text>();
            var product = card.gameObject.AddComponent<IapProductCard>();
            Gen.Wire(product, "buyButton", buy, "priceLabel", price);
            return product;
        }

        private static BoatCard BuildBoatCardPrefab()
        {
            Image card = Image(null, "BoatCard", Center, Vector2.zero, new Vector2(960f, 240f), Panel);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 240f;
            Image icon = Image(card.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(190f, 190f), Color.white, Art.Circle);
            LocalizedText nameLabel = Loc(card.transform, "Name", "boat.dinghy.name", 46, new Vector2(0f, 0.5f), new Vector2(430f, 60f), new Vector2(420f, 64f), TextLight, TextAnchor.MiddleLeft, true);
            LocalizedText rarity = Loc(card.transform, "Rarity", LocKeys.RarityCommon, 30, new Vector2(0f, 0.5f), new Vector2(430f, 10f), new Vector2(420f, 44f), Accent, TextAnchor.MiddleLeft);
            Text perk = Text(card.transform, "Perk", "", 32, new Vector2(0f, 0.5f), new Vector2(430f, -50f), new Vector2(420f, 70f), TextLight, TextAnchor.UpperLeft);

            Button buy = Button(card.transform, "Buy", "", new Vector2(1f, 0.5f), new Vector2(-150f, 45f), new Vector2(240f, 90f), Primary, 38, false);
            Text price = buy.GetComponentInChildren<Text>();
            Button select = Button(card.transform, "Select", LocKeys.BoatsSelect, new Vector2(1f, 0.5f), new Vector2(-150f, 45f), new Vector2(240f, 90f), Accent, 36);
            LocalizedText selected = Loc(card.transform, "Selected", LocKeys.BoatsSelected, 36, new Vector2(1f, 0.5f), new Vector2(-150f, 45f), new Vector2(240f, 90f), Accent, TextAnchor.MiddleCenter, true);
            LocalizedText iapOnly = Loc(card.transform, "IapOnly", LocKeys.BoatsIapOnly, 26, new Vector2(1f, 0.5f), new Vector2(-150f, 45f), new Vector2(260f, 90f), Primary);
            RewardedButton tryButton = Rewarded(card.transform, "Try", LocKeys.BoatsTry, AdPlacement.TryBoat, new Vector2(1f, 0.5f), new Vector2(-150f, -55f), new Vector2(240f, 80f), null, false, 28);

            var boatCard = card.gameObject.AddComponent<BoatCard>();
            Gen.Wire(boatCard, "icon", icon, "nameLabel", nameLabel, "rarityLabel", rarity, "perkLabel", perk, "priceLabel", price,
                "buyButton", buy, "selectButton", select, "selectedMark", selected.gameObject, "iapOnlyLabel", iapOnly.gameObject,
                "tryButton", tryButton);
            return Gen.SavePrefab<BoatCard>(card.gameObject, "UI", "BoatCard");
        }

        // ---------------------------------------------------------------- popups

        private static IdleIncomePopup BuildIdle(Transform parent)
        {
            var popup = Popup<IdleIncomePopup>(parent, "IdleIncome", LocKeys.IdleTitle, new Vector2(920f, 1000f), out RectTransform p);
            RectTransform origin = CoinIcon(p, Center, new Vector2(0f, 250f), 150f);
            LocalizedText body = Loc(p, "Body", LocKeys.IdleBody, 42, Center, new Vector2(0f, 90f), new Vector2(820f, 140f), TextLight);
            LocalizedText rate = Loc(p, "Rate", LocKeys.IdleRate, 34, Center, new Vector2(0f, -20f), new Vector2(820f, 50f), Muted);
            Image bar = Image(p, "Bar", Center, new Vector2(0f, -80f), new Vector2(700f, 36f), new Color(0f, 0f, 0f, 0.35f));
            Image fill = Fill(Image(bar.transform, "Fill", Center, Vector2.zero, new Vector2(690f, 28f), Gold), UnityEngine.UI.Image.FillMethod.Horizontal, 0.5f);
            LocalizedText full = Loc(p, "Full", LocKeys.IdleFull, 34, Center, new Vector2(0f, -130f), new Vector2(700f, 50f), Primary);
            RewardedButton doubleIt = Rewarded(p, "Double", LocKeys.IdleDouble, AdPlacement.DoubleIdle, Center, new Vector2(0f, -250f), new Vector2(600f, 150f), w.Ads, true);
            Button collect = Button(p, "Collect", LocKeys.CommonCollect, Center, new Vector2(0f, -410f), new Vector2(460f, 120f), Primary, 48);
            Gen.Wire(popup, "meta", w.Meta, "coinFly", coinFly, "coinOrigin", origin, "bodyLabel", body, "rateLabel", rate,
                "fillBar", fill, "fullLabel", full.gameObject, "collectButton", collect, "doubleButton", doubleIt);
            return popup;
        }

        private static LoginStreakPopup BuildStreak(Transform parent)
        {
            var popup = Popup<LoginStreakPopup>(parent, "LoginStreak", LocKeys.StreakTitle, new Vector2(960f, 1100f), out RectTransform p);
            var so = new SerializedObject(popup);
            SerializedProperty days = so.FindProperty("days");
            days.arraySize = 7;
            for (int i = 0; i < 7; i++)
            {
                float x = i < 4 ? (i - 1.5f) * 215f : (i - 5f) * 215f;
                float y = i < 4 ? 200f : -60f;
                Image cell = Image(p, "Day" + (i + 1), Center, new Vector2(x, y), new Vector2(200f, 240f), i == 6 ? Gen.Hex("4A3F8C") : PanelLight);
                Image today = Image(cell.transform, "Today", Center, Vector2.zero, new Vector2(212f, 252f), new Color(1f, 0.85f, 0.35f, 0.35f));
                today.transform.SetAsFirstSibling();
                LocalizedText day = Loc(cell.transform, "Day", LocKeys.StreakDay, 32, Top, new Vector2(0f, -32f), new Vector2(190f, 50f), TextLight, TextAnchor.MiddleCenter, true);
                Image icon = Image(cell.transform, "Icon", Center, new Vector2(0f, 10f), new Vector2(90f, 90f), Color.white, i == 2 || i == 5 ? Art.Key() : Art.Coin());
                icon.raycastTarget = false;
                Text reward = Text(cell.transform, "Reward", "", 28, Bottom, new Vector2(0f, 40f), new Vector2(190f, 60f), Gold, TextAnchor.MiddleCenter, true);
                Image claimed = Image(cell.transform, "Claimed", Center, Vector2.zero, new Vector2(200f, 240f), new Color(0.1f, 0.12f, 0.25f, 0.7f));
                Text(claimed.transform, "Check", "OK", 50, Center, Vector2.zero, new Vector2(180f, 80f), Accent, TextAnchor.MiddleCenter, true);

                SerializedProperty element = days.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DayLabel").objectReferenceValue = day;
                element.FindPropertyRelative("RewardLabel").objectReferenceValue = reward;
                element.FindPropertyRelative("ClaimedMark").objectReferenceValue = claimed.gameObject;
                element.FindPropertyRelative("TodayHighlight").objectReferenceValue = today.gameObject;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Button claim = Button(p, "Claim", LocKeys.CommonClaim, Center, new Vector2(0f, -400f), new Vector2(500f, 140f), Primary, 54);
            Gen.Wire(popup, "meta", w.Meta, "coinFly", coinFly, "claimButton", claim);
            return popup;
        }

        private static DailySpinPopup BuildSpin(Transform parent)
        {
            var popup = Popup<DailySpinPopup>(parent, "DailySpin", LocKeys.SpinTitle, new Vector2(980f, 1400f), out RectTransform p);
            Image rim = Image(p, "Rim", Center, new Vector2(0f, 150f), new Vector2(760f, 760f), Gold, Art.Circle);
            RectTransform wheel = Rect("Wheel", rim.transform, Center, Vector2.zero, new Vector2(720f, 720f));
            var labels = new List<Text>();
            var segments = new List<Image>();
            for (int i = 0; i < 8; i++)
            {
                Image segment = Image(wheel, "Segment" + i, Center, Vector2.zero, new Vector2(720f, 720f), Color.white, Art.Circle);
                segment.type = UnityEngine.UI.Image.Type.Filled;
                segment.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                segment.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                segment.fillClockwise = true;
                segment.fillAmount = 1f / 8f;
                segment.rectTransform.localEulerAngles = new Vector3(0f, 0f, 22.5f - i * 45f);
                segments.Add(segment);

                float angle = i * 45f * Mathf.Deg2Rad;
                Text label = Text(wheel, "Label" + i, "", 34, Center, new Vector2(Mathf.Sin(angle) * 240f, Mathf.Cos(angle) * 240f),
                    new Vector2(220f, 70f), Ink, TextAnchor.MiddleCenter, true);
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, -i * 45f);
                labels.Add(label);
            }

            Image(wheel, "Hub", Center, Vector2.zero, new Vector2(120f, 120f), Ink, Art.Circle);
            Image pointer = Image(p, "Pointer", Center, new Vector2(0f, 545f), new Vector2(70f, 70f), Danger, Art.Square);
            pointer.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

            LocalizedText status = Loc(p, "Status", LocKeys.SpinTitle, 44, Center, new Vector2(0f, -290f), new Vector2(900f, 70f), Gold, TextAnchor.MiddleCenter, true);
            Button free = Button(p, "Free", LocKeys.SpinFree, Center, new Vector2(0f, -430f), new Vector2(560f, 150f), Primary, 52);
            RewardedButton extra = Rewarded(p, "Extra", LocKeys.SpinExtra, AdPlacement.ExtraSpin, Center, new Vector2(0f, -430f), new Vector2(560f, 150f), w.Ads, false);
            LocalizedText left = Loc(p, "Left", LocKeys.SpinLeft, 32, Center, new Vector2(0f, -550f), new Vector2(700f, 50f), Muted);

            Gen.Wire(popup, "meta", w.Meta, "coinFly", coinFly, "wheel", wheel, "freeSpinButton", free, "extraSpinButton", extra,
                "extraLeftLabel", left, "statusLabel", status);
            Gen.SetArray(popup, "segmentLabels", labels);
            Gen.SetArray(popup, "segmentImages", segments);
            return popup;
        }

        private static MissionsPopup BuildMissions(Transform parent)
        {
            var popup = Popup<MissionsPopup>(parent, "Missions", LocKeys.MissionsTitle, new Vector2(960f, 1150f), out RectTransform p);
            var rows = new List<MissionRow>();
            for (int i = 0; i < 3; i++)
            {
                Image row = Image(p, "Mission" + i, Center, new Vector2(0f, 240f - i * 270f), new Vector2(880f, 240f), PanelLight);
                LocalizedText desc = Loc(row.transform, "Desc", "mission.near_misses", 38, new Vector2(0f, 0.5f), new Vector2(300f, 55f), new Vector2(560f, 80f), TextLight, TextAnchor.MiddleLeft, true);
                Image bar = Image(row.transform, "Bar", new Vector2(0f, 0.5f), new Vector2(300f, -30f), new Vector2(560f, 30f), new Color(0f, 0f, 0f, 0.35f));
                Image fill = Fill(Image(bar.transform, "Fill", Center, Vector2.zero, new Vector2(552f, 22f), Accent), UnityEngine.UI.Image.FillMethod.Horizontal, 0f);
                Text progress = Text(row.transform, "Progress", "0/5", 30, new Vector2(0f, 0.5f), new Vector2(300f, -80f), new Vector2(560f, 44f), Muted, TextAnchor.MiddleLeft);
                CoinIcon(row.transform, new Vector2(1f, 0.5f), new Vector2(-230f, 60f), 50f);
                Text reward = Text(row.transform, "Reward", "100", 36, new Vector2(1f, 0.5f), new Vector2(-140f, 60f), new Vector2(140f, 50f), Gold, TextAnchor.MiddleLeft, true);
                Button claim = Button(row.transform, "Claim", LocKeys.CommonClaim, new Vector2(1f, 0.5f), new Vector2(-150f, -35f), new Vector2(240f, 100f), Primary, 38);
                LocalizedText claimed = Loc(row.transform, "Claimed", LocKeys.CommonClaimed, 34, new Vector2(1f, 0.5f), new Vector2(-150f, -35f), new Vector2(240f, 100f), Accent, TextAnchor.MiddleCenter, true);
                var missionRow = row.gameObject.AddComponent<MissionRow>();
                Gen.Wire(missionRow, "description", desc, "progressFill", fill, "progressLabel", progress, "rewardLabel", reward,
                    "claimButton", claim, "claimedMark", claimed.gameObject);
                rows.Add(missionRow);
            }

            LocalizedText refresh = Loc(p, "Refresh", LocKeys.MissionsRefresh, 32, Center, new Vector2(0f, -470f), new Vector2(880f, 50f), Muted);
            Gen.Wire(popup, "meta", w.Meta, "coinFly", coinFly, "refreshLabel", refresh);
            Gen.SetArray(popup, "rows", rows);
            return popup;
        }

        private static SettingsPopup BuildSettings(Transform parent)
        {
            var popup = Popup<SettingsPopup>(parent, "Settings", LocKeys.SettingsTitle, new Vector2(920f, 1250f), out RectTransform p);
            ToggleButton music = ToggleRow(p, LocKeys.SettingsMusic, 330f);
            ToggleButton sound = ToggleRow(p, LocKeys.SettingsSound, 200f);
            ToggleButton haptics = ToggleRow(p, LocKeys.SettingsHaptics, 70f);

            Loc(p, "LanguageLabel", LocKeys.SettingsLanguage, 42, Center, new Vector2(-230f, -60f), new Vector2(380f, 70f), TextLight, TextAnchor.MiddleLeft, true);
            GameObject dropdownGo = DefaultControls.CreateDropdown(Resources());
            dropdownGo.name = "Language";
            var dropdownRt = (RectTransform)dropdownGo.transform;
            dropdownRt.SetParent(p, false);
            dropdownRt.anchorMin = dropdownRt.anchorMax = Center;
            dropdownRt.anchoredPosition = new Vector2(190f, -60f);
            dropdownRt.sizeDelta = new Vector2(420f, 90f);
            foreach (Text t in dropdownGo.GetComponentsInChildren<Text>(true))
            {
                t.font = UiKit.Font;
                t.fontSize = 36;
            }

            var dropdown = dropdownGo.GetComponent<Dropdown>();
            if (dropdown.template != null)
            {
                dropdown.template.sizeDelta = new Vector2(0f, 520f);
            }

            // Match the dark glass UI instead of the default white Unity dropdown.
            var dropdownBg = dropdownGo.GetComponent<Image>();
            dropdownBg.sprite = Art.PanelSprite;
            dropdownBg.type = UnityEngine.UI.Image.Type.Sliced;
            dropdownBg.color = PanelLight;
            foreach (Text t in dropdownGo.GetComponentsInChildren<Text>(true))
            {
                t.color = TextLight;
            }

            Transform arrow = dropdownGo.transform.Find("Arrow");
            if (arrow != null)
            {
                arrow.GetComponent<Image>().color = Gold;
            }

            if (dropdown.template != null)
            {
                var templateBg = dropdown.template.GetComponent<Image>();
                templateBg.sprite = Art.PanelSprite;
                templateBg.type = UnityEngine.UI.Image.Type.Sliced;
                templateBg.color = Panel;
                Transform itemBg = dropdown.template.Find("Viewport/Content/Item/Item Background");
                if (itemBg != null)
                {
                    itemBg.GetComponent<Image>().color = PanelLight;
                }

                Transform check = dropdown.template.Find("Viewport/Content/Item/Item Checkmark");
                if (check != null)
                {
                    check.GetComponent<Image>().color = Gold;
                }
            }

            Button privacy = Button(p, "PrivacyOptions", LocKeys.SettingsPrivacy, Center, new Vector2(0f, -230f), new Vector2(700f, 110f), PanelLight, 40);
            Button policy = Button(p, "PrivacyPolicy", LocKeys.SettingsPrivacyPolicy, Center, new Vector2(0f, -370f), new Vector2(700f, 110f), PanelLight, 40);
            LocalizedText version = Loc(p, "Version", LocKeys.SettingsVersion, 30, Center, new Vector2(0f, -500f), new Vector2(700f, 50f), Muted);
            Gen.Wire(popup, "musicToggle", music, "soundToggle", sound, "hapticsToggle", haptics, "languageDropdown", dropdown,
                "privacyOptionsButton", privacy, "privacyPolicyButton", policy, "versionLabel", version,
                "privacyPolicyUrl", "https://javidalishov700-blip.github.io/moonpull/privacy.html");
            return popup;
        }

        private static ToggleButton ToggleRow(Transform panel, string key, float y)
        {
            Loc(panel, "Label_" + key, key, 44, Center, new Vector2(-230f, y), new Vector2(380f, 80f), TextLight, TextAnchor.MiddleLeft, true);
            Image bg = Image(panel, "Toggle_" + key, Center, new Vector2(250f, y), new Vector2(260f, 100f), Muted);
            var button = bg.gameObject.AddComponent<Button>();
            Image on = Image(bg.transform, "On", Center, Vector2.zero, new Vector2(260f, 100f), Accent);
            Image off = Image(bg.transform, "Off", Center, Vector2.zero, new Vector2(260f, 100f), Muted);
            on.raycastTarget = false;
            off.raycastTarget = false;
            LocalizedText state = Loc(bg.transform, "State", LocKeys.CommonOn, 40, Center, Vector2.zero, new Vector2(240f, 90f), Ink, TextAnchor.MiddleCenter, true);
            _ = button;
            var toggle = bg.gameObject.AddComponent<ToggleButton>();
            Gen.Wire(toggle, "onVisual", on.gameObject, "offVisual", off.gameObject, "stateLabel", state);
            return toggle;
        }

        private static LighthousePopup BuildLighthouse(Transform parent, UIScreen idle)
        {
            var popup = Popup<LighthousePopup>(parent, "Lighthouse", LocKeys.LighthouseTitle, new Vector2(960f, 1150f), out RectTransform p);
            LocalizedText region = Loc(p, "Region", "region.tropical_lagoon", 46, Center, new Vector2(0f, 340f), new Vector2(620f, 80f), Accent, TextAnchor.MiddleCenter, true);
            Button prev = Button(p, "Prev", "<", Center, new Vector2(-380f, 340f), new Vector2(100f, 100f), PanelLight, 54, false);
            Button next = Button(p, "Next", ">", Center, new Vector2(380f, 340f), new Vector2(100f, 100f), PanelLight, 54, false);
            Image tower = Image(p, "Tower", Center, new Vector2(0f, 150f), new Vector2(120f, 260f), Gen.Hex("F4F1EA"));
            Image(tower.transform, "Light", Top, new Vector2(0f, 10f), new Vector2(90f, 90f), Gen.Hex("FFF1B8"), Art.Circle);
            LocalizedText stage = Loc(p, "Stage", LocKeys.LighthouseStage, 40, Center, new Vector2(0f, -30f), new Vector2(700f, 60f), TextLight);
            var pips = new List<Image>();
            for (int i = 0; i < 8; i++)
            {
                pips.Add(Image(p, "Pip" + i, Center, new Vector2((i - 3.5f) * 95f, -110f), new Vector2(80f, 40f), Color.white));
            }

            LocalizedText rate = Loc(p, "Rate", LocKeys.IdleRate, 36, Center, new Vector2(0f, -180f), new Vector2(700f, 60f), Gold);
            LocalizedText locked = Loc(p, "Locked", LocKeys.LighthouseRegionLocked, 38, Center, new Vector2(0f, -290f), new Vector2(800f, 60f), Primary);
            LocalizedText complete = Loc(p, "Complete", LocKeys.LighthouseComplete, 46, Center, new Vector2(0f, -290f), new Vector2(800f, 70f), Accent, TextAnchor.MiddleCenter, true);
            Button build = Button(p, "Build", LocKeys.LighthouseBuild, Center, new Vector2(-150f, -300f), new Vector2(380f, 130f), Primary, 48);
            Image costBg = Image(p, "Cost", Center, new Vector2(200f, -300f), new Vector2(260f, 100f), new Color(0f, 0f, 0f, 0.3f));
            CoinIcon(costBg.transform, new Vector2(0f, 0.5f), new Vector2(45f, 0f), 60f);
            Text cost = Text(costBg.transform, "Amount", "0", 40, Center, new Vector2(30f, 0f), new Vector2(170f, 70f), Gold, TextAnchor.MiddleCenter, true);
            Button collect = Button(p, "Collect", LocKeys.CommonCollect, Center, new Vector2(0f, -450f), new Vector2(420f, 110f), Accent, 42);
            Gen.Wire(popup, "meta", w.Meta, "regionLabel", region, "stageLabel", stage, "rateLabel", rate, "lockedLabel", locked,
                "completeLabel", complete.gameObject, "buildButton", build, "costLabel", cost, "previousButton", prev, "nextButton", next,
                "collectButton", collect, "idlePopup", idle);
            Gen.SetArray(popup, "stagePips", pips);
            return popup;
        }

        private static VillagePopup BuildVillage(Transform parent)
        {
            // A bottom sheet: the live village stays visible (and tappable) above it.
            var popup = Popup<VillagePopup>(parent, "Village", LocKeys.VillageTitle, new Vector2(1040f, 1080f), out RectTransform p);
            // A bottom sheet: pinned to the bottom edge on every aspect ratio, the village fills the space above.
            p.anchorMin = p.anchorMax = new Vector2(0.5f, 0f);
            p.pivot = new Vector2(0.5f, 0f);
            p.anchoredPosition = new Vector2(0f, 20f);
            Gen.Set(popup, "sheetOpenY", 20f);
            Gen.Set(popup, "sheetHiddenY", -1500f); // fully off screen: the village gets the whole view
            Image backdrop = popup.transform.Find("Backdrop").GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0f);
            backdrop.raycastTarget = false;

            LocalizedText level = Loc(p, "Level", LocKeys.VillageLevel, 40, Top, new Vector2(-250f, -170f), new Vector2(460f, 56f), Gold, TextAnchor.MiddleLeft, true);
            Image xpBg = Image(p, "XpBar", Top, new Vector2(-250f, -220f), new Vector2(460f, 26f), new Color(0f, 0f, 0f, 0.35f));
            Image xp = Fill(Image(xpBg.transform, "Fill", Center, Vector2.zero, new Vector2(460f, 26f), Gold), UnityEngine.UI.Image.FillMethod.Horizontal, 0.3f);
            LocalizedText population = Loc(p, "Population", LocKeys.VillagePopulation, 30, Top, new Vector2(-250f, -262f), new Vector2(460f, 44f), TextLight, TextAnchor.MiddleLeft);
            LocalizedText happiness = Loc(p, "Happiness", LocKeys.VillageHappiness, 32, Top, new Vector2(250f, -170f), new Vector2(460f, 50f), TextLight, TextAnchor.MiddleLeft, true);
            Image hapBg = Image(p, "HappyBar", Top, new Vector2(250f, -215f), new Vector2(460f, 22f), new Color(0f, 0f, 0f, 0.35f));
            Image hap = Fill(Image(hapBg.transform, "Fill", Center, Vector2.zero, new Vector2(460f, 22f), Accent), UnityEngine.UI.Image.FillMethod.Horizontal, 0.7f);
            LocalizedText food = Loc(p, "Food", LocKeys.VillageFood, 30, Top, new Vector2(140f, -262f), new Vector2(240f, 44f), TextLight, TextAnchor.MiddleLeft);
            LocalizedText housing = Loc(p, "Housing", LocKeys.VillageHousing, 30, Top, new Vector2(370f, -262f), new Vector2(240f, 44f), TextLight, TextAnchor.MiddleLeft);

            // Treasury card floating over the village: income piles up here, tap Collect to bank it.
            Image card = Image(popup.transform, "Treasury", Top, new Vector2(0f, -250f), new Vector2(960f, 170f), new Color(0.08f, 0.1f, 0.25f, 0.88f));
            CoinIcon(card.transform, new Vector2(0f, 0.5f), new Vector2(80f, 12f), 96f);
            Text treasury = Text(card.transform, "Amount", "0", 64, new Vector2(0f, 0.5f), new Vector2(330f, 24f), new Vector2(300f, 80f), Gold, TextAnchor.MiddleLeft, true);
            LocalizedText income = Loc(card.transform, "Income", LocKeys.VillageIncome, 30, new Vector2(0f, 0.5f), new Vector2(330f, -32f), new Vector2(300f, 44f), Accent, TextAnchor.MiddleLeft, true);
            LocalizedText supplies = Loc(popup.transform, "Supplies", LocKeys.VillageSupplies, 30, Top, new Vector2(0f, -360f), new Vector2(960f, 46f), TextLight, TextAnchor.MiddleCenter, true);
            supplies.GetComponent<Text>().gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0.1f, 0.8f);
            Image storeBg = Image(card.transform, "Storage", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(900f, 14f), new Color(0f, 0f, 0f, 0.4f));
            Image storeFill = Fill(Image(storeBg.transform, "Fill", Center, Vector2.zero, new Vector2(900f, 14f), Gold), UnityEngine.UI.Image.FillMethod.Horizontal, 0.4f);
            Button collect = Button(card.transform, "Collect", LocKeys.VillageCollect, new Vector2(1f, 0.5f), new Vector2(-170f, 10f), new Vector2(290f, 110f), Go, 46);

            Button explore = Button(popup.transform, "Explore", LocKeys.VillageExplore, TopRight, new Vector2(-180f, -440f), new Vector2(320f, 120f), Gen.Hex("4CC631"), 40);
            // Back to the menu from the full village view (the sheet's X is off screen while exploring).
            Button back = Button(popup.transform, "Back", "<", TopLeft, new Vector2(100f, -440f), new Vector2(130f, 120f), Danger, 60, false);
            back.gameObject.AddComponent<PopupBackButton>();

            // < > island stepping: the camera glides along the chain of islands, neighbours peeking in at the edges.
            for (int dir = -1; dir <= 1; dir += 2)
            {
                Button arrow = Button(popup.transform, dir < 0 ? "IslandLeft" : "IslandRight", dir < 0 ? "<" : ">", dir < 0 ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                    new Vector2(dir * -80f, 120f), new Vector2(120f, 150f), Gen.Hex("2F7FD6"), 70, false);
                if (arrow.GetComponent<ButtonFeel>() is ButtonFeel arrowFeel) Object.DestroyImmediate(arrowFeel);
                Gen.Set(arrow.gameObject.AddComponent<IslandArrow>(), "direction", dir);
            }

            Button tabBuildings = Button(p, "TabBuildings", LocKeys.VillageTabBuildings, Top, new Vector2(-325f, -335f), new Vector2(315f, 80f), Primary, 34);
            Button tabIslands = Button(p, "TabIslands", LocKeys.VillageTabIslands, Top, new Vector2(0f, -335f), new Vector2(315f, 80f), Accent, 34);
            Button tabBoat = Button(p, "TabBoat", LocKeys.VillageTabBoat, Top, new Vector2(325f, -335f), new Vector2(315f, 80f), Accent, 34);

            RectTransform buildingsPage = Stretch("BuildingsPage", p);
            RectTransform islandsPage = Stretch("IslandsPage", p);
            RectTransform boatPage = Stretch("BoatPage", p);
            string[] parts = { "sail", "hull", "lamp" };
            Color[] partTints = { Gen.Hex("7FA7D9"), Gen.Hex("C98A5B"), Gen.Hex("F2CC8F") };
            var boatLevels = new List<Text>();
            var boatButtons = new List<Button>();
            var boatCosts = new List<Text>();
            var boatMaxes = new List<GameObject>();
            for (int i = 0; i < parts.Length; i++)
            {
                float y = 80f - i * 150f;
                Image row = Image(boatPage, "Part_" + parts[i], Center, new Vector2(0f, y), new Vector2(980f, 136f), PanelLight);
                Image(row.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(110f, 110f), Color.white, Art.BuildingIcon(parts[i], partTints[i]));
                Loc(row.transform, "Name", "boat_up." + parts[i] + ".name", 36, new Vector2(0f, 0.5f), new Vector2(330f, 24f), new Vector2(420f, 50f), TextLight, TextAnchor.MiddleLeft, true);
                Loc(row.transform, "Desc", "boat_up." + parts[i] + ".desc", 26, new Vector2(0f, 0.5f), new Vector2(330f, -24f), new Vector2(420f, 50f), Muted, TextAnchor.MiddleLeft);
                boatLevels.Add(Text(row.transform, "Level", "0/5", 32, new Vector2(1f, 0.5f), new Vector2(-330f, 0f), new Vector2(100f, 60f), Gold, TextAnchor.MiddleCenter, true));
                Button up = Button(row.transform, "Upgrade", LocKeys.VillageUpgrade, new Vector2(1f, 0.5f), new Vector2(-140f, 18f), new Vector2(230f, 66f), Go, 32);
                Image costBg = Image(row.transform, "Cost", new Vector2(1f, 0.5f), new Vector2(-140f, -36f), new Vector2(230f, 44f), new Color(0f, 0f, 0f, 0.3f));
                CoinIcon(costBg.transform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), 36f);
                boatCosts.Add(Text(costBg.transform, "Amount", "0", 28, Center, new Vector2(20f, 0f), new Vector2(160f, 44f), Gold, TextAnchor.MiddleCenter, true));
                boatButtons.Add(up);
                boatMaxes.Add(Loc(row.transform, "Max", LocKeys.VillageMax, 32, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(230f, 60f), Accent, TextAnchor.MiddleCenter, true).gameObject);
            }

            string[] ids = { "shelter", "restaurant", "workshop", "shipyard", "market" };
            Color[] tints = { Gen.Hex("81B29A"), Gen.Hex("E07A5F"), Gen.Hex("F2CC8F"), Gen.Hex("7FA7D9"), Gen.Hex("C39BD3") };
            var levels = new List<Text>();
            var buttons = new List<Button>();
            var costs = new List<Text>();
            var maxes = new List<GameObject>();
            var capped = new List<LocalizedText>();
            for (int i = 0; i < ids.Length; i++)
            {
                float y = 80f - i * 138f;
                Image row = Image(buildingsPage, "Row_" + ids[i], Center, new Vector2(0f, y), new Vector2(980f, 128f), PanelLight);
                Image(row.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(104f, 104f), Color.white, Art.BuildingIcon(ids[i], tints[i]));
                Loc(row.transform, "Name", "village." + ids[i] + ".name", 36, new Vector2(0f, 0.5f), new Vector2(320f, 24f), new Vector2(400f, 50f), TextLight, TextAnchor.MiddleLeft, true);
                Loc(row.transform, "Desc", "village." + ids[i] + ".desc", 26, new Vector2(0f, 0.5f), new Vector2(320f, -24f), new Vector2(400f, 50f), Muted, TextAnchor.MiddleLeft);
                levels.Add(Text(row.transform, "Level", "0/3", 32, new Vector2(1f, 0.5f), new Vector2(-330f, 0f), new Vector2(100f, 60f), Gold, TextAnchor.MiddleCenter, true));
                Button build = Button(row.transform, "Build", LocKeys.VillageUpgrade, new Vector2(1f, 0.5f), new Vector2(-140f, 18f), new Vector2(230f, 66f), Primary, 32);
                Image costBg = Image(row.transform, "Cost", new Vector2(1f, 0.5f), new Vector2(-140f, -36f), new Vector2(230f, 44f), new Color(0f, 0f, 0f, 0.3f));
                CoinIcon(costBg.transform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), 36f);
                costs.Add(Text(costBg.transform, "Amount", "0", 28, Center, new Vector2(20f, 0f), new Vector2(160f, 44f), Gold, TextAnchor.MiddleCenter, true));
                buttons.Add(build);
                maxes.Add(Loc(row.transform, "Max", LocKeys.VillageMax, 32, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(230f, 60f), Accent, TextAnchor.MiddleCenter, true).gameObject);
                capped.Add(Loc(row.transform, "Capped", LocKeys.VillageNeedsLevel, 26, new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(240f, 80f), Muted, TextAnchor.MiddleCenter, true));
            }

            Color[] islandTints = { Gen.Hex("6FBF73"), Gen.Hex("F2B880"), Gen.Hex("7FC8A9"), Gen.Hex("9FA8DA") };
            var islandButtons = new List<Button>();
            var islandCosts = new List<Text>();
            var islandOwned = new List<GameObject>();
            var islandLocked = new List<LocalizedText>();
            for (int i = 0; i < MoonPull.Rescue.TycoonState.IslandCount; i++)
            {
                string id = MoonPull.Rescue.TycoonState.IslandIds[i];
                float y = 80f - i * 150f;
                Image row = Image(islandsPage, "Island_" + id, Center, new Vector2(0f, y), new Vector2(980f, 136f), PanelLight);
                Image(row.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(110f, 110f), Color.white, Art.BuildingIcon("island", islandTints[i]));
                Loc(row.transform, "Name", "island." + id + ".name", 36, new Vector2(0f, 0.5f), new Vector2(330f, 24f), new Vector2(420f, 50f), TextLight, TextAnchor.MiddleLeft, true);
                Loc(row.transform, "Desc", "island." + id + ".desc", 26, new Vector2(0f, 0.5f), new Vector2(330f, -24f), new Vector2(420f, 50f), Muted, TextAnchor.MiddleLeft);
                Button buy = Button(row.transform, "Buy", LocKeys.IslandBuy, new Vector2(1f, 0.5f), new Vector2(-150f, 20f), new Vector2(250f, 70f), Go, 34);
                Image costBg = Image(row.transform, "Cost", new Vector2(1f, 0.5f), new Vector2(-150f, -38f), new Vector2(250f, 44f), new Color(0f, 0f, 0f, 0.3f));
                CoinIcon(costBg.transform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), 36f);
                islandCosts.Add(Text(costBg.transform, "Amount", "0", 28, Center, new Vector2(20f, 0f), new Vector2(180f, 44f), Gold, TextAnchor.MiddleCenter, true));
                islandButtons.Add(buy);
                islandOwned.Add(Loc(row.transform, "Owned", LocKeys.IslandOwned, 36, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(250f, 60f), Accent, TextAnchor.MiddleCenter, true).gameObject);
                islandLocked.Add(Loc(row.transform, "Locked", LocKeys.VillageNeedsLevel, 24, new Vector2(1f, 0.5f), new Vector2(-150f, 22f), new Vector2(260f, 60f), Muted, TextAnchor.MiddleCenter, true));
            }

            Gen.Wire(popup, "sheet", p, "exploreButton", explore, "boatTab", tabBoat, "boatPage", boatPage.gameObject);
            Gen.SetArray(popup, "boatLevelLabels", boatLevels);
            Gen.SetArray(popup, "boatButtons", boatButtons);
            Gen.SetArray(popup, "boatCostLabels", boatCosts);
            Gen.SetArray(popup, "boatMaxLabels", boatMaxes);
            Gen.Wire(popup, "coinFly", coinFly, "buildingsTab", tabBuildings, "islandsTab", tabIslands,
                "buildingsPage", buildingsPage.gameObject, "islandsPage", islandsPage.gameObject, "treasuryLabel", treasury,
                "incomeLabel", income, "suppliesLabel", supplies, "collectButton", collect, "treasuryFill", storeFill);
            Gen.SetArray(popup, "islandButtons", islandButtons);
            Gen.SetArray(popup, "islandCostLabels", islandCosts);
            Gen.SetArray(popup, "islandOwnedLabels", islandOwned);
            Gen.SetArray(popup, "islandLockedLabels", islandLocked);
            Gen.Wire(popup, "meta", w.Meta, "director", w.Village, "populationLabel", population, "levelLabel", level, "xpFill", xp,
                "happinessLabel", happiness, "happinessFill", hap, "foodLabel", food, "housingLabel", housing);
            Gen.SetArray(popup, "levelLabels", levels);
            Gen.SetArray(popup, "upgradeButtons", buttons);
            Gen.SetArray(popup, "costLabels", costs);
            Gen.SetArray(popup, "maxLabels", maxes);
            Gen.SetArray(popup, "cappedLabels", capped);
            if (w.Village != null)
            {
                Gen.Set(w.Village, "toast", toast);
            }

            if (w.Pins != null)
            {
                Gen.Set(w.Pins, "popup", popup);
            }

            return popup;
        }

        private static BossChestPopup BuildBossChest(Transform parent)
        {
            var popup = Popup<BossChestPopup>(parent, "BossChest", LocKeys.ChestTitle, new Vector2(920f, 1050f), out RectTransform p);
            Image chest = Image(p, "Chest", Center, new Vector2(0f, 220f), new Vector2(300f, 220f), Gen.Hex("8A5A2B"));
            Image(chest.transform, "Lid", Top, new Vector2(0f, -30f), new Vector2(320f, 70f), Gen.Hex("C8932E"));
            Image(chest.transform, "Lock", Center, new Vector2(0f, 0f), new Vector2(60f, 60f), Gold, Art.Circle);
            Text pending = Text(p, "Pending", "x1", 50, Center, new Vector2(200f, 320f), new Vector2(200f, 70f), TextLight, TextAnchor.MiddleCenter, true);
            LocalizedText keys = Loc(p, "Keys", LocKeys.ChestKeys, 40, Center, new Vector2(0f, 40f), new Vector2(600f, 60f), Gold);
            LocalizedText result = Loc(p, "Result", LocKeys.ChestGot, 46, Center, new Vector2(0f, -40f), new Vector2(800f, 70f), Accent, TextAnchor.MiddleCenter, true);
            Button withKey = Button(p, "OpenKey", LocKeys.ChestOpenKey, Center, new Vector2(0f, -200f), new Vector2(560f, 140f), Primary, 48);
            RewardedButton withAd = Rewarded(p, "OpenAd", LocKeys.ChestOpenAd, AdPlacement.OpenBossChest, Center, new Vector2(0f, -370f), new Vector2(560f, 140f), w.Ads, true);
            Gen.Wire(popup, "meta", w.Meta, "coinFly", coinFly, "chest", chest.rectTransform, "pendingLabel", pending, "keysLabel", keys,
                "resultLabel", result, "openWithKeyButton", withKey, "openWithAdButton", withAd);
            return popup;
        }

        private static PausePopup BuildPause(Transform parent)
        {
            var popup = Popup<PausePopup>(parent, "Pause", LocKeys.PauseTitle, new Vector2(800f, 880f), out RectTransform p);
            Button resume = Button(p, "Resume", LocKeys.PauseResume, Center, new Vector2(0f, 170f), new Vector2(560f, 150f), Primary, 54);
            Button finish = Button(p, "Finish", LocKeys.PauseFinish, Center, new Vector2(0f, 20f), new Vector2(560f, 124f), Gen.Hex("2F8C99"), 44);
            Button restart = Button(p, "Restart", LocKeys.PauseRestart, Center, new Vector2(0f, -125f), new Vector2(560f, 124f), PanelLight, 44);
            Button home = Button(p, "Home", LocKeys.CommonHome, Center, new Vector2(0f, -270f), new Vector2(560f, 124f), PanelLight, 44);
            Gen.Wire(popup, "timeScale", w.TimeScale, "resumeButton", resume, "restartButton", restart, "homeButton", home,
                "finishButton", finish, "rescue", w.Rescue);
            return popup;
        }

        private static DefaultControls.Resources Resources() => new DefaultControls.Resources
        {
            standard = Art.Rounded,
            background = Art.Square,
            inputField = Art.BuiltinUi("InputFieldBackground.psd"),
            knob = Art.Circle,
            checkmark = Art.BuiltinUi("Checkmark.psd"),
            dropdown = Art.BuiltinUi("DropdownArrow.psd"),
            mask = Art.BuiltinUi("UIMask.psd")
        };
    }
}
