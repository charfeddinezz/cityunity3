using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Streaming
{
    public sealed class OcclusionCullingSystem : MonoBehaviour
    {
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private float refreshInterval = 0.25f;

        private ChunkSystem chunkSystem;
        private float timer;

        public void Configure(WorldGenerationSettings settings, ChunkSystem chunkSystem, Camera viewer)
        {
            if (settings == null)
            {
                return;
            }

            enableOcclusion = settings.enableOcclusionCulling;
            refreshInterval = Mathf.Max(0.05f, settings.occlusionRefreshInterval);
            this.chunkSystem = chunkSystem;
            viewerCamera = viewer ?? Camera.main;
            timer = refreshInterval;
        }

        private void Update()
        {
            if (!enableOcclusion || chunkSystem == null || viewerCamera == null)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = refreshInterval;
            var planes = GeometryUtility.CalculateFrustumPlanes(viewerCamera);
            chunkSystem.ApplyOcclusionCulling(planes);
        }
    }
}
