using MoonPull.Core.TimeControl;
using MoonPull.Core;
using MoonPull.Layers.Boss;
using MoonPull.Layers.Passengers;
using MoonPull.Level;
using MoonPull.Localization;
using MoonPull.Mechanics;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>In-level HUD. Labels only change on events; the per-frame work (progress bar, gauge) sets no text.</summary>
    public sealed class HudScreen : UIScreen
    {
        [Header("Systems")]
        [SerializeField] private LevelSession session;
        [SerializeField] private FullMoonMode fullMoon;
        [SerializeField] private PassengerSystem passengers;
        [SerializeField] private KrakenBoss boss;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TimeScaleController timeScale;
        [SerializeField] private PopupManager popups;
        [SerializeField] private UIScreen pausePopup;

        [Header("Progress & score")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text multiplierLabel;
        [SerializeField] private Button pauseButton;

        [Header("Callout (near miss, shield, passengers)")]
        [SerializeField] private CanvasGroup callout;
        [SerializeField] private LocalizedText calloutText;
        [SerializeField] private Text calloutMultiplier;
        [SerializeField, Min(0.1f)] private float calloutSeconds = 0.9f;

        [Header("Moonstones / Full Moon")]
        [SerializeField] private GameObject moonstoneMeter;
        [SerializeField] private Image moonstoneFill;
        [SerializeField] private Text moonstoneLabel;
        [SerializeField] private GameObject fullMoonBanner;
        [SerializeField] private Image fullMoonTimer;

        [Header("Passenger gauge")]
        [SerializeField] private GameObject passengerGauge;
        [SerializeField] private RectTransform gaugeMarker;
        [SerializeField] private Image gaugeZone;
        [SerializeField, Min(1f)] private float gaugeHalfWidth = 180f;
        [SerializeField] private Color alignedColor = new Color(0.4f, 1f, 0.55f);
        [SerializeField] private Color misalignedColor = new Color(1f, 0.55f, 0.45f);

        [Header("Weather")]
        [SerializeField] private CanvasGroup weatherBanner;
        [SerializeField] private LocalizedText weatherText;
        [SerializeField] private Image weatherIcon;
        [SerializeField] private Sprite stormIcon;
        [SerializeField] private Sprite fogIcon;
        [SerializeField] private Sprite eclipseIcon;

        [Header("Boss")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private Image bossFill;
        [SerializeField] private Text bossLabel;

        [Header("Rewind")]
        [SerializeField] private GameObject rewindOverlay;

        private float calloutHideAt;
        private float fullMoonDuration = 1f;
        private float eclipseAt;
        private int lastEclipseSeconds = -1;

        private void Awake()
        {
            pauseButton.onClick.AddListener(OpenPause);
        }

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.PerfectCrest += OnPerfectCrest;
            GameEvents.TideTurned += OnTideTurned;
            GameEvents.BoatBumped += OnBoatBumped;
            GameEvents.ShieldConsumed += OnShield;
            GameEvents.PassengerBoarded += OnPassengerBoarded;
            GameEvents.MoonstoneCollected += OnMoonstone;
            GameEvents.FullMoonStarted += OnFullMoonStarted;
            GameEvents.FullMoonEnded += OnFullMoonEnded;
            GameEvents.WeatherWarning += OnWeatherWarning;
            GameEvents.WeatherStarted += OnWeatherStarted;
            GameEvents.WeatherEnded += OnWeatherEnded;
            GameEvents.BossHit += OnBossHit;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.RewindStarted += OnRewindStarted;
            GameEvents.RewindFinished += OnRewindFinished;
            GameEvents.AppPaused += OnAppPaused;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.PerfectCrest -= OnPerfectCrest;
            GameEvents.TideTurned -= OnTideTurned;
            GameEvents.BoatBumped -= OnBoatBumped;
            GameEvents.ShieldConsumed -= OnShield;
            GameEvents.PassengerBoarded -= OnPassengerBoarded;
            GameEvents.MoonstoneCollected -= OnMoonstone;
            GameEvents.FullMoonStarted -= OnFullMoonStarted;
            GameEvents.FullMoonEnded -= OnFullMoonEnded;
            GameEvents.WeatherWarning -= OnWeatherWarning;
            GameEvents.WeatherStarted -= OnWeatherStarted;
            GameEvents.WeatherEnded -= OnWeatherEnded;
            GameEvents.BossHit -= OnBossHit;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.RewindStarted -= OnRewindStarted;
            GameEvents.RewindFinished -= OnRewindFinished;
            GameEvents.AppPaused -= OnAppPaused;
        }

        protected override void OnShown() => ResetWidgets();

        protected override void OnUpdate()
        {
            progressBar.value = session.Progress;

            if (callout.alpha > 0f && Time.unscaledTime >= calloutHideAt)
            {
                callout.alpha = Mathf.MoveTowards(callout.alpha, 0f, Time.unscaledDeltaTime * 4f);
            }

            if (fullMoon.IsActive)
            {
                fullMoonTimer.fillAmount = fullMoon.Remaining / fullMoonDuration;
            }

            UpdatePassengerGauge();
            UpdateEclipseCountdown();
        }

        private void ResetWidgets()
        {
            LevelPlan plan = session.Plan;
            callout.alpha = 0f;
            multiplierLabel.gameObject.SetActive(false);
            fullMoonBanner.SetActive(false);
            passengerGauge.SetActive(false);
            weatherBanner.alpha = 0f;
            rewindOverlay.SetActive(false);
            scoreLabel.SetText("{0}", 0);
            bossPanel.SetActive(false); // Moonlight Sail has no Kraken fight
            bossFill.fillAmount = 0f;
            bossLabel.SetText("0/{0}", plan.KrakenHitsRequired);
            moonstoneMeter.SetActive(plan.Count(PlacementKind.Moonstone) > 0);
            SetMoonstones(fullMoon.Moonstones);
            eclipseAt = 0f;
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            if (IsVisible)
            {
                ResetWidgets();
            }

            if (args.LevelIndex < 3)
            {
                calloutMultiplier.gameObject.SetActive(false);
                ShowCallout(LocKeys.HudSteerHint);
                calloutHideAt = Time.unscaledTime + 3f;
            }
        }

        private void OnScoreChanged(int score, int multiplier)
        {
            scoreLabel.SetText("{0}", score);
            bool show = multiplier > 1;
            multiplierLabel.gameObject.SetActive(show);
            if (show)
            {
                multiplierLabel.SetText("x{0}", multiplier);
            }
        }

        private void ShowCallout(string key, params object[] args)
        {
            calloutText.SetKey(key, args);
            callout.alpha = 1f;
            calloutHideAt = Time.unscaledTime + calloutSeconds;
            UiTween.PopIn(callout.transform, 0.25f);
        }

        private void OnNearMiss(int chain, int multiplier)
        {
            ShowCallout(LocKeys.HudNearMiss);
            calloutMultiplier.gameObject.SetActive(multiplier > 1);
            calloutMultiplier.SetText("x{0}", multiplier);
        }

        private void OnPerfectCrest(int streak)
        {
            ShowCallout(LocKeys.HudPerfectCrest);
            calloutMultiplier.gameObject.SetActive(streak > 1);
            calloutMultiplier.SetText("x{0}", Mathf.Min(streak, 5));
        }

        private void OnTideTurned(int freeLeft)
        {
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudTideTurned, freeLeft);
        }

        private void OnBoatBumped(int heartsLeft)
        {
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudBumped, heartsLeft);
        }

        private void OnShield()
        {
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudShield);
        }

        private void OnPassengerBoarded(int onboard)
        {
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudPassengersBoarded, onboard);
        }

        private void OnMoonstone(int count)
        {
            SetMoonstones(count);
            UiTween.Punch(moonstoneMeter.transform);
        }

        private void SetMoonstones(int count)
        {
            moonstoneFill.fillAmount = count / (float)fullMoon.Required;
            moonstoneLabel.SetText("{0}/{1}", count, fullMoon.Required);
        }

        private void OnFullMoonStarted(float duration)
        {
            fullMoonDuration = Mathf.Max(0.01f, duration);
            fullMoonBanner.SetActive(true);
            SetMoonstones(0);
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudFullMoon);
        }

        private void OnFullMoonEnded() => fullMoonBanner.SetActive(false);

        private void UpdatePassengerGauge()
        {
            bool visible = passengers.GaugeVisible;
            if (passengerGauge.activeSelf != visible)
            {
                passengerGauge.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            // Marker shows where the water is relative to the dock; the green zone is the tolerance band.
            float range = passengers.AlignmentTolerance * 4f;
            float x = Mathf.Clamp(passengers.AlignmentOffset / range, -1f, 1f) * gaugeHalfWidth;
            gaugeMarker.anchoredPosition = new Vector2(x, gaugeMarker.anchoredPosition.y);
            gaugeZone.color = passengers.IsAligned ? alignedColor : misalignedColor;
        }

        private void OnWeatherWarning(WeatherKind kind, float secondsUntil)
        {
            weatherIcon.sprite = kind == WeatherKind.Storm ? stormIcon : kind == WeatherKind.Fog ? fogIcon : eclipseIcon;
            weatherBanner.alpha = 1f;
            UiTween.PopIn(weatherBanner.transform, 0.25f);
            if (kind == WeatherKind.Eclipse)
            {
                eclipseAt = Time.time + secondsUntil;
                lastEclipseSeconds = -1;
                UpdateEclipseCountdown();
            }
            else
            {
                eclipseAt = 0f;
                weatherText.SetKey(kind == WeatherKind.Storm ? LocKeys.HudStorm : LocKeys.HudFog);
            }
        }

        // Scaled time: the countdown must match gameplay time, including slow motion.
        private void UpdateEclipseCountdown()
        {
            if (eclipseAt <= 0f)
            {
                return;
            }

            int seconds = Mathf.CeilToInt(eclipseAt - Time.time);
            if (seconds != lastEclipseSeconds && seconds > 0)
            {
                lastEclipseSeconds = seconds;
                weatherText.SetKey(LocKeys.HudEclipse, seconds);
            }
        }

        private void OnWeatherStarted(WeatherKind kind)
        {
            if (kind == WeatherKind.Eclipse)
            {
                eclipseAt = 0f;
                weatherText.SetKey(LocKeys.HudEclipseNow);
            }
        }

        private void OnWeatherEnded(WeatherKind kind)
        {
            eclipseAt = 0f;
            weatherBanner.alpha = 0f;
        }

        private void OnBossHit(int hits, int required)
        {
            bossFill.fillAmount = hits / (float)Mathf.Max(1, required);
            bossLabel.SetText("{0}/{1}", hits, required);
            UiTween.Punch(bossPanel.transform, 0.2f);
        }

        private void OnBossDefeated()
        {
            calloutMultiplier.gameObject.SetActive(false);
            ShowCallout(LocKeys.HudBossDefeated);
        }

        private void OnRewindStarted() => rewindOverlay.SetActive(true);

        private void OnRewindFinished() => rewindOverlay.SetActive(false);

        private void OnAppPaused()
        {
            if (gameManager.State == GameState.Playing && !popups.IsShowing)
            {
                OpenPause();
            }
        }

        private void OpenPause()
        {
            if (gameManager.State != GameState.Playing || pausePopup.IsVisible)
            {
                return;
            }

            timeScale.PushPause();
            popups.Open(pausePopup);
        }
    }
}
