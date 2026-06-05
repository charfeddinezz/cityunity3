using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Generation
{
    public sealed class BuildingPlacementSystem
    {
        private readonly PrefabDatabase prefabDatabase;
        private readonly AssetCatalog assetCatalog;
        private readonly List<Bounds> occupiedBounds = new List<Bounds>();

        public BuildingPlacementSystem(PrefabDatabase prefabDatabase, AssetCatalog assetCatalog)
        {
            this.prefabDatabase = prefabDatabase;
            this.assetCatalog = assetCatalog;
        }

        public GameObject PlaceBuilding(Transform parent, DistrictPlan district, LotPlan lot, Vector3 worldPosition)
        {
            var lotSize = new Vector2(lot.widthMeters, lot.lengthMeters);
            var preferSkyscraper = district.type == DistrictType.Downtown || district.type == DistrictType.Business;
            var entry = FindBestBuildingEntry(district.type, lotSize, preferSkyscraper);
            var buildingObject = default(GameObject);
            var buildingHeight = 0f;
            var footprint = lotSize;

            if (entry != null && entry.prefab != null)
            {
                buildingObject = Object.Instantiate(entry.prefab, parent);
                buildingObject.name = entry.id;
                buildingHeight = Mathf.Max(1f, entry.heightMeters);
                footprint = entry.footprintMeters;
            }
            else
            {
                buildingObject = CreatePlaceholderBuilding(parent, district, lot, out buildingHeight, out footprint);
                buildingObject.name = lot.name;
            }

            var bounds = new Bounds(new Vector3(worldPosition.x, buildingHeight * 0.5f, worldPosition.z), new Vector3(footprint.x, buildingHeight, footprint.y));
            if (DetectOverlap(bounds))
            {
                Debug.LogWarning($"Building placement overlap detected in {district.name} for lot {lot.name}. Using fallback placeholder.");
                if (buildingObject != null)
                {
                    Object.DestroyImmediate(buildingObject);
                }

                buildingObject = CreatePlaceholderBuilding(parent, district, lot, out buildingHeight, out footprint);
                buildingObject.name = lot.name + " (Fallback)";
                bounds = new Bounds(new Vector3(worldPosition.x, buildingHeight * 0.5f, worldPosition.z), new Vector3(footprint.x, buildingHeight, footprint.y));
            }

            buildingObject.transform.position = new Vector3(worldPosition.x, buildingHeight * 0.5f, worldPosition.z);
            AddOccupied(bounds);

            UpdateLotMetadata(lot, entry, footprint, buildingHeight);
            return buildingObject;
        }

        private PrefabEntry FindBestBuildingEntry(DistrictType districtType, Vector2 lotSizeMeters, bool preferSkyscraper)
        {
            var preferredCategory = GetPreferredCategory(districtType);
            var candidate = default(PrefabEntry);
            var bestScore = float.MinValue;

            if (prefabDatabase != null)
            {
                foreach (var entry in prefabDatabase.prefabs)
                {
                    if (entry == null || !entry.IsAllowedIn(districtType) || !entry.Fits(lotSizeMeters))
                    {
                        continue;
                    }

                    var lotArea = Mathf.Max(1f, lotSizeMeters.x * lotSizeMeters.y);
                    var footprintArea = entry.footprintMeters.x * entry.footprintMeters.y;
                    var utilization = footprintArea / lotArea;
                    var score = utilization * 0.75f + entry.priority * 0.03f;

                    if (entry.category == preferredCategory || entry.category == PrefabCategory.Mixed)
                    {
                        score += 0.28f;
                    }

                    if (preferSkyscraper && entry.heightMeters > 45f)
                    {
                        score += 0.22f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        candidate = entry;
                    }
                }

                return candidate;
            }

            if (assetCatalog != null)
            {
                var bestAsset = assetCatalog.FindBestFit(districtType, lotSizeMeters);
                if (bestAsset != null)
                {
                    return new PrefabEntry
                    {
                        id = bestAsset.id,
                        prefab = bestAsset.prefab,
                        footprintMeters = bestAsset.footprintMeters,
                        heightMeters = bestAsset.heightMeters,
                        category = GetPreferredCategory(districtType),
                        priority = bestAsset.priority,
                        allowedDistricts = bestAsset.allowedDistricts,
                        plainText = bestAsset.id
                    };
                }
            }

            return null;
        }

        private PrefabCategory GetPreferredCategory(DistrictType districtType)
        {
            switch (districtType)
            {
                case DistrictType.Downtown:
                case DistrictType.Business:
                case DistrictType.Commercial:
                case DistrictType.Tourism:
                    return PrefabCategory.Commercial;
                case DistrictType.LuxuryResidential:
                case DistrictType.MiddleResidential:
                case DistrictType.PopularResidential:
                case DistrictType.Residential:
                    return PrefabCategory.Residential;
                case DistrictType.Industrial:
                    return PrefabCategory.Industrial;
                case DistrictType.Government:
                    return PrefabCategory.Government;
                case DistrictType.Education:
                case DistrictType.University:
                    return PrefabCategory.Education;
                case DistrictType.PublicPark:
                case DistrictType.Park:
                    return PrefabCategory.Park;
                case DistrictType.Airport:
                case DistrictType.Port:
                case DistrictType.FreightTerminal:
                case DistrictType.Utility:
                    return PrefabCategory.Infrastructure;
                case DistrictType.Luxury:
                    return PrefabCategory.Luxury;
                default:
                    return PrefabCategory.Generic;
            }
        }

        private GameObject CreatePlaceholderBuilding(Transform parent, DistrictPlan district, LotPlan lot, out float heightMeters, out Vector2 footprint)
        {
            var isSkyscraper = district.type == DistrictType.Downtown || district.type == DistrictType.Business;
            footprint = new Vector2(Mathf.Min(lot.widthMeters, 0.92f * lot.widthMeters), Mathf.Min(lot.lengthMeters, 0.92f * lot.lengthMeters));
            heightMeters = isSkyscraper ? Mathf.Lerp(18f, 72f, district.development) : Mathf.Lerp(6f, 20f, district.development);
            if (district.type == DistrictType.Industrial)
            {
                heightMeters = Mathf.Lerp(10f, 28f, district.development);
            }

            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.transform.SetParent(parent, false);
            placeholder.transform.localScale = new Vector3(footprint.x, heightMeters, footprint.y);
            placeholder.name = lot.name;
            return placeholder;
        }

        private bool DetectOverlap(Bounds bounds)
        {
            foreach (var occupied in occupiedBounds)
            {
                if (occupied.Intersects(bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddOccupied(Bounds bounds)
        {
            occupiedBounds.Add(bounds);
        }

        private void UpdateLotMetadata(LotPlan lot, PrefabEntry entry, Vector2 footprint, float heightMeters)
        {
            lot.matchedPrefabId = entry?.id ?? "None";
            lot.matchedPrefabCategory = entry?.category ?? PrefabCategory.Generic;
            lot.matchedFootprintMeters = footprint;
            lot.matchedHeightMeters = heightMeters;
            lot.matchedPrefabPlainText = entry?.plainText ?? $"Placeholder | {lot.zoneType} | {footprint.x:0.##}m x {footprint.y:0.##}m x {heightMeters:0.##}m";
        }
    }
}
