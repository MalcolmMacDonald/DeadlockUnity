using UnityEngine;

namespace Utility
{
    public static class GeometryFunctions
    {
        public static bool IntersectLineSegment(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2, out Vector3 intersection)
        {
            var d1 = b1 - a1;
            var d2 = b2 - a2;
            var cross = Vector3.Cross(d1, d2);
            var denom = cross.sqrMagnitude;

            if (denom < Mathf.Epsilon)
            {
                intersection = Vector3.zero;
                return false;
            }

            var t = Vector3.Dot(Vector3.Cross(a2 - a1, d2), cross) / denom;
            var u = Vector3.Dot(Vector3.Cross(a2 - a1, d1), cross) / denom;

            if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
            {
                intersection = a1 + d1 * t;
                return true;
            }

            intersection = Vector3.zero;
            return false;
        }

        public static Vector3 ClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point, out float t)
        {
            var ab = b - a;
            t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }
    }
}