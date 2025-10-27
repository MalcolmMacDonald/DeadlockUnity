using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ValveFileImporter.Data;
using ValveFileImporter.Data.Structs;

namespace ValveFileImporter.NavMeshDisplay
{
    public partial class SourceNavMeshDisplay
    {
        private readonly Dictionary<int, List<AreaConnection>> _neighborsMap = new();
        private Dictionary<int, int> _areaIDMap = new();


        private void SetupData()
        {
            _areaIDMap = navMeshData.Areas
                .Select((area, index) => (area, index))
                .ToDictionary(pair => pair.area.AreaId, pair => pair.index);


            static void AddNeighbor<T>(Dictionary<int, List<T>> map, int from, T to)
            {
                if (!map.ContainsKey(from))
                {
                    map[from] = new List<T>();
                }

                if (!map[from].Contains(to))
                {
                    map[from].Add(to);
                }
            }


            _neighborsMap.Clear();
            foreach (var area in navMeshData.Areas)
            {
                foreach (var areaConnection in GetNeighboringConnections(area))
                {
                    AddNeighbor(_neighborsMap, area.AreaId, areaConnection);
                    AddNeighbor(_neighborsMap, areaConnection.targetAreaId, areaConnection.Reverse());
                }
            }
        }

        private IEnumerable<AreaConnection> GetNeighboringConnections(NavMeshArea area)
        {
            var connectedAreas = new HashSet<NavMeshArea>();
            foreach (var connection in area.Connections)
            {
                var connectedArea = GetArea(connection.ConnectedAreaId);
                if (connectedAreas.Add(connectedArea))
                {
                    yield return new AreaConnection
                    {
                        sourceAreaId = area.AreaId,
                        targetAreaId = connectedArea.AreaId,
                        sourceEdgeIndex = connection.ThisEdgeIndex,
                        targetEdgeIndex = connection.OtherEdgeIndex,
                        cost = CalculateCostBetweenAreas(area, connectedArea)
                    };
                }
            }
        }

        public IEnumerable<AreaConnection> GetCachedAreaNeighbors(int areaId)
        {
            if (_neighborsMap.TryGetValue(areaId, out var connections))
            {
                foreach (var connection in connections)
                {
                    yield return connection;
                }
            }
        }

        public IEnumerable<AreaCorner> GetNeighboringCorners(NavMeshArea area)
        {
            return area.Connections
                .SelectMany(connection => GetArea(connection.ConnectedAreaId).GetCorners());
        }


        //aggressive inline
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NavMeshArea GetArea(int areaId)
        {
            return navMeshData.Areas[_areaIDMap[areaId]];
        }
    }
}