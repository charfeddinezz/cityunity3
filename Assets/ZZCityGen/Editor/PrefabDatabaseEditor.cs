using UnityEditor;
using UnityEngine;
using ZZCityGen.Data;

namespace ZZCityGen.Editor
{
    [CustomEditor(typeof(PrefabDatabase))]
    public sealed class PrefabDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Prefab Dimensions"))
            {
                ((PrefabDatabase)target).RefreshDimensions();
            }
        }
    }
}
