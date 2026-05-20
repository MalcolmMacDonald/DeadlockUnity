using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValveFileImporter.Data;
using ValveFileImporter.Data.Structs;

namespace ValveFileImporter.NavMeshDisplay
{
    public partial class SourceNavMeshDisplay
    {
        private static NavMeshArea NearestArea(NavMeshDataSO navMeshDataSo, Vector3 position)
        {
            NavMeshArea? nearestArea = null;
            var nearestDistance = float.MaxValue;
            foreach (var area in navMeshDataSo.Areas)
            {
                var areaCenter = AreaCenter(area);
                var distance = Vector3.Distance(position, areaCenter);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestArea = area;
                }
            }

            return nearestArea!.Value;
        }


        private static float CalculateCostBetweenAreas(NavMeshArea fromArea, NavMeshArea toArea)
        {
            return Vector3.Distance(fromArea.Center, toArea.Center);
        }


        private static Vector3 AreaCenter(NavMeshArea area)
        {
            var center = Vector3.zero;
            foreach (var corner in area.Corners)
            {
                center += corner;
            }

            return center / area.Corners.Length;
        }

        public IEnumerable<AreaConnection> FindAreaPath(NavMeshArea start, NavMeshArea end, out HashSet<int> visited)
        {
            visited = new HashSet<int>();
            var priorityQueue = new AdvancedPriorityQueue<AreaConnection>();
            var cameFrom = new Dictionary<int, AreaConnection>();
            var costSoFar = new Dictionary<int, float>();

            priorityQueue.Enqueue(new AreaConnection { sourceAreaId = start.AreaId, targetAreaId = start.AreaId, cost = 0 }, 0);
            visited.Add(start.AreaId);
            costSoFar[start.AreaId] = 0;
            var stepCount = 0;

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();
                stepCount++;

                if (current.targetAreaId == end.AreaId)
                {
                    var path = new List<AreaConnection>();
                    var pathNode = current;
                    while (cameFrom.TryGetValue(pathNode.targetAreaId, out var previous))
                    {
                        path.Add(pathNode);
                        pathNode = previous;
                    }

                    path.Add(pathNode);
                    path.Reverse();
                    path.RemoveAt(0);

                    return path;
                }

                var currentArea = GetArea(current.targetAreaId);

                foreach (var areaConnection in GetCachedAreaNeighbors(currentArea.AreaId))
                {
                    if (visited.Contains(areaConnection.targetAreaId))
                    {
                        continue;
                    }

                    var neighborAreaId = areaConnection.targetAreaId;
                    var newCost = costSoFar[current.sourceAreaId] + areaConnection.cost;

                    if (!costSoFar.ContainsKey(neighborAreaId) || newCost < costSoFar[neighborAreaId])
                    {
                        costSoFar[neighborAreaId] = newCost;
                        cameFrom[neighborAreaId] = current;
                        var priority = newCost + Heuristic(currentArea, end);
                        priorityQueue.Enqueue(areaConnection, priority);
                        visited.Add(neighborAreaId);
                    }
                }
            }

            return Enumerable.Empty<AreaConnection>();
        }

        public IEnumerable<AreaCorner> FindPathThetaStar(Vector3 startPos, Vector3 endPos, out HashSet<AreaCorner> visitedCorners)
        {
            var costSoFar = new Dictionary<AreaCorner, float>();
            var cameFrom = new Dictionary<AreaCorner, AreaCorner>();
            var startCorner = GetNearestCorner(startPos);
            var endCorner = GetNearestCorner(endPos);
            var visible = new Dictionary<(AreaCorner, AreaCorner), bool>();

            bool IsVisible(AreaCorner fromCorner, AreaCorner toCorner)
            {
                if (visible.TryGetValue((fromCorner, toCorner), out var isVisible) || visible.TryGetValue((toCorner, fromCorner), out isVisible))
                {
                    return isVisible;
                }

                isVisible = AreaCornerExtensions.IsVisible(fromCorner, toCorner, out _);
                visible[(fromCorner, toCorner)] = isVisible;
                return isVisible;
            }

            float Score(AreaCorner corner)
            {
                var g = costSoFar.GetValueOrDefault(corner, 0);
                var h = Vector3.Distance(corner.position, endCorner.position);
                return g + h;
            }


            var priorityQueue = new AdvancedPriorityQueue<AreaCorner>();
            priorityQueue.Enqueue(startCorner, Score(startCorner));
            costSoFar[startCorner] = 0f;

            void UpdateCorner(AreaCorner originalCorner, AreaCorner newCorner)
            {
                if (!cameFrom.TryGetValue(originalCorner, out var parent))
                {
                    return;
                }


                if (IsVisible(parent, newCorner))
                {
                    var newCost = costSoFar[parent] + Vector3.Distance(parent.position, newCorner.position);
                    if (!costSoFar.ContainsKey(newCorner) || newCost < costSoFar[newCorner])
                    {
                        costSoFar[newCorner] = newCost;
                        cameFrom[newCorner] = parent;
                        var priority = Score(newCorner);
                        priorityQueue.Enqueue(newCorner, priority);
                    }
                }
                else
                {
                    var newCost = costSoFar[originalCorner] + Vector3.Distance(originalCorner.position, newCorner.position);
                    if (!costSoFar.ContainsKey(newCorner) || newCost < costSoFar[newCorner])
                    {
                        costSoFar[newCorner] = newCost;
                        cameFrom[newCorner] = originalCorner;
                        var priority = Score(newCorner);
                        priorityQueue.Enqueue(newCorner, priority);
                    }
                }
            }

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();
                var currentArea = GetArea(current.areaId);
                if (current.Equals(endCorner))
                {
                    visitedCorners = new HashSet<AreaCorner>(cameFrom.Keys);
                    return ReconstructPath(cameFrom, current);
                }

                foreach (var neighbor in GetNeighboringCorners(currentArea))
                {
                    var newCost = costSoFar[current] + Vector3.Distance(current.position, neighbor.position);

                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        cameFrom[neighbor] = current;
                        var priority = Score(neighbor);
                        priorityQueue.Enqueue(neighbor, priority);
                    }

                    UpdateCorner(current, neighbor);
                }
            }

            visitedCorners = new HashSet<AreaCorner>(cameFrom.Keys);
            return Enumerable.Empty<AreaCorner>();
        }

        private IEnumerable<T> ReconstructPath<T>(Dictionary<T, T> cameFrom, T current)
        {
            var path = new List<T> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private AreaCorner GetNearestCorner(Vector3 position)
        {
            var nearestArea = NearestArea(navMeshData, position);
            return nearestArea.GetCorners().OrderBy(c => Vector3.Distance(c.position, position)).First();
        }

        private float Heuristic(NavMeshArea a, NavMeshArea b)
        {
            var centerA = AreaCenter(a);
            var centerB = AreaCenter(b);
            return Vector3.Distance(centerA, centerB);
        }

        public float PathLength(IEnumerable<AreaCorner> path)
        {
            var length = 0f;
            AreaCorner? previousCorner = null;
            foreach (var corner in path)
            {
                if (previousCorner != null)
                {
                    length += Vector3.Distance(previousCorner.Value.position, corner.position);
                }

                previousCorner = corner;
            }

            return length;
        }

        public float PathLength(IEnumerable<AreaConnection> path)
        {
            return path.Sum(c => c.cost);
        }
    }
}