using ValveFileImporter.NavMeshDisplay;

namespace ValveFileImporter.Data
{
    public interface INavMeshGizmoProvider
    {
        public void DrawGizmo(SourceNavMeshDisplay navMeshDisplay);
    }
}