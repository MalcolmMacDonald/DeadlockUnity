using UnityEngine;

namespace Utility
{
    public static class CustomGizmos
    {
        public static void DrawArrow(Vector3 from, Vector3 to)
        {
            if (from == to)
            {
                return;
            }

            Gizmos.DrawLine(from, to);
            var direction = (to - from).normalized;
            var right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            var left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
            Gizmos.DrawLine(to, to + right * 0.2f);
            Gizmos.DrawLine(to, to + left * 0.2f);
        }

        public static void DrawCapsule(Vector3 a, Vector3 b, float radius)
        {
            var originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(a, Quaternion.LookRotation(b - a), Vector3.one);

            Gizmos.DrawWireSphere(a, radius);
            Gizmos.DrawWireSphere(b, radius);
            var direction = (b - a).normalized;
            var distance = Vector3.Distance(a, b);
            var radialSegments = 12;
            for (var i = 0; i < radialSegments; i++)
            {
                var angle = i / (float)radialSegments * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                var rotation = Quaternion.LookRotation(direction);
                var pointA = a + rotation * offset;
                var pointB = b + rotation * offset;
                Gizmos.DrawLine(pointA, pointB);
            }

            Gizmos.matrix = originalMatrix;
        }
    }
}