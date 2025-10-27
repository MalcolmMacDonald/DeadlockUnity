using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ValveFileImporter.NavFlowMap
{
    [ScriptedImporter(1, "navflowmap")]
    public class NavFlowMapImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var so = NavFlowMapSO.GetNavFlowMap(ctx.assetPath);
            ctx.AddObjectToAsset("navflowmap", so);
            ctx.SetMainObject(so);
        }
    }


    [CustomEditor(typeof(NavFlowMapImporter))]
    public class NavFlowMapImporterEditor : ScriptedImporterEditor

    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Reimport"))
            {
                ApplyAndReimport();
            }
        }

        public void ApplyAndReimport()
        {
            serializedObject.ApplyModifiedProperties();
            (target as ScriptedImporter)?.SaveAndReimport();
        }
    }
}