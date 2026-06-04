using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZZCityGen.Data
{
    [CreateAssetMenu(menuName = "ZZ CityGen/Asset Catalog", fileName = "ZZCityGenAssetCatalog")]
    public sealed class AssetCatalog : ScriptableObject
    {
        public List<PlaceableAssetDefinition> assets = new List<PlaceableAssetDefinition>();

        public PlaceableAssetDefinition FindBestFit(DistrictType districtType, Vector2 lotSizeMeters)
        {
            PlaceableAssetDefinition best = null;
            var bestScore = float.MinValue;

            foreach (var asset in assets)
            {
                if (asset == null || !asset.IsAllowedIn(districtType) || !asset.Fits(lotSizeMeters))
                {
                    continue;
                }

                var footprintArea = asset.footprintMeters.x * asset.footprintMeters.y;
                var lotArea = Mathf.Max(1f, lotSizeMeters.x * lotSizeMeters.y);
                var utilization = footprintArea / lotArea;
                var score = utilization + asset.priority * 0.01f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = asset;
                }
            }

            return best;
        }
    }

    [Serializable]
    public sealed class PlaceableAssetDefinition
    {
        public string id;
        public GameObject prefab;
        public Vector2 footprintMeters = new Vector2(20f, 20f);
        public float heightMeters = 10f;
        public int priority = 1;
        public List<DistrictType> allowedDistricts = new List<DistrictType>();

        public bool Fits(Vector2 lotSizeMeters)
        {
            return footprintMeters.x <= lotSizeMeters.x && footprintMeters.y <= lotSizeMeters.y;
        }

        public bool IsAllowedIn(DistrictType districtType)
        {
            return allowedDistricts.Count == 0 || allowedDistricts.Contains(districtType);
        }
    }
}
