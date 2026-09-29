using System.Runtime.InteropServices;
using UnityEngine;

namespace MoonPull.Haptics
{
    public enum HapticType
    {
        Selection,
        Light,
        Medium,
        Heavy,
        Success
    }

    public interface IHapticsService
    {
        bool Enabled { get; set; }
        void Play(HapticType type);
    }

    /// <summary>
    /// Native haptics: UIImpactFeedbackGenerator on iOS (MoonPullHaptics.mm), VibrationEffect on Android 8+.
    /// Short, distinct pulses per event; long buzzes feel cheap and drain battery.
    /// </summary>
    public sealed class NativeHapticsService : IHapticsService
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MoonPull_Haptic(int type);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const int EffectClick = 0;
        private const int EffectTick = 2;
        private const int EffectHeavyClick = 5;
        private AndroidJavaObject vibrator;
        private AndroidJavaClass vibrationEffect;
        private int sdkLevel;
#endif

        public NativeHapticsService()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdkLevel = version.GetStatic<int>("SDK_INT");
                }

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (sdkLevel >= 26)
                {
                    vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Haptics] Android vibrator unavailable: {e.Message}");
                vibrator = null;
            }
#endif
        }

        public bool Enabled { get; set; } = true;

        public void Play(HapticType type)
        {
            if (!Enabled)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            MoonPull_Haptic((int)type);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(type);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void PlayAndroid(HapticType type)
        {
            if (vibrator == null)
            {
                return;
            }

            if (sdkLevel >= 29)
            {
                int effectId = type == HapticType.Heavy || type == HapticType.Success ? EffectHeavyClick
                    : type == HapticType.Selection ? EffectTick : EffectClick;
                using (AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>("createPredefined", effectId))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else if (sdkLevel >= 26)
            {
                long ms = type == HapticType.Heavy ? 40 : type == HapticType.Medium ? 25 : 12;
                int amplitude = type == HapticType.Heavy ? 255 : type == HapticType.Medium ? 160 : 80;
                using (AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", type == HapticType.Heavy ? 40L : 15L);
            }
        }

        // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android manifest. Never called.
        private static void EnsureVibratePermission() => Handheld.Vibrate();
#endif
    }
}
