using System.Collections.Generic;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Meta;
using MoonPull.UI;
using UnityEngine;

namespace MoonPull.Gameplay.Visuals
{
    /// <summary>The menu island: shows the current region's lighthouse with every built stage, popping in new ones.</summary>
    public sealed class LighthouseIslandView : MonoBehaviour
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private Transform stageRoot;

        private readonly List<GameObject> stages = new List<GameObject>(8);
        private int region = -1;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            if (meta.IsInitialized)
            {
                Bind();
            }
            else
            {
                meta.Initialized += Bind;
            }
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            meta.Initialized -= Bind;
            if (meta.Lighthouses != null)
            {
                meta.Lighthouses.StageBuilt -= OnStageBuilt;
            }
        }

        private void Bind()
        {
            meta.Lighthouses.StageBuilt -= OnStageBuilt;
            meta.Lighthouses.StageBuilt += OnStageBuilt;
            Refresh();
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu && meta.IsInitialized)
            {
                Refresh();
            }
        }

        private void OnStageBuilt(int regionIndex, int stage)
        {
            if (regionIndex != region)
            {
                region = regionIndex;
                Rebuild(meta.Regions[region]);
            }

            ShowStages(stage);
            if (stage - 1 < stages.Count && stages[stage - 1] != null)
            {
                UiTween.PopIn(stages[stage - 1].transform, 0.4f);
            }
        }

        private void Refresh()
        {
            int current = meta.Progress.RegionOf(meta.Progress.NextLevelToPlay());
            if (current != region)
            {
                region = current;
                Rebuild(meta.Regions[region]);
            }

            ShowStages(meta.Lighthouses.StageOf(region));
        }

        // Instantiates only on region change (rare, menu-only), never during gameplay.
        private void Rebuild(RegionDefinition definition)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null)
                {
                    Destroy(stages[i]);
                }
            }

            stages.Clear();
            GameObject[] visuals = definition.LighthouseStageVisuals;
            for (int i = 0; i < visuals.Length; i++)
            {
                stages.Add(visuals[i] != null ? Instantiate(visuals[i], stageRoot, false) : null);
            }
        }

        private void ShowStages(int built)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null)
                {
                    stages[i].SetActive(i < built);
                }
            }
        }
    }
}
