using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValveFileImporter.Data;
using ValveFileImporter.NavMeshDisplay;
using ValveFileImporter.ValveResourceFormat.KeyValues;
using ValveFileImporter.ValveResourceFormat.Resource;

namespace ValveFileImporter.NavFlowMap
{
    public class NavFlowMapSO : ScriptableObject
    {
        public FlowMapNode[] nodes;

        public static NavFlowMapSO GetNavFlowMap(string path)
        {
            var binaryKV3 = new BinaryKV3(path);
            var root = binaryKV3.Data.Properties;
            var hulls = root["hulls"];
            var hullArray = hulls.Value as KVObject;
            var firstHull = hullArray[0].Value as KVObject;
            var allNodes = firstHull.GetArray<KVObject>("nodes");
            var nodes = new List<FlowMapNode>();
            var sourceToUnityMatrix = NavMeshImporter.sourceToUnityMatrix;
            foreach (var node in allNodes)
            {
                var i = GetIntFromKV(node, "i");

                var center = GetArrayFromKv<float>(node, "center");

                var centerVec = sourceToUnityMatrix.MultiplyPoint(new Vector3(center[0], center[1], center[2]));

                var navIds = GetArrayFromKv<int>(node, "nav_ids");

                var flow_map = GetArrayFromKv<string>(node, "flow_map", true);


                var connections = new List<FlowMapConnection>();


                var connectionsArray = GetArrayFromKv<KVObject>(node, "connections");

                foreach (var conn in connectionsArray)
                {
                    var cost = conn.GetProperty<double>("cost");
                    var node_index = GetIntFromKV(conn, "node_index");
                    var nav_id = GetIntFromKV(conn, "nav_id");
                    connections.Add(new FlowMapConnection
                    {
                        cost = (float)cost,
                        node_index = node_index,
                        nav_id = nav_id
                    });
                }

                nodes.Add(new FlowMapNode
                {
                    i = i,
                    center = centerVec,
                    nav_ids = navIds,
                    connections = connections.ToArray(),
                    flow_map = flow_map
                });
            }

            var so = CreateInstance<NavFlowMapSO>();
            so.nodes = nodes.ToArray();
            return so;
        }

        private static int GetIntFromKV(KVObject kvObject, string key)
        {
            var kvValue = kvObject.Properties[key];
            if (kvValue is { Value: short intValue16 })
            {
                return intValue16;
            }

            if (kvValue is { Value: int intValue32 })
            {
                return intValue32;
            }

            if (kvValue is { Value: long intValue64 })
            {
                if (intValue64 > int.MaxValue)
                {
                    Debug.LogWarning($"Value for key {key} exceeds int.MaxValue, clamping to int.MaxValue");
                }

                return intValue64 > int.MaxValue ? int.MaxValue : (int)intValue64;
            }


            throw new Exception($"Unable to convert KVValue of type {kvValue.Type} to int");
        }


        private static T[] GetArrayFromKv<T>(KVObject kvObject, string key, bool isbitFlags = false)
        {
            var arrayValue = kvObject.Properties[key];

            if (arrayValue.Type == KVValueType.Null)
            {
                return Array.Empty<T>();
            }

            if (arrayValue.Value is not KVObject array)
            {
                throw new Exception($"Expected KVObject for key {key}, got {arrayValue.Value?.GetType()}");
            }


            var kvValues = array.Properties.Values.ToArray();
            var list = new List<T>();
            foreach (var kvValue in kvValues)
            {
                if (kvValue.Type == KVValueType.Null)
                {
                    continue;
                }

                if (isbitFlags)
                {
                    if (kvValue.Value is long l && typeof(T) == typeof(string))
                    {
                        list.Add((T)(object)Convert.ToString(l, 2));
                        continue;
                    }

                    if (kvValue.Value is short s && typeof(T) == typeof(string))
                    {
                        list.Add((T)(object)Convert.ToString(s, 2));
                        continue;
                    }

                    if (kvValue.Value is int i && typeof(T) == typeof(string))
                    {
                        list.Add((T)(object)Convert.ToString(i, 2));
                    }
                }
                else
                {
                    if (kvValue.Value is T value)
                    {
                        list.Add(value);
                    }
                }
            }

            return list.ToArray();
        }
    }

    [Serializable]
    public struct FlowMapNode : IEquatable<FlowMapNode>, INavMeshGizmoProvider
    {
        public int i;
        public Vector3 center;

        /// <summary>
        ///     Areas within this node
        /// </summary>
        public int[] nav_ids;

        public FlowMapConnection[] connections;

        /// <summary>
        ///     Tied to connections,
        ///     First value is always 111100000000000 (16 chars)
        ///     0 connections and it will only have that value
        ///     more to do with neighbor nodes than current nodes, one node has 4 areas and 1 connections but a 30 element flow map
        /// </summary>
        public string[] flow_map;

        public bool Equals(FlowMapNode other)
        {
            return i == other.i && center.Equals(other.center) && Equals(nav_ids, other.nav_ids) && Equals(connections, other.connections) &&
                   Equals(flow_map, other.flow_map);
        }

        public override bool Equals(object obj)
        {
            return obj is FlowMapNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(i, center, nav_ids, connections, flow_map);
        }

        public void DrawGizmo(SourceNavMeshDisplay navMeshDisplay)
        {
            foreach (var navID in nav_ids)
            {
                var area = navMeshDisplay.GetArea(navID);
                area.DrawGizmo(navMeshDisplay);
            }
        }
    }

    [Serializable]
    public struct FlowMapConnection
    {
        public float cost;

        /// <summary>
        ///     Index of the node this connection leads to
        /// </summary>
        public int node_index;

        /// <summary>
        ///     Nav area ID this connection leads to within the other node
        /// </summary>
        public int nav_id;
    }
}