using ZZCityGen.Data;

namespace ZZCityGen.Plugins
{
    public interface IZCityGenPlugin
    {
        string PluginName { get; }
        void ExtendMasterPlan(MasterPlan plan, WorldGenerationSettings settings);
    }
}
