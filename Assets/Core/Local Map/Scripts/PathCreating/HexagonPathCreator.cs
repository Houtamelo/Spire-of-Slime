using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Coordinates;
using UnityEngine;
using UnityEngine.Pool;
using Utils.Extensions;
using static Core.Local_Map.Scripts.Coordinates.PathUtils;

namespace Core.Local_Map.Scripts.PathCreating
{
    public static class PathCreator
    {
        public static bool CreatePath(IReadOnlyDictionary<Axial, (HexagonObject.Cell cell, float weight)> map, HexagonObject.Cell start, HexagonObject.Cell end, int maxLength, out List<HexagonObject.Cell> result, params HexagonObject.Cell[] intermediary)
        {
            int manhattanDistance = 0;
            {
                HexagonObject.Cell first = start;
                foreach (HexagonObject.Cell inter in intermediary)
                {
                    manhattanDistance += ManhattanDistance(first, inter);
                    first = inter;
                }

                manhattanDistance += ManhattanDistance(first, end);
            }

            maxLength = Mathf.Max(maxLength, (int)(manhattanDistance * 1.5f));
            using PooledObject<List<HexagonObject.Cell>> interPool = ListPool<HexagonObject.Cell>.Get(out List<HexagonObject.Cell> inters);
            {
                using PooledObject<List<HexagonObject.Cell>> pivotPool = ListPool<HexagonObject.Cell>.Get(out List<HexagonObject.Cell> pivot);
                pivot.Add(intermediary);
                HexagonObject.Cell current = start;
                while (pivot.Count > 0)
                {
                    HexagonObject.Cell best = pivot[0];
                    int bestDistance = int.MaxValue;
                    foreach (HexagonObject.Cell tile in pivot)
                    {
                        int distance = ManhattanDistance(tile, current);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = tile;
                        }
                    }

                    current = best;
                    pivot.Remove(best);
                    inters.Add(best);
                }
            }

            List<HexagonObject.Cell> finalPath = new() { start };

            int retries = 0;
            HexagonObject.Cell currentStart = start;
            for (int i = 0; i <= inters.Count; i++)
            {
                HexagonObject.Cell currentEnd = i == inters.Count ? end : inters[index: i];
                if (AStarPathFinding.TryFindPath(start: currentStart, goal: currentEnd, map, finalPath: out List<HexagonObject.Cell> path) == false || path.Count > maxLength)
                {
                    using PooledObject<List<Axial>> pool = GetDirectPathBetween(left: currentStart.position, right: currentEnd.position, path: out List<Axial> directPath);
                    Debug.Assert(condition: directPath.All(predicate: map.ContainsKey));
                    do
                    {
                        retries++;
                        maxLength++;
                        if (retries > 15)
                        {
                            result = null;
                            return false;
                        }
                    } while (AStarPathFinding.TryFindPath(start: currentStart, goal: currentEnd, map, finalPath: out path) == false || path.Count > maxLength);
                }

                finalPath.Add(path);
                currentStart = currentEnd;
            }

            {
                HexagonObject.Cell current = finalPath.TakeFirst();
                List<HexagonObject.Cell> pivotPath = new() { current };
                while (finalPath.Count > 0)
                {
                    for (int j = finalPath.Count - 1; j >= 0; j--)
                    {
                        HexagonObject.Cell iterated = finalPath[index: j];
                        if (iterated.IsNeighbor(current) == false)
                            continue;
                        
                        current = iterated;
                        pivotPath.Add(current);
                        finalPath.RemoveRange(index: 0, count: j + 1);
                        break;
                    }

                }

                finalPath = pivotPath;
            }

            finalPath.Remove(end);
            result = finalPath;
            return true;
        }
    }
}