using MoonPull.Audio;
using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.TimeControl;
using MoonPull.Core;
using MoonPull.Gameplay.CameraControl;
using MoonPull.Haptics;
using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Feedback
{
    /// <summary>
    /// All game-feel responses in one place: sound, haptics, camera shake, slow motion, music. Mechanics only raise
    /// events, so juice can be tuned (or muted for tests) without touching gameplay code.
    /// </summary>
    public sealed class GameFeedbackDirector : MonoBehaviour
    {
        [SerializeField] private FeedbackConfig config;
        [SerializeField] private NearMissConfig nearMiss;
        [SerializeField] private AudioService audioService;
        [SerializeField] private TimeScaleController timeScale;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private BoatController boat;
        [SerializeField] private LevelSession session;

        private IHapticsService haptics;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.StarCollected += OnStar;
            GameEvents.CoinCollected += OnCoin;
            GameEvents.MoonstoneCollected += OnMoonstone;
            GameEvents.FullMoonStarted += OnFullMoonStarted;
            GameEvents.FullMoonEnded += OnFullMoonEnded;
            GameEvents.WaveLaunched += OnWaveLaunched;
            GameEvents.PerfectCrest += OnPerfectCrest;
            GameEvents.RunFailed += OnRunFailed;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.TreasureFound += OnTreasure;
            GameEvents.PassengerBoarded += OnPassengerBoarded;
            GameEvents.PassengersDelivered += OnPassengersDelivered;
            GameEvents.ShieldConsumed += OnShield;
            GameEvents.BossHit += OnBossHit;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.WeatherWarning += OnWeatherWarning;
            GameEvents.RewindStarted += OnRewind;
            boat.Landed += OnLanded;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.StarCollected -= OnStar;
            GameEvents.CoinCollected -= OnCoin;
            GameEvents.MoonstoneCollected -= OnMoonstone;
            GameEvents.FullMoonStarted -= OnFullMoonStarted;
            GameEvents.FullMoonEnded -= OnFullMoonEnded;
            GameEvents.WaveLaunched -= OnWaveLaunched;
            GameEvents.PerfectCrest -= OnPerfectCrest;
            GameEvents.RunFailed -= OnRunFailed;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.TreasureFound -= OnTreasure;
            GameEvents.PassengerBoarded -= OnPassengerBoarded;
            GameEvents.PassengersDelivered -= OnPassengersDelivered;
            GameEvents.ShieldConsumed -= OnShield;
            GameEvents.BossHit -= OnBossHit;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.WeatherWarning -= OnWeatherWarning;
            GameEvents.RewindStarted -= OnRewind;
            boat.Landed -= OnLanded;
        }

        private void Haptic(HapticType type)
        {
            if (haptics != null || Services.TryGet(out haptics))
            {
                haptics.Play(type);
            }
        }

        private void Shake(Vector2 amplitudeDuration) => cameraRig.Shake(amplitudeDuration.x, amplitudeDuration.y);

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu || to == GameState.Shop)
            {
                audioService.PlayMusic(audioService.Library.MenuMusic);
                audioService.SetMusicIntensity(0f);
            }
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            if (session.Region != null)
            {
                audioService.PlayMusic(session.Region.Music);
            }

            audioService.SetMusicIntensity(0f);
        }

        private void OnPerfectCrest(int streak)
        {
            audioService.PlaySfx(SfxId.Reward, Semitones(Mathf.Min(streak - 1, 7)));
            Shake(config.NearMissShake);
        }

        private void OnNearMiss(int chain, int multiplier)
        {
            timeScale.RequestSlowMotion(nearMiss.SlowMotionScale, nearMiss.SlowMotionHold, nearMiss.SlowMotionRecover);
            audioService.Duck(config.NearMissDuckDepth, config.NearMissDuckHold, config.NearMissDuckRecover);
            audioService.PlaySfx(SfxId.NearMiss, Semitones(Mathf.Min(chain - 1, 7)));
            Haptic(HapticType.Light);
            Shake(config.NearMissShake);
        }

        private void OnStar(int streak)
        {
            audioService.PlaySfx(SfxId.StarPickup, Semitones(Mathf.Min(streak - 1, config.MaxStarStreakSemitones)));
            Haptic(HapticType.Selection);
        }

        private void OnCoin(int amount) => audioService.PlaySfx(SfxId.CoinPickup);

        private void OnMoonstone(int count)
        {
            audioService.PlaySfx(SfxId.MoonstonePickup, Semitones(count * 2));
            Haptic(HapticType.Light);
        }

        private void OnFullMoonStarted(float duration)
        {
            audioService.PlaySfx(SfxId.FullMoonStart);
            audioService.SetMusicIntensity(1f);
            Haptic(HapticType.Success);
            Shake(config.FullMoonShake);
        }

        private void OnFullMoonEnded()
        {
            audioService.PlaySfx(SfxId.FullMoonEnd);
            audioService.SetMusicIntensity(0f);
        }

        private void OnWaveLaunched(float strength)
        {
            audioService.PlaySfx(SfxId.WaveLaunch, 0.9f + 0.2f * strength);
            Haptic(HapticType.Medium);
            cameraRig.Shake(config.LaunchShake.x * (0.5f + strength), config.LaunchShake.y);
        }

        private void OnLanded(float impact)
        {
            float loudness = Mathf.Clamp01(impact / config.SplashImpactForMax);
            audioService.PlaySfx(SfxId.Splash, 1f, 0.3f + 0.7f * loudness);
            if (impact >= config.HapticLandingImpact)
            {
                Haptic(HapticType.Light);
            }
        }

        private void OnRunFailed(FailReason reason)
        {
            audioService.PlaySfx(SfxId.Crash);
            audioService.PlaySfx(SfxId.LevelFail);
            audioService.SetMusicIntensity(0f);
            Haptic(HapticType.Heavy);
            Shake(config.CrashShake);
        }

        private void OnLevelCompleted(LevelResult result)
        {
            audioService.PlaySfx(SfxId.LevelComplete);
            Haptic(HapticType.Success);
        }

        private void OnTreasure(int coins)
        {
            audioService.PlaySfx(SfxId.ChestOpen);
            Haptic(HapticType.Medium);
        }

        private void OnPassengerBoarded(int onboard)
        {
            audioService.PlaySfx(SfxId.PassengerBoard);
            Haptic(HapticType.Selection);
        }

        private void OnPassengersDelivered(int count) => audioService.PlaySfx(SfxId.PassengerDeliver);

        private void OnShield()
        {
            audioService.PlaySfx(SfxId.ShieldHit);
            Haptic(HapticType.Medium);
            Shake(config.NearMissShake);
        }

        private void OnBossHit(int hits, int required)
        {
            audioService.PlaySfx(SfxId.KrakenHit, Semitones(hits));
            Haptic(HapticType.Heavy);
            Shake(config.BossHitShake);
        }

        private void OnBossDefeated()
        {
            audioService.PlaySfx(SfxId.KrakenDefeat);
            Haptic(HapticType.Success);
        }

        private void OnWeatherWarning(WeatherKind kind, float secondsUntil) => audioService.PlaySfx(SfxId.WeatherWarning);

        private void OnRewind() => audioService.PlaySfx(SfxId.Rewind);

        private static float Semitones(int steps) => Mathf.Pow(2f, steps / 12f);
    }
}
