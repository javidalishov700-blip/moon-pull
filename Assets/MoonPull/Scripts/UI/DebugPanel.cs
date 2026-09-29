using MoonPull.Core;
using MoonPull.Level;
using MoonPull.Meta;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Development-only cheats. The body compiles out of release builds (the empty class stays so the scene has no
    /// missing-script component). Toggle: F1 or a three-finger tap.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private MetaGame meta;
        [SerializeField] private LevelSession session;
        [SerializeField] private GameManager gameManager;

        private bool visible;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) || (Input.touchCount == 3 && Input.GetTouch(2).phase == TouchPhase.Began))
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible || !meta.IsInitialized)
            {
                return;
            }

            float scale = Screen.height / 1280f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(20f, 120f, 460f, 1000f), GUI.skin.box);
            ISaveService save = Services.Get<ISaveService>();
            GUILayout.Label($"State {gameManager.State}  Coins {meta.Wallet.Coins}  Keys {meta.Wallet.Keys}");
            GUILayout.Label($"Lifetime {save.Data.Ads.LifetimePlaySeconds:F0}s  Sessions {save.Data.Ads.SessionCount}  SinceAd {save.Data.Ads.LevelsSinceInterstitial}");

            if (GUILayout.Button("+1000 coins")) meta.Wallet.AddCoins(1000, "debug");
            if (GUILayout.Button("+3 keys")) meta.Wallet.AddKeys(3);
            if (GUILayout.Button("+1 boss chest")) meta.BossChests.AddChest();
            if (GUILayout.Button("Unlock all levels"))
            {
                save.Data.HighestUnlockedLevel = meta.Progress.LevelCount - 1;
                save.MarkDirty();
            }

            if (GUILayout.Button("Skip ad grace & cooldown"))
            {
                save.Data.Ads.LifetimePlaySeconds = 10000;
                save.Data.Ads.LastFullscreenAdUtcTicks = 0;
                save.Data.Ads.LevelsSinceInterstitial = 10;
                save.Data.Ads.SkipNextInterstitial = false;
            }

            if (GUILayout.Button("Reset tutorial")) save.Data.TutorialFlags = 0;

            if (gameManager.State == GameState.Playing && GUILayout.Button("Win level (3 stars)"))
            {
                LevelStartArgs args = session.Args;
                GameEvents.RaiseLevelCompleted(new LevelResult(args.LevelIndex, 3, session.Plan.ThreeStarScore, 30f, 5, 20, 5, 0, 0, session.Plan.IsBoss, false));
            }

            if (GUILayout.Button("Wipe save (restart app)"))
            {
                save.ResetAll();
                save.SaveNow();
            }

            GUILayout.EndArea();
        }
#endif
    }
}
