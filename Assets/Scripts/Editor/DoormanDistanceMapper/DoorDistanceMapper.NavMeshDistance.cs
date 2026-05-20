using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public partial class DoorDistanceMapper
{
    public bool GetPointOnNavMesh(Vector3 inPoint, out Vector3 point)
    {
        if (NavMesh.SamplePosition(inPoint, out var hit, float.MaxValue, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = inPoint;
        return false;
    }


    private void DrawFullPath(FullPathResult fullPath, Color color)
    {
        for (var i = 0; i < fullPath.paths.Count; i++)
        {
            var path = fullPath.paths[i];


            DrawPath(path, color);
        }
    }

    private void DrawPath(Vector3[] path, Color color)
    {
        if (path.Length < 2)
        {
            return;
        }
        //get off mesh links  

        for (var i = 1; i < path.Length; i++)
        {
            Debug.DrawLine(path[i - 1], path[i], color);
        }
    }

    /*
    private static bool GetFullPath(Vector3 a, Vector3 b, out FullPathResult path)
    {
        var forwardNavMeshPath = new NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(a, b, UnityEngine.AI.NavMesh.AllAreas, forwardNavMeshPath))
        {
            path = new FullPathResult { paths = new List<Vector3[]> { forwardNavMeshPath.corners } };
            return false;
        }

        if (forwardNavMeshPath.status == NavMeshPathStatus.PathInvalid)
        {
            path = new FullPathResult { paths = new List<Vector3[]> { forwardNavMeshPath.corners } };
            return false;
        }

        path = new FullPathResult { paths = new List<Vector3[]> { forwardNavMeshPath.corners } };


        if (forwardNavMeshPath.status == NavMeshPathStatus.PathComplete)
        {
            return true;
        }


        //try reverse path
        var reverseNavMeshPath = new NavMeshPath();
        if (!UnityEngine.AI.NavMesh.CalculatePath(b, a, UnityEngine.AI.NavMesh.AllAreas, reverseNavMeshPath))
        {
            throw new Exception("Unexpected failure calculating reverse path");
        }

        if (reverseNavMeshPath.status == NavMeshPathStatus.PathInvalid)
        {
            throw new Exception("Unexpected invalid reverse path");
        }

        path.paths.Add(reverseNavMeshPath.corners.Reverse().ToArray());
        return true;
    }
    */


    private static bool NavDistance(Vector3 a, Vector3 b, out float distance)
    {
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path))
        {
            distance = float.MaxValue;
            return false;
        }

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            distance = PathLength(path.corners);
            return true;
        }


        distance = float.MaxValue;
        return false;
    }


    private bool TraversalRatio(Vector3 a, Vector3 b, out float ratio)
    {
        var straightLineDistance = Vector3.Distance(a, b);
        if (!NavDistance(a, b, out var pathDistance))
        {
            ratio = 0;
            return false;
        }

        ratio = pathDistance / straightLineDistance;
        return true;
    }

    public static float FullPathLength(FullPathResult fullPath)
    {
        return fullPath.paths.Sum(PathLength);
    }

    private static float PathLength(Vector3[] path)
    {
        if (path.Length < 2)
        {
            return 0f;
        }

        var length = 0f;
        for (var i = 1; i < path.Length; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }

        return length;
    }

    public struct FullPathResult
    {
        public List<Vector3[]> paths;
    }
}