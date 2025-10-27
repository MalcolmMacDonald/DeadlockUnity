using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using ValveFileImporter.NavMeshDisplay;

namespace ValveFileImporter.Data.Structs
{
    public struct AreaCorner : IEquatable<AreaCorner>, INavMeshGizmoProvider
    {
        public Vector3 position;
        public int areaId;
        public int cornerIndex;
        public Vector3 normal;

        public bool Equals(AreaCorner other)
        {
            return position.Equals(other.position) && areaId == other.areaId && cornerIndex == other.cornerIndex && normal.Equals(other.normal);
        }

        public override bool Equals(object obj)
        {
            return obj is AreaCorner other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(areaId, cornerIndex);
        }

        public void DrawGizmo(SourceNavMeshDisplay _)
        {
            Gizmos.DrawRay(position, normal * 0.5f);
        }
    }

    public static class AreaCornerExtensions
    {
        public const float NormalOffset = .75f;
        public const float SphereCastRadius = NormalOffset * 0.6f;

        public static Vector3 GetOffsetPosition(this AreaCorner corner)
        {
            return corner.position + corner.normal * NormalOffset;
        }

        public static IEnumerable<AreaCorner> GetCorners(this NavMeshArea area)
        {
            var cornerCount = area.Corners.Length;
            for (var i = 0; i < cornerCount; i++)
            {
                var a = area.Corners[i];
                yield return new AreaCorner
                {
                    position = a,
                    areaId = area.AreaId,
                    cornerIndex = i,
                    normal = area.Normal
                };
            }
        }

        public static bool IsVisible(AreaCorner a, AreaCorner b, out RaycastHit hit)
        {
            var offsetA = a.GetOffsetPosition();
            var offsetB = b.GetOffsetPosition();
            var toB = offsetB - offsetA;
            return !Physics.SphereCast(offsetA, SphereCastRadius, toB, out hit, toB.magnitude)
                   && !Physics.SphereCast(offsetB, SphereCastRadius, -toB, out hit, toB.magnitude);
        }

        public static void DrawCornerVisibility(AreaCorner initialCorner, AreaCorner corner)
        {
            var cornerPoint = corner.GetOffsetPosition();
            var initialCornerPoint = initialCorner.GetOffsetPosition();

            CustomGizmos.DrawCapsule(Vector3.zero, Vector3.forward * Vector3.Distance(cornerPoint, initialCornerPoint), SphereCastRadius);

            if (!IsVisible(corner, initialCorner, out var hit))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, 0.1f);
            }
        }
    }
}