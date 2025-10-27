using Utility;
using ValveFileImporter.NavMeshDisplay;

namespace ValveFileImporter.Data.Structs
{
    public struct AreaConnection : INavMeshGizmoProvider
    {
        public int sourceAreaId;
        public int targetAreaId;
        public int sourceEdgeIndex;
        public int targetEdgeIndex;
        public float cost;

        public AreaConnection Reverse()
        {
            return new AreaConnection
            {
                sourceAreaId = targetAreaId,
                targetAreaId = sourceAreaId,
                sourceEdgeIndex = targetEdgeIndex,
                targetEdgeIndex = sourceEdgeIndex,
                cost = cost
            };
        }


        public void DrawGizmo(SourceNavMeshDisplay navMeshDisplay)
        {
            var fromArea = navMeshDisplay.GetArea(sourceAreaId);
            var toArea = navMeshDisplay.GetArea(targetAreaId);
            CustomGizmos.DrawArrow(fromArea.Center, toArea.Center);
        }
    }
}