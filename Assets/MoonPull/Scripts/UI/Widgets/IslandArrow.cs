using MoonPull.Rescue;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>Village view: steps the camera to the island on the left or right.</summary>
    public sealed class IslandArrow : MonoBehaviour
    {
        [SerializeField] private int direction = 1;

        private VillageDirector director;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Step);
        }

        private void Step()
        {
            if (director == null)
            {
                director = FindFirstObjectByType<VillageDirector>();
            }

            if (director != null)
            {
                director.FocusStep(direction);
                transform.localScale = Vector3.one * 0.88f;
            }
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }
    }
}
