#pragma warning disable 618 // Unity marks Social deprecated but it is still the Game Center bridge on iOS
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace MoonPull.Online
{
    /// <summary>
    /// Game Center leaderboard "most people rescued in one night". Keeps a local personal best, reports every
    /// night, and finds the next player just above you so the menu can say who to beat.
    /// Create the leaderboard in App Store Connect with id <see cref="BestNightId"/>.
    /// </summary>
    public static class Leaderboards
    {
        public const string BestNightId = "moonpull_best_night";
        private const string BestKey = "mp_best_night";

        public static int PersonalBest => PlayerPrefs.GetInt(BestKey, 0);
        public static string RivalName { get; private set; }
        public static long RivalScore { get; private set; }
        public static bool SignedIn => Application.platform == RuntimePlatform.IPhonePlayer && UnityEngine.Social.localUser.authenticated;

        public static event Action Changed;

        public static void Init()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                return;
            }

            UnityEngine.Social.localUser.Authenticate(ok =>
            {
                if (ok)
                {
                    if (PersonalBest > 0) Report(PersonalBest);
                    RefreshRival();
                }
            });
        }

        /// <summary>Called at dawn. Returns true when this night beat the personal best.</summary>
        public static bool ReportNight(int rescued)
        {
            bool record = rescued > PersonalBest;
            if (record)
            {
                PlayerPrefs.SetInt(BestKey, rescued);
                PlayerPrefs.Save();
            }

            if (SignedIn)
            {
                Report(Mathf.Max(rescued, PersonalBest));
                RefreshRival();
            }

            Changed?.Invoke();
            return record;
        }

        public static void Show()
        {
            if (SignedIn)
            {
                UnityEngine.Social.ShowLeaderboardUI();
            }
            else
            {
                Init();
            }
        }

        private static void Report(long score)
        {
            UnityEngine.Social.ReportScore(score, BestNightId, _ => { });
        }

        /// <summary>Finds the lowest score above ours among the top 100.</summary>
        private static void RefreshRival()
        {
            ILeaderboard board = UnityEngine.Social.CreateLeaderboard();
            board.id = BestNightId;
            board.userScope = UserScope.Global;
            board.range = new UnityEngine.SocialPlatforms.Range(1, 100);
            board.LoadScores(ok =>
            {
                if (!ok || board.scores == null)
                {
                    return;
                }

                IScore best = null;
                foreach (IScore s in board.scores)
                {
                    if (s.value > PersonalBest && s.userID != UnityEngine.Social.localUser.id && (best == null || s.value < best.value))
                    {
                        best = s;
                    }
                }

                if (best == null)
                {
                    RivalName = null;
                    RivalScore = 0;
                    Changed?.Invoke();
                    return;
                }

                RivalScore = best.value;
                UnityEngine.Social.LoadUsers(new[] { best.userID }, users =>
                {
                    RivalName = users != null && users.Length > 0 ? users[0].userName : "?";
                    Changed?.Invoke();
                });
            });
        }
    }
}
