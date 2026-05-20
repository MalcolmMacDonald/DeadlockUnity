using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using ValveFileImporter.ValveResourceFormat.NavMesh;

namespace ValveFileImporter.Data
{
    [ScriptedImporter(1, "nav")]
    public class NavMeshImporter : ScriptedImporter
    {
        public static float scale = 0.0254f; // Example scale factor
        public static Quaternion rotation = Quaternion.Euler(90, 270, 0);

        public static Matrix4x4 sourceToUnityMatrix = Matrix4x4.TRS(Vector3.zero, rotation, new Vector3(1, 1, -1) * scale);

        public int hullToStore = 1;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var navmeshFile = new NavMeshFile();
            navmeshFile.Read(ctx.assetPath);


            var index = 0;
            var areaCount = navmeshFile.Areas.Count;

            foreach (var (key, area) in navmeshFile.Areas)
            {
                //an area is a 3 or 4 sided polygon
                //the connections arrays contain the keys of the areas it connects to, associated with the edges

                var corners = area.Corners;
                if (key != area.AreaId)
                {
                    Debug.LogWarning($"Area key {key} does not match area id {area.AreaId}!");
                    continue;
                }


                var color = Color.HSVToRGB((float)index / areaCount, 1f, 1f);


                index++;
            }

            var navMeshData = ScriptableObject.CreateInstance<NavMeshDataSO>();
            navMeshData.Areas = navmeshFile.Areas.Values.Where(area => area.HullIndex == hullToStore).Select(area => new NavMeshArea
            {
                AreaId = (int)area.AreaId,
                HullIndex = area.HullIndex,
                DynamicAttributeFlags = area.DynamicAttributeFlags,
                Corners = area.Corners.Select(corner => sourceToUnityMatrix.MultiplyPoint(corner)).ToArray(),
                Normal = sourceToUnityMatrix.MultiplyVector(GetNormal(area.Corners)).normalized,
                Center = sourceToUnityMatrix.MultiplyPoint(GetCentroid(area.Corners)),
                Connections = area.Connections.SelectMany((connections, edgeIndex) => connections.Select(connection => new NavMeshConnection
                {
                    ConnectedAreaId = (int)connection.AreaId,
                    ThisEdgeIndex = edgeIndex,
                    OtherEdgeIndex = (int)connection.EdgeId
                })).ToArray(),
                LaddersAbove = area.LaddersAbove,
                LaddersBelow = area.LaddersBelow
            }).ToArray();
            ctx.AddObjectToAsset("navmeshData", navMeshData);
            ctx.SetMainObject(navMeshData);

            // Implement import logic here
        }

        private static Vector3 GetCentroid(Vector3[] points)
        {
            var centroid = Vector3.zero;
            foreach (var point in points)
            {
                centroid += point;
            }

            centroid /= points.Length;
            return centroid;
        }

        private static Vector3 GetNormal(Vector3[] points)
        {
            if (points.Length < 3)
            {
                return Vector3.up; // Default normal for less than 3 points
            }

            var v1 = points[1] - points[0];
            var v2 = points[2] - points[0];
            return Vector3.Cross(v1, v2);
        }
    }

    [CustomEditor(typeof(NavMeshImporter))]
    public class CustomNavmeshImporterEditor : ScriptedImporterEditor
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