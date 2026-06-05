using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Streaming
{
    public sealed class StreamingSystem : MonoBehaviour
    {
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private bool enableStreaming = true;

        private ChunkSystem chunkSystem;
        private ChunkStreamingController chunkController;
        private OcclusionCullingSystem occlusionCullingSystem;

        public void Configure(WorldGenerationSettings settings, ChunkSystem chunkSystem, ChunkStreamingController chunkController, Camera viewer)
        {
            if (settings == null)
            {
                return;
            }

            enableStreaming = settings.enableStreamingSystem;
            this.chunkSystem = chunkSystem;
            this.chunkController = chunkController;
            this.occlusionCullingSystem = GetComponent<OcclusionCullingSystem>();
            viewerCamera = viewer ?? Camera.main;

            if (this.chunkController != null)
            {
                this.chunkController.Configure(settings, viewerCamera != null ? viewerCamera.transform : null);
            }
        }

        private void Update()
        {
            if (!enableStreaming || chunkController == null || chunkSystem == null)
            {
                return;
            }

            chunkSystem.SetActiveChunks(chunkController.ActiveChunks);
        }
    }
}
