using System.Collections.Generic;
using System.Linq;
using ZZCityGen.WorldGenerator.DataModels.World;

namespace ZZCityGen.WorldGenerator.Core
{
    public class MasterDatabase
    {
        private readonly Dictionary<string, AssetRecord> assets = new Dictionary<string, AssetRecord>();

        public void Register(AssetRecord record)
        {
            if (string.IsNullOrEmpty(record.id)) record.id = System.Guid.NewGuid().ToString();
            assets[record.id] = record;
        }

        public AssetRecord GetById(string id)
        {
            assets.TryGetValue(id, out var r);
            return r;
        }

        public IEnumerable<AssetRecord> QueryByTag(string tag)
        {
            return assets.Values.Where(a => a.tags != null && System.Array.IndexOf(a.tags, tag) >= 0);
        }

        public IEnumerable<AssetRecord> QueryByCategory(string category)
        {
            return assets.Values.Where(a => a.category == category);
        }
    }
}