namespace ZZCityGen.WorldGenerator.DataModels.World
{
    [System.Serializable]
    public class AssetRecord
    {
        public string id;
        public string name;
        public string category;
        public float width;
        public float length;
        public float height;
        public string[] tags;
    }
}