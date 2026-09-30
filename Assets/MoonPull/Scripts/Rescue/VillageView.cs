using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>
    /// The harbor village on the menu island: one more lit house for every two people you have rescued, so the
    /// village visibly grows between nights.
    /// </summary>
    public sealed class VillageView : MonoBehaviour
    {
        [SerializeField] private GameObject[] houses = new GameObject[0];
        [SerializeField, Min(1)] private int peoplePerHouse = 2;

        [Tooltip("Building tiers, building-major: index = building * 3 + tier. Tier n shows once the building reaches level n+1.")]
        [SerializeField] private GameObject[] buildingTiers = new GameObject[0];

        private int builtSignature = -1;

        private int shown = -1;

        private void OnEnable() => Refresh();

        private void Update()
        {
            if (Time.frameCount % 30 == 0)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            int signature = 0;
            for (int b = 0; b < VillageService.BuildingCount; b++)
            {
                signature = signature * 4 + VillageService.Level((VillageBuilding)b);
            }

            if (signature != builtSignature)
            {
                builtSignature = signature;
                for (int i = 0; i < buildingTiers.Length; i++)
                {
                    if (buildingTiers[i] != null)
                    {
                        buildingTiers[i].SetActive(VillageService.Level((VillageBuilding)(i / VillageService.MaxLevel)) > i % VillageService.MaxLevel);
                    }
                }
            }

            int population = PlayerPrefs.GetInt(NightRescue.VillageKey, 0);
            int count = Mathf.Min(houses.Length, 1 + population / peoplePerHouse);
            if (count == shown)
            {
                return;
            }

            shown = count;
            for (int i = 0; i < houses.Length; i++)
            {
                if (houses[i] != null)
                {
                    houses[i].SetActive(i < count);
                }
            }
        }
    }
}
