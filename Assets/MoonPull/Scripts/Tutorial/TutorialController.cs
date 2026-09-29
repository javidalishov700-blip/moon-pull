using System;
using MoonPull.Boat;
using MoonPull.Core;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Tutorial
{
    /// <summary>
    /// Invisible tutorial. Level 1 shows a swiping hand until the first obstacle is cleared; every later mechanic
    /// shows its own gesture as its first intro placement approaches. Each hint appears until learned, then never again.
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        [Flags]
        private enum Lesson
        {
            None = 0,
            Tide = 1 << 0,
            Launch = 1 << 1,
            Treasure = 1 << 2,
            Dock = 1 << 3,
            Gate = 1 << 4,
            Whale = 1 << 5,
            Shark = 1 << 6,
            Eclipse = 1 << 7,
            Boss = 1 << 8,
            FirstWin = 1 << 9
        }

        [SerializeField] private LevelSession session;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private ObstacleInteractionSystem obstacles;
        [SerializeField] private BoatController boat;
        [SerializeField] private HandHint hand;
        [Tooltip("The hint appears when the intro placement is this far ahead of the boat.")]
        [SerializeField, Min(1f)] private float lookAhead = 9f;

        private Lesson active;
        private int targetIndex = -1;
        private bool shown;

        private SaveData Save => Services.Get<ISaveService>().Data;

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.RunFailed += OnRunFailed;
            GameEvents.WeatherWarning += OnWeatherWarning;
            GameEvents.WeatherEnded += OnWeatherEnded;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.RunFailed -= OnRunFailed;
            GameEvents.WeatherWarning -= OnWeatherWarning;
            GameEvents.WeatherEnded -= OnWeatherEnded;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.StateChanged -= OnStateChanged;
        }

        private bool Learned(Lesson lesson) => (Save.TutorialFlags & (int)lesson) != 0;

        private void OnLevelStarted(LevelStartArgs args)
        {
            hand.Hide();
            active = Lesson.None;
            targetIndex = -1;
            shown = false;

            LevelPlan plan = session.Plan;
            if (plan.IsTutorial && !Learned(Lesson.Tide))
            {
                Begin(Lesson.Tide, -1);
                hand.Show(HintGesture.SwipeUpDown);
                shown = true;
                GameEvents.RaiseTutorialStepCompleted(1, "tide_hint_shown");
                return;
            }

            Lesson lesson = plan.IsBoss && !Learned(Lesson.Boss) ? Lesson.Boss : LessonFor(plan.IntroducedSegment);
            if (lesson != Lesson.None && !Learned(lesson))
            {
                Begin(lesson, FindTarget(lesson == Lesson.Boss ? PlacementKind.KrakenSurface : (PlacementKind?)null));
            }
        }

        private void Update()
        {
            if (active == Lesson.None || active == Lesson.Eclipse)
            {
                return;
            }

            if (active == Lesson.Tide)
            {
                if (obstacles.ObstaclesPassed >= 1)
                {
                    Complete(2, "tide_learned");
                }

                return;
            }

            if (targetIndex < 0 || runner.Plan == null)
            {
                return;
            }

            LevelPlacement target = runner.GetPlacement(targetIndex);
            if (!shown && boat.X >= target.MinX - lookAhead)
            {
                shown = true;
                hand.Show(GestureFor(active));
            }
            else if (shown && boat.X > target.MaxX + 1f)
            {
                Complete(10 + BitIndex(active), active.ToString().ToLowerInvariant() + "_learned");
            }
        }

        private void OnWeatherWarning(WeatherKind kind, float secondsUntil)
        {
            if (kind == WeatherKind.Eclipse && !Learned(Lesson.Eclipse))
            {
                Begin(Lesson.Eclipse, -1);
                shown = true;
                hand.Show(HintGesture.Hold);
            }
        }

        private void OnWeatherEnded(WeatherKind kind)
        {
            if (kind == WeatherKind.Eclipse && active == Lesson.Eclipse)
            {
                Complete(10 + BitIndex(Lesson.Eclipse), "eclipse_learned");
            }
        }

        private void OnLevelCompleted(LevelResult result)
        {
            if (result.LevelIndex == 0 && !Learned(Lesson.FirstWin))
            {
                MarkLearned(Lesson.FirstWin);
                GameEvents.RaiseTutorialStepCompleted(3, "first_level_complete");
            }
        }

        // A failed attempt keeps the lesson unlearned, so the hint returns on retry.
        private void OnRunFailed(FailReason reason) => hand.Hide();

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to != GameState.Playing && to != GameState.Rewinding)
            {
                hand.Hide();
            }
        }

        private void Begin(Lesson lesson, int index)
        {
            active = lesson;
            targetIndex = index;
            shown = false;
        }

        private void Complete(int step, string name)
        {
            hand.Hide();
            MarkLearned(active);
            active = Lesson.None;
            targetIndex = -1;
            GameEvents.RaiseTutorialStepCompleted(step, name);
        }

        private void MarkLearned(Lesson lesson)
        {
            ISaveService save = Services.Get<ISaveService>();
            save.Data.TutorialFlags |= (int)lesson;
            save.MarkDirty();
        }

        private int FindTarget(PlacementKind? kind)
        {
            LevelPlan plan = session.Plan;
            for (int i = 0; i < plan.Placements.Count; i++)
            {
                LevelPlacement p = plan.Placements[i];
                bool match = kind.HasValue ? p.Kind == kind.Value : p.Has(PlacementFlags.Intro);
                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        private static Lesson LessonFor(LevelSegment? segment)
        {
            if (!segment.HasValue)
            {
                return Lesson.None;
            }

            switch (segment.Value)
            {
                case LevelSegment.LaunchRock: return Lesson.Launch;
                case LevelSegment.Treasure: return Lesson.Treasure;
                case LevelSegment.Dock: return Lesson.Dock;
                case LevelSegment.Gate: return Lesson.Gate;
                case LevelSegment.Whale: return Lesson.Whale;
                case LevelSegment.SharkZone: return Lesson.Shark;
                default: return Lesson.None;
            }
        }

        private static HintGesture GestureFor(Lesson lesson)
        {
            switch (lesson)
            {
                case Lesson.Launch:
                case Lesson.Boss: return HintGesture.FlickUp;
                case Lesson.Treasure: return HintGesture.SwipeDown;
                case Lesson.Whale:
                case Lesson.Shark: return HintGesture.SwipeUp;
                case Lesson.Dock:
                case Lesson.Gate: return HintGesture.Hold;
                default: return HintGesture.SwipeUpDown;
            }
        }

        private static int BitIndex(Lesson lesson)
        {
            int value = (int)lesson;
            int index = 0;
            while (value > 1)
            {
                value >>= 1;
                index++;
            }

            return index;
        }
    }
}
