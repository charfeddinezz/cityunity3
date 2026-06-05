using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Streaming
{
    public sealed class LODGenerator : MonoBehaviour
    {
        [SerializeField] private int lodLevels = 4;
        [SerializeField] private float[] lodTransitionHeights = new float[] { 0.6f, 0.3f, 0.12f, 0.03f };

        public void Configure(WorldGenerationSettings settings, MasterPlan plan)
        {
            if (settings == null)
            {
                return;
            }

            lodLevels = Mathf.Max(1, settings.lodLevels);
            lodTransitionHeights = new[] { 0.6f, 0.3f, 0.12f, 0.03f };
            if (lodLevels < lodTransitionHeights.Length)
            {
                var resized = new float[lodLevels];
                for (var i = 0; i < lodLevels; i++)
                {
                    resized[i] = lodTransitionHeights[i];
                }
                lodTransitionHeights = resized;
            }
        }

        public void GenerateLods(Transform worldRoot)
        {
            if (worldRoot == null || lodLevels <= 1)
            {
                return;
            }

            var renderers = worldRoot.GetComponentsInChildren<Renderer>(true);
            var visited = new HashSet<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null || visited.Contains(renderer) || renderer.GetComponent<LODGroup>() != null)
                {
                    continue;
                }

                visited.Add(renderer);
                GenerateLodGroup(renderer);
            }
        }

        private void GenerateLodGroup(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            var lodGroup = renderer.gameObject.AddComponent<LODGroup>();
            var renderers = new[] { renderer };
            var lods = new LOD[lodLevels];
            for (var i = 0; i < lodLevels; i++)
            {
                var height = i < lodTransitionHeights.Length ? lodTransitionHeights[i] : 0.01f;
                lods[i] = new LOD(height, renderers);
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }
    }
}
