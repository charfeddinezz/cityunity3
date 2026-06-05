namespace ZZCityGen.WorldGenerator.Core.SaveLoad
{
    public interface ISaveLoadProvider
    {
        string Save(string key, string json);
        string Load(string key);
        bool Exists(string key);
    }
}