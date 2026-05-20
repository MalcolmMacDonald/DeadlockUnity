using System.Collections.Generic;
using UnityEngine;

public partial class DoorDistanceMapper
{
    private static void DrawEdgePath(NavMeshEdgePath edgePath, Color color)
    {
        if (edgePath.vertices.Count < 2)
        {
            return;
        }

        for (var i = 1; i < edgePath.vertices.Count; i++)
        {
            Debug.DrawLine(edgePath.vertices[i - 1], edgePath.vertices[i], color, 10f);

            var direction = (edgePath.vertices[i - 1] - edgePath.vertices[i]).normalized;
            var rotatedLeft = Quaternion.Euler(0, 90, 0) * direction;
            var midPoint = (edgePath.vertices[i - 1] + edgePath.vertices[i]) / 2;
            Debug.DrawRay(midPoint, rotatedLeft * 0.2f, color, 10f);
        }
    }

    public List<NavMeshEdgePath> AllExteriorEdges()
    {
        navMeshSurface.enabled = true;
        navMeshSurface.BuildNavMesh();
        var triangulatedMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();

        var vertices = triangulatedMesh.vertices;
        var indices = triangulatedMesh.indices;

        // Dictionary to count edge occurrences
        var edgeCount = new Dictionary<(int, int), int>();

        var vertexToIndex = new Dictionary<Vector3, int>();


        int GetOrAddVertex(int index)
        {
            var vertex = vertices[index];
            if (!vertexToIndex.TryGetValue(vertex, out var newIndex))
            {
                newIndex = index;
                vertexToIndex[vertex] = newIndex;
            }

            return newIndex;
        }

        // Iterate through triangles and count edges
        for (var i = 0; i < indices.Length; i += 3)
        {
            var v0 = indices[i];
            var v1 = indices[i + 1];
            var v2 = indices[i + 2];
            v0 = GetOrAddVertex(v0);
            v1 = GetOrAddVertex(v1);
            v2 = GetOrAddVertex(v2);


            var edges = new[]
            {
                (Mathf.Min(v0, v1), Mathf.Max(v0, v1)),
                (Mathf.Min(v1, v2), Mathf.Max(v1, v2)),
                (Mathf.Min(v2, v0), Mathf.Max(v2, v0))
            };

            foreach (var edge in edges)
            {
                if (!edgeCount.TryAdd(edge, 1))
                {
                    edgeCount[edge]++;
                }
            }
        }

        var bounds = new Bounds
        {
            center = navMeshSurface.center + navMeshSurface.transform.position,
            size = navMeshSurface.size
        };
        bounds.Expand(-0.001f);


        // Collect exterior edges (those that appear only once)
        var exteriorEdges = new List<NavMeshExteriorEdge>();
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1) // Edge is on the exterior
            {
                var edge = kvp.Key;
                if (!bounds.Contains(vertices[edge.Item1]) &&
                    !bounds.Contains(vertices[edge.Item2]))
                {
                    continue;
                }

                exteriorEdges.Add(new NavMeshExteriorEdge
                {
                    v0 = vertices[edge.Item1],
                    i0 = edge.Item1,
                    v1 = vertices[edge.Item2],
                    i1 = edge.Item2
                });
            }
        }


        //breadth first search to connect edges into paths
        var edgePaths = new List<NavMeshEdgePath>();
        while (exteriorEdges.Count > 0)
        {
            var currentEdge = exteriorEdges[0];
            exteriorEdges.RemoveAt(0);

            var edgePath = new NavMeshEdgePath
            {
                vertices = new List<Vector3> { currentEdge.v0, currentEdge.v1 },
                indices = new List<int> { currentEdge.i0, currentEdge.i1 }
            };
            var addedEdge = true;

            while (addedEdge)
            {
                addedEdge = false;
                for (var i = exteriorEdges.Count - 1; i >= 0; i--)
                {
                    var edge = exteriorEdges[i];
                    if (edgePath.AddEdge(edge))
                    {
                        exteriorEdges.RemoveAt(i);
                        addedEdge = true;
                    }
                }
            }

            edgePaths.Add(edgePath);
        }

        return edgePaths;
    }

    private List<EdgePoint> PlacePointsAlongPath(NavMeshEdgePath path, float spacing)
    {
        var points = new List<EdgePoint>();
        if (path.vertices.Count < 2)
        {
            return points;
        }

        Quaternion GetWallDirection(Vector3 a, Vector3 b)
        {
            var direction = (b - a).normalized;
            return Quaternion.LookRotation(Quaternion.Euler(0, -90, 0) * direction);
        }

        points.Add(new EdgePoint
        {
            position = path.vertices[0],
            wallDirection = GetWallDirection(path.vertices[0], path.vertices[1])
        });
        var accumulatedDistance = 0f;
        for (var i = 1; i < path.vertices.Count; i++)
        {
            var segmentStart = path.vertices[i - 1];
            var segmentEnd = path.vertices[i];
            var segmentDirection = (segmentEnd - segmentStart).normalized;
            var segmentLength = Vector3.Distance(segmentStart, segmentEnd);

            while (accumulatedDistance + spacing <= segmentLength)
            {
                accumulatedDistance += spacing;
                var newPoint = segmentStart + segmentDirection * accumulatedDistance;
                points.Add(new EdgePoint
                {
                    position = newPoint,
                    wallDirection = GetWallDirection(segmentStart, segmentEnd)
                });
            }

            accumulatedDistance -= segmentLength;
            if (accumulatedDistance < 0)
            {
                accumulatedDistance = 0;
            }
        }

        return points;
    }
}