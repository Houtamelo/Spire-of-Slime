using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.HexagonObject;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;
using static Core.Local_Map.Scripts.Coordinates.PathUtils;
using Core.Utils.Collections.Extensions;

namespace Core.Local_Map.Scripts.PathCreating
{
    public static class PathCreator
    {
        public static bool CreatePath(Dictionary<Axial, (Cell cell, float weight)> map, Cell start, [NotNull] Cell end, int maxLength, [CanBeNull] out List<Cell> result, [NotNull] params Cell[] intermediary)
        {
            int manhattanDistance = 0;
            {
                Cell first = start;
                foreach (Cell inter in intermediary)
                {
                    manhattanDistance += ManhattanDistance(first, inter);
                    first = inter;
                }

                manhattanDistance += ManhattanDistance(first, end);
            }

            maxLength = Mathf.Max(maxLength, (int)(manhattanDistance * 1.5f));
            
            using CustomValuePooledList<Cell> inters = new(capacity: 32);
            
            using (CustomValuePooledList<Cell> pivot = new(intermediary, CustomValuePooledList<Cell>.SourceType.Copy))
            {
                Cell current = start;
                while (pivot.Count > 0)
                {
                    Cell best = pivot[0];
                    int bestDistance = int.MaxValue;
                    foreach (Cell tile in pivot)
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

            List<Cell> finalPath = new() { start };

            int retries = 0;
            Cell currentStart = start;
            for (int i = 0; i <= inters.Count; i++)
            {
                Cell currentEnd = i == inters.Count ? end : inters[i];
                if (AStarPathFinding.TryFindPath(start: currentStart, goal: currentEnd, map, finalPath: out List<Cell> path) == false || path.Count > maxLength)
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
                Cell current = finalPath.TakeFirst();
                List<Cell> pivotPath = new() { current };
                while (finalPath.Count > 0)
                {
                    for (int j = finalPath.Count - 1; j >= 0; j--)
                    {
                        Cell iterated = finalPath[index: j];
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