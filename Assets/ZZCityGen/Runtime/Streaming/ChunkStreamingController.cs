using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Streaming
{
    public sealed class ChunkStreamingController : MonoBehaviour
    {
        [SerializeField] private Transform viewer;
        [SerializeField] private int activeChunkRadius = 3;
        [SerializeField] private int chunkSizeMeters = 256;
        private readonly HashSet<Vector2Int> activeChunks = new HashSet<Vector2Int>();

        public IReadOnlyCollection<Vector2Int> ActiveChunks => activeChunks;

        public void Configure(WorldGenerationSettings settings, MasterPlan plan)
        {
            Configure(settings, Camera.main?.transform);
        }

        public void Configure(WorldGenerationSettings settings, Transform viewerTransform)
        {
            if (settings == null)
            {
                return;
            }

            activeChunkRadius = settings.activeChunkRadius;
            chunkSizeMeters = settings.chunkSizeMeters;
            viewer = viewerTransform ?? Camera.main?.transform;
            RefreshActiveChunks(viewer != null ? viewer.position : Vector3.zero);
        }

        private void Update()
        {
            if (viewer != null)
            {
                RefreshActiveChunks(viewer.position);
            }
        }

        private void RefreshActiveChunks(Vector3 worldPosition)
        {
            var center = new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / Mathf.Max(1, chunkSizeMeters)),
                Mathf.FloorToInt(worldPosition.z / Mathf.Max(1, chunkSizeMeters)));

            activeChunks.Clear();
            for (var x = -activeChunkRadius; x <= activeChunkRadius; x++)
            {
                for (var y = -activeChunkRadius; y <= activeChunkRadius; y++)
                {
                    activeChunks.Add(new Vector2Int(center.x + x, center.y + y));
                }
            }
        }
    }
}
