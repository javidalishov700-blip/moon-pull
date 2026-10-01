using MoonPull.Rescue;
using UnityEngine;

namespace MoonPull.Audio
{
    /// <summary>
    /// Background life for the menu map and village: rolling shoreline, seagulls now and then, and a murmur of
    /// villagers whose loudness follows how many people live there. During a night it thins to distant gulls.
    /// </summary>
    public sealed class ShoreAmbience : MonoBehaviour
    {
        [SerializeField] private AudioService audioService;
        [SerializeField] private AudioSource shore;
        [SerializeField] private AudioSource crowd;
        [SerializeField] private AudioSource gulls;
        [SerializeField] private AudioClip shoreClip;
        [SerializeField] private AudioClip crowdClip;
        [SerializeField] private AudioClip gullClip;
        [Tooltip("Population at which the crowd reaches full volume.")]
        [SerializeField, Min(1)] private int fullCrowd = 40;

        private float mapBlend;
        private float nextGull = 3f;

        private void Start()
        {
            Begin(shore, shoreClip);
            Begin(crowd, crowdClip);
            if (crowd != null) crowd.time = 3.1f; // de-phase from the shore loop
        }

        private static void Begin(AudioSource src, AudioClip clip)
        {
            if (src == null || clip == null) return;
            src.clip = clip;
            src.loop = true;
            src.volume = 0f;
            src.Play();
        }

        private void Update()
        {
            bool sfx = audioService == null || audioService.SfxEnabled;
            bool onMap = VillageDirector.Active || VillageDirector.MenuView;
            float dt = Time.unscaledDeltaTime;
            mapBlend = Mathf.MoveTowards(mapBlend, onMap ? 1f : 0f, dt * 0.8f);

            float people = Mathf.Clamp01(VillageState.Population / (float)fullCrowd);
            // Even a tiny village hums a little once anyone lives there.
            float crowdLevel = VillageState.Population > 0 ? Mathf.Lerp(0.06f, 0.32f, Mathf.Sqrt(people)) : 0f;
            if (VillageState.IsHungry) crowdLevel *= 0.6f; // hungry villages are quieter

            if (shore != null) shore.volume = sfx ? 0.32f * mapBlend : 0f;
            if (crowd != null) crowd.volume = sfx ? crowdLevel * (VillageDirector.Active ? 1f : 0.55f) * mapBlend : 0f;

            nextGull -= dt;
            if (nextGull <= 0f && gulls != null && gullClip != null)
            {
                nextGull = onMap ? Random.Range(4f, 11f) : Random.Range(14f, 30f);
                if (sfx)
                {
                    gulls.pitch = Random.Range(0.85f, 1.2f);
                    gulls.panStereo = Random.Range(-0.7f, 0.7f);
                    gulls.PlayOneShot(gullClip, onMap ? Random.Range(0.25f, 0.45f) : 0.12f);
                }
            }
        }
    }
}
