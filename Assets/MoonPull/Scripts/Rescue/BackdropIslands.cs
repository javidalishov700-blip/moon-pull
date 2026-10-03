using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>
    /// Little islands on the horizon behind the boat. They stay put in the world while the camera sails past and
    /// wrap ahead when they fall behind, so the sea always has somewhere to look at. Hidden on the village map.
    /// </summary>
    public sealed class BackdropIslands : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform[] islands;
        [SerializeField, Min(5f)] private float spacing = 26f;

        private System.Random random = new System.Random(11);
        private bool shown = true;

        private void LateUpdate()
        {
            bool show = !VillageDirector.Active && !VillageDirector.MenuView;
            if (show != shown)
            {
                shown = show;
                foreach (Transform t in islands) t.gameObject.SetActive(show);
            }

            if (!show || cameraTransform == null || islands == null || islands.Length == 0)
            {
                return;
            }

            float cx = cameraTransform.position.x;
            float span = spacing * islands.Length;
            foreach (Transform t in islands)
            {
                Vector3 p = t.position;
                if (p.x < cx - span * 0.4f)
                {
                    p.x += span;
                    p.z = 30f + (float)random.NextDouble() * 16f;
                    t.localScale = Vector3.one * (0.7f + (float)random.NextDouble() * 0.6f);
                    t.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                    t.position = p;
                }
                else if (p.x > cx + span * 0.6f)
                {
                    p.x -= span; // a reset sent the camera back to the start
                    t.position = p;
                }
            }
        }
    }
}
