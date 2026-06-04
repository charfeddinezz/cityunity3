using System.Collections.Generic;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Plugins
{
    public sealed class PluginRegistry : MonoBehaviour
    {
        private readonly List<IZCityGenPlugin> runtimePlugins = new List<IZCityGenPlugin>();

        public IReadOnlyList<IZCityGenPlugin> RuntimePlugins => runtimePlugins;

        public void Register(IZCityGenPlugin plugin)
        {
            if (plugin != null && !runtimePlugins.Contains(plugin))
            {
                runtimePlugins.Add(plugin);
            }
        }

        public void Unregister(IZCityGenPlugin plugin)
        {
            runtimePlugins.Remove(plugin);
        }

        public void ApplyMasterPlanExtensions(MasterPlan plan, WorldGenerationSettings settings)
        {
            foreach (var plugin in runtimePlugins)
            {
                plugin.ExtendMasterPlan(plan, settings);
            }
        }
    }
}
