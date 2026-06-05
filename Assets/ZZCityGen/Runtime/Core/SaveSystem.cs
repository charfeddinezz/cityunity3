using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Core
{
    public sealed class SaveSystem : MonoBehaviour
    {
        public void SaveMasterPlan(MasterPlan plan, string filePath)
        {
            if (plan == null || string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("SaveMasterPlan called with invalid plan or path.");
                return;
            }

            WorldSaveUtility.SaveMasterPlan(plan, filePath);
        }

        public void SaveWorldPlan(WorldPlan plan, string filePath)
        {
            if (plan == null || string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("SaveWorldPlan called with invalid plan or path.");
                return;
            }

            WorldSaveUtility.SaveWorldPlan(plan, filePath);
        }

        public void ExportWorldPlan(WorldPlan plan, string filePath)
        {
            if (plan == null || string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("ExportWorldPlan called with invalid plan or path.");
                return;
            }

            WorldSaveUtility.SaveWorldPlan(plan, filePath);
        }
    }
}
