using UnityEngine;

namespace ZZCityGen.WorldGenerator.DataModels.Lots
{
    [System.Serializable]
    public class LotRecord
    {
        public string id;
        public Vector2[] polygon;
        public float area;
        public string allowedUsage; // e.g., residential, commercial, park
        public string[] tags;
    }
}