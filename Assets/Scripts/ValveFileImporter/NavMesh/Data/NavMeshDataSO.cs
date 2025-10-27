using System;
using UnityEngine;
using ValveFileImporter.NavMeshDisplay;
using ValveFileImporter.ValveResourceFormat.NavMesh;

namespace ValveFileImporter.Data
{
    public class NavMeshDataSO : ScriptableObject
    {
        [HideInInspector] [SerializeField] public NavMeshArea[] Areas;
    }

    [Serializable]
    public struct NavMeshArea : IEquatable<NavMeshArea>, INavMeshGizmoProvider
    {
        public int AreaId;

        public byte HullIndex;
        public DynamicAttributeFlags DynamicAttributeFlags;
        public Vector3[] Corners;
        public uint[] LaddersAbove;
        public uint[] LaddersBelow;
        public NavMeshConnection[] Connections;
        public Vector3 Normal;
        public Vector3 Center;

        public bool Equals(NavMeshArea other)
        {
            return AreaId == other.AreaId && HullIndex == other.HullIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is NavMeshArea other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AreaId, HullIndex);
        }

        public void DrawGizmo(SourceNavMeshDisplay _)
        {
            for (var i = 0; i < Corners.Length; i++)
            {
                var start = Corners[i];
                var end = Corners[(i + 1) % Corners.Length];
                Gizmos.DrawLine(start, end);
            }
        }

        public static bool operator ==(NavMeshArea left, NavMeshArea right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NavMeshArea left, NavMeshArea right)
        {
            return !left.Equals(right);
        }
    }

    [Serializable]
    public struct NavMeshConnection
    {
        public int ThisEdgeIndex;
        public int ConnectedAreaId;
        public int OtherEdgeIndex;
    }
}