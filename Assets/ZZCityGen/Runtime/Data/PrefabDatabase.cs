using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Data
{
    [CreateAssetMenu(menuName = "ZZ CityGen/Prefab Database", fileName = "ZZCityGenPrefabDatabase")]
    public sealed class PrefabDatabase : ScriptableObject
    {
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();

        public PrefabEntry FindBestMatch(DistrictType districtType, Vector2 lotSizeMeters)
        {
            PrefabEntry best = null;
            var bestScore = float.MinValue;
            var lotArea = Mathf.Max(1f, lotSizeMeters.x * lotSizeMeters.y);

            foreach (var entry in prefabs)
            {
                if (entry == null || !entry.IsAllowedIn(districtType) || !entry.Fits(lotSizeMeters))
                {
                    continue;
                }

                var footprintArea = entry.footprintMeters.x * entry.footprintMeters.y;
                var utilization = footprintArea / lotArea;
                var score = utilization * 0.8f + entry.priority * 0.02f;

                if (entry.category.ToString().Equals(districtType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.22f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = entry;
                }
            }

            return best;
        }

        public void RefreshDimensions()
        {
#if UNITY_EDITOR
            foreach (var entry in prefabs)
            {
                entry?.RefreshDimensions();
            }
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

    [Serializable]
    public sealed class PrefabEntry
    {
        public string id;
        public GameObject prefab;
        public Vector2 footprintMeters = new Vector2(20f, 20f);
        public float heightMeters = 10f;
        public PrefabCategory category = PrefabCategory.Generic;
        public int priority = 1;
        public List<DistrictType> allowedDistricts = new List<DistrictType>();
        public string plainText;

        public bool Fits(Vector2 lotSizeMeters)
        {
            return footprintMeters.x <= lotSizeMeters.x && footprintMeters.y <= lotSizeMeters.y;
        }

        public bool IsAllowedIn(DistrictType districtType)
        {
            return allowedDistricts.Count == 0 || allowedDistricts.Contains(districtType);
        }

#if UNITY_EDITOR
        public void RefreshDimensions()
        {
            if (prefab == null)
            {
                return;
            }

            var path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var contents = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try
            {
                var bounds = GetPrefabBounds(contents);
                footprintMeters = new Vector2(bounds.size.x, bounds.size.z);
                heightMeters = bounds.size.y;
                plainText = $"{id} | {category} | {footprintMeters.x:0.##}m x {footprintMeters.y:0.##}m x {heightMeters:0.##}m";
            }
            finally
            {
                UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private Bounds GetPrefabBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                return bounds;
            }

            var colliders = root.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                var bounds = colliders[0].bounds;
                foreach (var collider in colliders)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                return bounds;
            }

            return new Bounds(Vector3.zero, Vector3.one * 1f);
        }
#endif
    }

    public enum PrefabCategory
    {
        Generic,
        Residential,
        Commercial,
        Industrial,
        Government,
        Education,
        Park,
        Luxury,
        Infrastructure,
        Tourism,
        Transit,
        Utility,
        Mixed
    }
}
