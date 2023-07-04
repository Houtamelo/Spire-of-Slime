using System.Buffers;
using System.Collections.Generic;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.Enums;
using KGySoft.CoreLibraries;
using NetFabric.Hyperlinq;
using Priority_Queue;
using UnityEngine;

namespace Core.Local_Map.Scripts.PathCreating
{
    public static class AStarPathFinding
    {
        public static int Heuristic(HexagonObject.Cell a, HexagonObject.Cell b) => PathUtils.ManhattanDistance(a.position, b.position) * 5;

        public static bool TryFindPath(HexagonObject.Cell start, HexagonObject.Cell goal, IReadOnlyDictionary<Axial, (HexagonObject.Cell cell, float weight)> weightMap, out List<HexagonObject.Cell> finalPath)
        {
            Dictionary<HexagonObject.Cell, float> costSoFar = new();
            finalPath = new List<HexagonObject.Cell>();
            SimplePriorityQueue<HexagonObject.Cell> frontier = new();
            frontier.Enqueue(start, priority: 0);
            bool success = false;
            costSoFar[start] = 0;
            
            while (frontier.Count > 0)
            {
                HexagonObject.Cell current = frontier.Dequeue();
                if (current.Equals(goal))
                {
                    success = true;
                    break;
                }
                
                using Lease<KeyValuePair<Direction, HexagonObject.Cell>> adjacentTiles = current.Neighbors.AsValueEnumerable().ToArray(ArrayPool<KeyValuePair<Direction, HexagonObject.Cell>>.Shared);
                {
                    foreach ((Direction _, HexagonObject.Cell next) in adjacentTiles.Shuffle())
                    {
                        float weight = weightMap[next.position].weight;
                        float newCost = costSoFar[current] + weight;
                        if (costSoFar.ContainsKey(next) == false || newCost < costSoFar[next])
                        {
                            costSoFar[next] = newCost;
                            float priority = newCost + Heuristic(next, goal);
                            frontier.Enqueue(next, priority);
                        }
                    }
                }
            }

            if (!success)
                return false;
            
            finalPath.Add(goal);
            HexagonObject.Cell currentInPath = goal;
            while (currentInPath != start)
            {
                bool found = false;
                HexagonObject.Cell best = default;
                float smallestCost = float.MaxValue;
                foreach ((Direction _, HexagonObject.Cell tile) in currentInPath.Neighbors)
                {
                    if (costSoFar.TryGetValue(tile, out float cost) && cost < smallestCost)
                    {
                        smallestCost = cost;
                        best = tile;
                        found = true;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"Couldn't backtrack towards goal, something is wrong, start: {start.position.ToString()} | goal: {goal.position.ToString()}");
                    success = false;
                    break;
                }
                currentInPath = best;
                finalPath.Add(best);
            }

            finalPath.Reverse();
            return success;
        }
    }
}