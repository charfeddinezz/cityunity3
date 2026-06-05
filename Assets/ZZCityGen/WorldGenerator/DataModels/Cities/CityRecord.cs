using UnityEngine;

namespace ZZCityGen.WorldGenerator.DataModels.Cities
{
    [System.Serializable]
    public class CityRecord
    {
        public string id;
        public string name;
        public Vector2 center;
        public float area;
        public int estimatedPopulation;
        public string[] tags;
    }
}