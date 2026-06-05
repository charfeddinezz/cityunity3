using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Streaming
{
    public sealed class ChunkSystem : MonoBehaviour
    {
        [SerializeField] private int chunkSizeMeters = 256;
        [SerializeField] private int activeChunkRadius = 3;
        [SerializeField] private Transform chunksRoot;

        private readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

        public IReadOnlyDictionary<Vector2Int, Chunk> Chunks => chunks;

        public void Configure(WorldGenerationSettings settings, Transform worldRoot)
        {
            if (settings == null || worldRoot == null)
            {
                return;
            }

            chunkSizeMeters = settings.chunkSizeMeters;
            activeChunkRadius = settings.activeChunkRadius;
            if (chunksRoot == null || chunksRoot.parent != worldRoot)
            {
                ClearChunks();
                chunksRoot = new GameObject("Performance Chunks").transform;
                chunksRoot.SetParent(worldRoot, false);
            }
        }

        public void BuildChunks(Transform worldRoot)
        {
            if (worldRoot == null)
            {
                return;
            }

            ClearChunks();
            chunksRoot = new GameObject("Performance Chunks").transform;
            chunksRoot.SetParent(worldRoot, false);

            var renderers = worldRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.transform == chunksRoot || renderer.transform.IsChildOf(chunksRoot))
                {
                    continue;
                }

                var index = GetChunkIndex(renderer.transform.position);
                var chunk = GetOrCreateChunk(index);
                renderer.transform.SetParent(chunk.Root, true);
                chunk.AddObject(renderer.gameObject);
            }
        }

        public void SetActiveChunks(IReadOnlyCollection<Vector2Int> activeChunkIndices)
        {
            if (activeChunkIndices == null)
            {
                return;
            }

            foreach (var chunk in chunks.Values)
            {
                chunk.IsStreamingActive = activeChunkIndices.Contains(chunk.Index);
                chunk.UpdateActiveState();
            }
        }

        public void ApplyOcclusionCulling(Plane[] frustumPlanes)
        {
            if (frustumPlanes == null || frustumPlanes.Length == 0)
            {
                return;
            }

            foreach (var chunk in chunks.Values)
            {
                var visible = GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.Bounds);
                chunk.IsVisible = visible;
                chunk.UpdateActiveState();
            }
        }

        public Vector2Int GetChunkIndex(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / Mathf.Max(1, chunkSizeMeters)),
                Mathf.FloorToInt(worldPosition.z / Mathf.Max(1, chunkSizeMeters)));
        }

        public void ClearChunks()
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.Root == null)
                {
                    continue;
                }

                var restoreParent = chunksRoot != null ? chunksRoot.parent : null;
                while (chunk.Root.childCount > 0)
                {
                    var child = chunk.Root.GetChild(0);
                    if (restoreParent != null)
                    {
                        child.SetParent(restoreParent, true);
                    }
                    else
                    {
                        child.SetParent(null, true);
                    }
                }

                if (Application.isPlaying)
                {
                    Destroy(chunk.Root.gameObject);
                }
                else
                {
                    DestroyImmediate(chunk.Root.gameObject);
                }
            }

            chunks.Clear();
            if (chunksRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(chunksRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(chunksRoot.gameObject);
                }
                chunksRoot = null;
            }
        }

        private Chunk GetOrCreateChunk(Vector2Int index)
        {
            if (chunks.TryGetValue(index, out var chunk))
            {
                return chunk;
            }

            var root = new GameObject($"Chunk {index.x},{index.y}");
            root.transform.SetParent(chunksRoot, false);
            var boundsCenter = new Vector3(
                (index.x + 0.5f) * chunkSizeMeters,
                0f,
                (index.y + 0.5f) * chunkSizeMeters);
            chunk = new Chunk(index, root, new Bounds(boundsCenter, new Vector3(chunkSizeMeters, chunkSizeMeters, chunkSizeMeters)));
            chunks.Add(index, chunk);
            return chunk;
        }

        public sealed class Chunk
        {
            public Vector2Int Index { get; }
            public Transform Root { get; }
            public Bounds Bounds { get; }
            public bool IsStreamingActive { get; set; } = true;
            public bool IsVisible { get; set; } = true;

            public Chunk(Vector2Int index, Transform root, Bounds bounds)
            {
                Index = index;
                Root = root;
                Bounds = bounds;
            }

            public void AddObject(GameObject gameObject)
            {
                if (gameObject == null)
                {
                    return;
                }

                if (!IsVisible || !IsStreamingActive)
                {
                    gameObject.SetActive(false);
                }
            }

            public void UpdateActiveState()
            {
                if (Root != null)
                {
                    Root.gameObject.SetActive(IsStreamingActive && IsVisible);
                }
            }
        }
    }
}
