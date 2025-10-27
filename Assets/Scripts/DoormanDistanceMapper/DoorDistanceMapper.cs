using System.Collections.Generic;
using EasyButtons;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public partial class DoorDistanceMapper : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [SerializeField] private NavMeshSurface navMeshSurface;

    [SerializeField] private float pointSpacing = 1f;

    [SerializeField] private float doorWidth;
    [SerializeField] private float doorHeight;
    [SerializeField] private float doorDepth;
    [SerializeField] private float doorWallOffset = 0.1f;

    [SerializeField] private float testHeight;
    [SerializeField] private float testZOffset;
    [SerializeField] private float testDepth = 0.1f;

    private void OnDrawGizmos()
    {
        if (pointA.gameObject.activeInHierarchy)
        {
            DrawValidatedDoor(new EdgePoint { position = pointA.position, wallDirection = pointA.rotation });
        }

        if (pointB.gameObject.activeInHierarchy)
        {
            DrawValidatedDoor(new EdgePoint { position = pointB.position, wallDirection = pointB.rotation });
        }

        if (pointA.gameObject.activeInHierarchy && pointB.gameObject.activeInHierarchy)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(pointA.position, pointB.position,
                NavMesh.AllAreas, path);
            DrawPath(path.corners, Color.magenta);
            //draw path

            /*var path = new FullPathResult();
            if ((pointB.position, pointA.position, out path))
            {
                DrawFullPath(path, Color.green);
            }*/
        }
    }


    [Button]
    public void DrawExteriorEdges()
    {
        var edgePaths = AllExteriorEdges();
        if (!GetPointOnNavMesh(transform.position, out var navMeshSourcePoint))
        {
            Debug.LogWarning("Could not find point on navmesh");
            return;
        }


        var allValidDoorPositions = new List<EdgePoint>();

        for (var i = 0; i < edgePaths.Count; i++)
        {
            var edgePath = edgePaths[i];
            var color = Color.HSVToRGB(i / (float)edgePaths.Count, 1f, 1f);
            var points = PlacePointsAlongPath(edgePath, pointSpacing);
            for (var j = 0; j < points.Count; j++)
            {
                var doorPoint = points[j];
                if (!TestDoorPlacement(doorPoint, out var doorPosition))
                {
                    doorPoint.wallDirection = Quaternion.Euler(0, 180, 0) * doorPoint.wallDirection;
                    if (!TestDoorPlacement(doorPoint, out doorPosition))
                    {
                        continue;
                    }
                }

                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(navMeshSourcePoint, doorPoint.position, NavMesh.AllAreas, path))
                {
                    continue;
                }

                if (path.status == NavMeshPathStatus.PathInvalid)
                {
                    continue;
                }


                //DrawPath(path, color);

                allValidDoorPositions.Add(doorPoint);
                Debug.DrawRay(doorPoint.position, Vector3.up * 0.2f, color, 10f);
                Debug.DrawRay(doorPoint.position, doorPoint.wallDirection * Vector3.forward * 0.2f, color, 10f);
            }
        }

        var maximumRatios = new Dictionary<int, int>();

        var globalMaximumRatio = 0f;
        var globalA = -1;
        var globalB = -1;
        //get all pairs of distances between valid door positions
        for (var i = 0; i < allValidDoorPositions.Count; i++)
        {
            var maximumRatio = 0f;
            for (var j = i + 1; j < allValidDoorPositions.Count; j++)
            {
                var posA = allValidDoorPositions[i].position;
                var posB = allValidDoorPositions[j].position;
                if (!TraversalRatio(posA, posB, out var ratio))
                {
                    continue;
                }

                if (ratio > maximumRatio)
                {
                    maximumRatio = ratio;
                    maximumRatios[i] = j;
                }

                if (ratio > globalMaximumRatio)
                {
                    globalMaximumRatio = ratio;
                    globalA = i;
                    globalB = j;
                }
            }
        }


        if (globalA != -1 && globalB != -1)
        {
            pointA.position = allValidDoorPositions[globalA].position;
            pointB.position = allValidDoorPositions[globalB].position;
            pointA.rotation = allValidDoorPositions[globalA].wallDirection;
            pointB.rotation = allValidDoorPositions[globalB].wallDirection;

            pointA.gameObject.SetActive(true);
            pointB.gameObject.SetActive(true);


            //alert the user that the calculation is complete by making the unity editor flash
            //EditorApplication.Beep();
            /*var path = new FullPathResult();
            GetFullPath(allValidDoorPositions[globalA].position, allValidDoorPositions[globalB].position,
                out path);
            DrawFullPath(path, Color.magenta);*/
        }
    }


    private struct DoorPosition
    {
        public Vector3 center;
        public Quaternion rotation;
    }

    private struct EdgePoint
    {
        public Vector3 position;
        public Quaternion wallDirection;
    }

    public struct NavMeshExteriorEdge
    {
        public Vector3 v0;
        public int i0;
        public Vector3 v1;
        public int i1;
    }


    public struct NavMeshEdgePath
    {
        public List<Vector3> vertices;
        public List<int> indices;
        public int StartIndex => indices[0];
        public int EndIndex => indices[^1];

        public bool AddEdge(NavMeshExteriorEdge edge)
        {
            if (edge.i0 == EndIndex)
            {
                vertices.Add(edge.v1);
                indices.Add(edge.i1);
                return true;
            }

            if (edge.i1 == EndIndex)
            {
                vertices.Add(edge.v0);
                indices.Add(edge.i0);
                return true;
            }

            if (edge.i0 == StartIndex)
            {
                vertices.Insert(0, edge.v1);
                indices.Insert(0, edge.i1);
                return true;
            }

            if (edge.i1 == StartIndex)
            {
                vertices.Insert(0, edge.v0);
                indices.Insert(0, edge.i0);
                return true;
            }

            return false;
        }
    }
}