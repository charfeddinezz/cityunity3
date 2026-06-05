using System;
using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;
using ZZCityGen.Utilities;

namespace ZZCityGen.Planning
{
    public sealed class LotGenerator
    {
        private readonly WorldGenerationSettings settings;
        private readonly System.Random random;
        private readonly SeededNameGenerator names;

        public LotGenerator(WorldGenerationSettings settings, System.Random random, SeededNameGenerator names)
        {
            this.settings = settings ?? new WorldGenerationSettings();
            this.random = random ?? new System.Random(this.settings.worldSeed);
            this.names = names ?? new SeededNameGenerator(this.settings.worldSeed);
        }

        public List<LotPlan> BuildLots(DistrictPlan district)
        {
            var lots = new List<LotPlan>();
            if (district == null)
            {
                return lots;
            }

            var lotsPerAxis = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 9f, district.density)), 1, 16);
            var lotSize = new Vector2(district.bounds.width / lotsPerAxis, district.bounds.height / lotsPerAxis);

            for (var x = 0; x < lotsPerAxis; x++)
            {
                for (var y = 0; y < lotsPerAxis; y++)
                {
                    if ((district.type == DistrictType.Park || district.type == DistrictType.PublicPark) && (x + y) % 3 != 0)
                    {
                        continue;
                    }

                    var center = new Vector2(
                        district.bounds.xMin + lotSize.x * (x + 0.5f),
                        district.bounds.yMin + lotSize.y * (y + 0.5f));

                    var width = Mathf.Max(1f, lotSize.x);
                    var length = Mathf.Max(1f, lotSize.y);
                    var area = width * length;
                    var lotName = $"{district.name} Lot {x + 1}-{y + 1}";

                    lots.Add(new LotPlan
                    {
                        name = lotName,
                        districtName = district.name,
                        center = center,
                        widthMeters = width,
                        lengthMeters = length,
                        areaSquareMeters = area,
                        zoneType = district.type,
                        plainText = $"{lotName} | {district.type} | {width:0.##}m x {length:0.##}m | {area:0.##}m²"
                    });
                }
            }

            return lots;
        }
    }
}
