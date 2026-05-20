using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using UnityEngine;
using Utility;
using ValveFileImporter.Data;
using ValveFileImporter.Data.Structs;
using ValveFileImporter.NavFlowMap;

namespace ValveFileImporter.NavMeshDisplay
{
    [ExecuteAlways]
    public partial class SourceNavMeshDisplay : MonoBehaviour
    {
        [SerializeField] private NavMeshDataSO navMeshData;

        [SerializeField] private NavFlowMapSO navFlowMap;

        [SerializeField] private Transform testPointA;
        [SerializeField] private Transform testPointB;
        public IEnumerable<AreaConnection> currentAreaPath;


        public IEnumerable<AreaCorner> currentCornerPath;


        private void OnEnable()

        {
            if (navMeshData == null || navMeshData.Areas == null)
            {
                return;
            }

            SetupData();
        }


        private void OnDrawGizmos()
        {
            if (currentAreaPath != null)
            {
                //DrawPath(visitedAreas.Select(GetArea), Color.white);
                DrawPath(currentAreaPath);
                //DrawPath(areaPath.Select(connection => GetArea(connection.targetAreaId)).Prepend(areaA));
                //Debug.Log(currentPath.Sum(connection => connection.cost));
            }

            if (currentCornerPath != null)
            {
                DrawPath(currentCornerPath);
            }
        }

        [Button]
        public void FindCornerPath()
        {
            currentCornerPath = FindPathThetaStar(testPointA.position, testPointB.position, out _).ToList();
            testPointA.position = currentCornerPath.First().GetOffsetPosition();
            testPointB.position = currentCornerPath.Last().GetOffsetPosition();
        }

        [Button]
        public void FindAreaPath()
        {
            var areaA = NearestArea(navMeshData, testPointA.position);
            var areaB = NearestArea(navMeshData, testPointB.position);

            currentAreaPath = FindAreaPath(areaA, areaB, out var visitedAreas).ToList();
        }

        private void DrawPath<T>(IEnumerable<T> path, Color? color = null) where T : INavMeshGizmoProvider
        {
            var pathList = path.ToList();
            for (var i = 0; i < pathList.Count; i++)
            {
                var gizmoProvider = pathList[i];
                Gizmos.color = color ?? Color.HSVToRGB(i / (float)pathList.Count, 1f, 1f);
                gizmoProvider.DrawGizmo(this);
            }
        }


        private void DrawPath(IEnumerable<AreaCorner> corners, Color? color = null)
        {
            var cornerList = corners.ToList();
            for (var i = 0; i < cornerList.Count - 1; i++)
            {
                var fromCorner = cornerList[i];
                var toCorner = cornerList[i + 1];
                var cornerColor = color ?? Color.HSVToRGB(i / (float)cornerList.Count, 1f, 1f);
                Gizmos.color = cornerColor;
                CustomGizmos.DrawArrow(fromCorner.position, toCorner.position);
            }
        }
    }
}