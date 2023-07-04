using System;
using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Enums;
using Core.Local_Map.Scripts.HexagonObject;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using UnityEngine.Pool;
using Utils.Collections;
using Utils.Extensions;
using Random = System.Random;

namespace Core.Local_Map.Scripts.Coordinates
{
    public static class PathUtils
    {
        private static readonly Random Random = new();
        private static readonly IReadOnlyList<Direction> ClockWiseDirections = new []
        {
            Direction.NorthEast,
            Direction.East,
            Direction.SouthEast,
            Direction.SouthWest,
            Direction.West,
            Direction.NorthWest,
        };

        public static int ManhattanDistance(Axial left, Axial right)
        {
            Axial vec = left - right;
            int distance = (Mathf.Abs(value: vec.q) + Mathf.Abs(value: vec.q + vec.r) + Mathf.Abs(value: vec.r)) / 2;
            return distance;
        }
        
        public static int ManhattanDistance(Cell left, Cell right)
        {
            Axial vec = left.position - right.position;
            int distance = (Mathf.Abs(value: vec.q) + Mathf.Abs(value: vec.q + vec.r) + Mathf.Abs(value: vec.r)) / 2;
            return distance;
        }

        public static PooledObject<HashSet<Axial>> GetNeighbors(Axial currentPosition, int maxDistance, out HashSet<Axial> neighbors)
        {
            PooledObject<HashSet<Axial>> pool = HashSetPool<Axial>.Get(out neighbors);
            for (int q = -maxDistance; q <= maxDistance; q++)
            {
                for (int r = Math.Max(val1: -maxDistance, val2: -q - maxDistance); r <= Math.Min(val1: maxDistance, val2: -q + maxDistance); r++)
                    neighbors.Add(currentPosition + new Axial(q1: q, r1: r));
            }

            neighbors.Remove(currentPosition);

            return pool;
        }
        
        // I use ref in these to make it clear that the collection passed is being edited

        #region Mapping

        private static void RemoveIfNeighbor(ref HashSet<Axial> collection, Axial position)
        {
            foreach (Axial cube in collection.ToArray())
                if (cube.IsNeighborOrEqual(other: ref position))
                    collection.Remove(cube);
        }
        
        private static void RemoveIfNeighbor(ref HashSet<Axial> candidates, HashSet<Axial> excludingPositions)
        {
            foreach (Axial cube in excludingPositions)
                RemoveIfNeighbor(collection: ref candidates, position: cube);
        }

        private static void KeepClosestToTangent(ref HashSet<Axial> candidates, Axial tangent, Axial goal)
        {
            Dictionary<int, List<Axial>> dictionary = new();
            foreach (Axial candidate in candidates)
            {
                int distance = ManhattanDistance(candidate, goal);
                if (dictionary.TryGetValue(distance, out List<Axial> cubes))
                    cubes.Add(candidate);
                else
                    dictionary[key: distance] = new List<Axial> { candidate };
            }

            candidates.Clear();
            foreach ((int _, List<Axial> cubes) in dictionary.ToArray())
            {
                int minDistance = 9999;
                Axial best = cubes.First();
                foreach (Axial cube in cubes)
                {
                    int distance = ManhattanDistance(cube, tangent);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        best = cube;
                    }
                }

                candidates.Add(best);
            }
        }

        #endregion
        
        public static Axial PolarToAxialLength(this float polarAngleDeg, int length)
        {
            using PooledObject<HashSet<Axial>> pool = GetRing(center: Axial.Zero, radius: length, results: out HashSet<Axial> ring);
            float closestDistance = float.MaxValue;
            Axial bestMatch = ring.First();
            foreach (Axial cube in ring)
            {
                float candidate = cube.PolarAngleDeg();
                float distance = Mathf.Abs(Mathf.DeltaAngle(current: candidate, target: polarAngleDeg));
                if (distance < closestDistance)
                {
                    bestMatch = cube;
                    closestDistance = distance;
                }
            }
            
            return bestMatch;
        }

        public static Vector2Int RoundToAxial(float q, float r)
        {
            int qGrid = Mathf.RoundToInt(f: q);
            int rGrid = Mathf.RoundToInt(f: r);
            q -= qGrid;
            r -= rGrid;
            return Math.Abs(value: q) >= Math.Abs(value: r) ? new Vector2Int(x: qGrid + Mathf.RoundToInt(f: q + 0.5f * r), y: rGrid) :
                new Vector2Int(x: qGrid, y: rGrid + Mathf.RoundToInt(f: r + 0.5f * q));
        }

        public static Axial GetTangent(Axial begin, Axial end, float originalAngle, float curveDelta, float size)
        {
            float tangentAngle = originalAngle + curveDelta * 45;
            int distance = Mathf.CeilToInt(ManhattanDistance(begin, end) / (2 - Math.Abs(value: curveDelta) / 90));
            return tangentAngle.PolarToAxialLength(distance);
        }

        public static float DegreeAngleBetween(Axial left, Axial right, float size)
        {
            Vector3 leftPos = left.ToWorldCoordinates(size);
            Vector3 rightPos = right.ToWorldCoordinates(size);

            Vector3 direction = rightPos - leftPos;
            return Vector2.SignedAngle(from: Vector2.right, to: direction);
        }
        
        public static PooledObject<HashSet<Axial>> GetRing(Axial center, int radius, out HashSet<Axial> results)
        {
            PooledObject<HashSet<Axial>> pool = HashSetPool<Axial>.Get(out results);
            Axial hex = center + ToAxial(direction: Direction.NorthWest) * radius;
            results.Add(hex);
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < radius; j++)
                {
                    hex = GetNeighbor(center: hex, direction: (Direction) i);
                    results.Add(hex);
                }
            
            return pool;
        }

        /*public static PooledObject<List<Axial>> GetRingEnsuringValidDistance(Axial center, in int radius, in int index, IReadOnlyDictionary<int, Axial> dictionary, out HashSet<Axial> positions)
        {
            PooledObject<HashSet<Axial>> pool = GetRing(center, radius, out positions);
            for (int i = 0; i < positions.Count; i++)
            {
                Axial candidate = positions[i];
                foreach ((int key, Axial value) in dictionary)
                {
                    int start, end;
                    Axial current;

                    if (key > index)
                    {
                        start = index;
                        end = key;
                        current = candidate;
                    }
                    else
                    {
                        start = key;
                        end = index;
                        current = value;
                    }

                    int distance = 0;
                    for (int j = start; j < end; j++)
                    {
                        if (dictionary.TryGetValue(j, out Axial iterated))
                        {
                            distance += ManhattanDistance(iterated, current);
                            current = iterated;
                        }
                    }

                    int indexDistance = Math.Abs(key - index);
                    if ((distance > indexDistance || (indexDistance != 1 && value.IsNeighborOrEqual(candidate))) || 
                        (dictionary.TryGetValue(index + 1, out Axial above) && ManhattanDistance(above, candidate) != 1) ||
                        (dictionary.TryGetValue(index - 1, out Axial bellow) && ManhattanDistance(bellow, candidate) != 1))
                    {
                        positions.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            return pool;
        }*/

        public static Axial ToAxial(this Direction direction)
        {
            return direction switch
            {
                Direction.East => new Axial(q1: 1, r1: 0),
                Direction.NorthEast => new Axial(q1: 1, r1: -1),
                Direction.NorthWest => new Axial(q1: 0, r1: -1),
                Direction.West => new Axial(q1: -1, r1: 0),
                Direction.SouthWest => new Axial(q1: -1, r1: 1),
                Direction.SouthEast => new Axial(q1: 0, r1: 1),
                _ => throw new ArgumentOutOfRangeException(paramName: nameof(direction), actualValue: direction, message: null)
            };
        }

        public static Axial GetNeighbor(this Axial center, Direction direction) => center + ToAxial(direction: direction);

        public static Dictionary<int, Axial> NodesBetween(int left, int right, IReadOnlyDictionary<int, Axial> collectionToCheck)
        {
            Dictionary<int, Axial> dictionary = new();
            int lowest = Math.Min(val1: left, val2: right);
            int max = Math.Max(val1: left, val2: right);
            foreach ((int key, Axial value) in collectionToCheck)
                if (key >= lowest || key <= max)
                    dictionary[key: key] = value;

            return dictionary;
        }

        public static (Dictionary<int, Axial> path, float weight)[] WeighByAverageDistance(this List<Dictionary<int, Axial>> paths, Axial reference)
        {
            (Dictionary<int, Axial> path, float weight)[] weights = new (Dictionary<int, Axial> path, float weight)[paths.Count];
            
            for (int index = 0; index < paths.Count; index++)
            {
                Dictionary<int, Axial> tiles = paths[index: index];
                int distance = 0;
                foreach (KeyValuePair<int, Axial> pair in tiles)
                    distance += ManhattanDistance(pair.Value, reference);

                float weight = 1f / distance;
                weights[index] = (tiles, weight * weight);
            }

            return weights;
        }

        public static Dictionary<int, Axial> GetAverageCloserTo(this List<Dictionary<int, Axial>> paths, Axial reference)
        {
            Dictionary<int, Axial> best = paths[index: 0];
            int bestDistance = int.MaxValue;
            for (int index = 0; index < paths.Count; index++)
            {
                Dictionary<int, Axial> tiles = paths[index: index];
                int distance = 0;
                foreach (KeyValuePair<int, Axial> pair in tiles)
                    distance += ManhattanDistance(pair.Value, reference);

                if (distance < bestDistance)
                {
                    best = tiles;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public static void AvoidNeighboring(this List<Axial> positions, params Axial[] tilesToAvoid)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                Axial pos = positions[index: i];
                foreach (Axial avoid in tilesToAvoid)
                {
                    Axial a = avoid;
                    if (pos.IsNeighborOrEqual(other: ref a))
                    {
                        positions.RemoveAt(index: i);
                        i--;
                        break;
                    }
                }
            }
        }

        public static void AvoidNeighboring(this HashSet<Axial> positions, params Axial[] tilesToAvoid)
        {
            foreach (Axial pos in positions.ToArray())
            {
                foreach (Axial avoid in tilesToAvoid)
                {
                    Axial a = avoid;
                    if (pos.IsNeighborOrEqual(other: ref a))
                    {
                        positions.Remove(pos);
                        break;
                    }
                }
            }
        }

        public static NeighborEnumerator GetClosestNeighbors(this Axial center) => new(center);

        public static PooledObject<List<(Direction, Axial)>> GetClockWiseOrderedNeighbors(this Axial center, out List<(Direction, Axial)> neighbors)
        {
            PooledObject<List<(Direction, Axial)>> pool = UnityEngine.Pool.ListPool<(Direction, Axial)>.Get(out neighbors);
            neighbors.Add((Direction.NorthEast, center + ToAxial(direction: Direction.NorthEast)));
            neighbors.Add((Direction.East, center + ToAxial(direction: Direction.East)));
            neighbors.Add((Direction.SouthEast, center + ToAxial(direction: Direction.SouthEast)));
            neighbors.Add((Direction.SouthWest, center + ToAxial(direction: Direction.SouthWest)));
            neighbors.Add((Direction.West, center + ToAxial(direction: Direction.West)));
            neighbors.Add((Direction.NorthWest, center + ToAxial(direction: Direction.NorthWest)));

            return pool;
        }

        public static int[] GetPickOrder(int begin, int end)
        {
            using PooledObject<List<int>> pool1 = UnityEngine.Pool.ListPool<int>.Get(out List<int> range);
            if (begin < end)
                for (int i = begin; i <= end; i++)
                    range.Add(i);
            else if (begin > end)
                for (int i = begin; i >= end; i--)
                    range.Add(i);
            else
                range.Add(begin);
            
            int length = Mathf.Abs(value: begin - end);
            int previous = range.TakeAt(index: range.Count / 2);
            using PooledObject<List<int>> pool = UnityEngine.Pool.ListPool<int>.Get(out List<int> pickList);
            pickList.Add(previous);
            while (range.Count != 0)
            {
                int count = range.Count;
                float relativeLength = (float) count / length;
                int extracted = range.TakeAt(index: Mathf.Clamp(value: Mathf.FloorToInt((end - previous) * relativeLength), min: 0, max: count - 1));
                previous = extracted;
                pickList.Add(extracted);

                count = range.Count;
                if (count != 0)
                {
                    relativeLength = (float) count / length;
                    extracted = range.TakeAt(index: Mathf.Clamp(value: Mathf.FloorToInt((previous - begin) * relativeLength), min: 0, max: count - 1));
                    previous = extracted;
                    pickList.Add(extracted);
                }
            }

            return pickList.ToArray();
        }

        public static float GetAverageDistanceTo(this Dictionary<int, Axial> dictionary, Axial reference)
        {
            float distance = 0;
            foreach ((int _, Axial value) in dictionary) 
                distance += ManhattanDistance(value, reference);

            distance /= dictionary.Count;
            return distance;
        }
        
        public static (int, Axial) FindClosest(Dictionary<int, Axial> dictionary, int index)
        {
            Axial closest = default;
            int closestIndex = 0;
            int closestDistance = int.MaxValue;
            foreach ((int key, Axial value) in dictionary)
            {
                int distance = Math.Abs(value: index - key);
                if (distance < closestDistance)
                {
                    closest = value;
                    closestIndex = key;
                    closestDistance = distance;
                }
            }

            return (closestIndex, closest);
        }
        
        public static Dictionary<int, Axial> ExtractFirst(this SortedList<float, Dictionary<int, Axial>> list)
        {
            Dictionary<int, Axial> dic = list.Values[index: 0];
            list.RemoveAt(index: 0);
            return dic;
        }
        
        public static Dictionary<int, Axial> ExtractRandom(this SortedList<float, Dictionary<int, Axial>> list)
        {
            int index = Random.Next(minValue: 0, maxValue: list.Count);
            Dictionary<int, Axial> dic = list.Values[index: index];
            list.RemoveAt(index: index);
            return dic;
        }

        public static (Dictionary<int, Axial> path, float)[] WeighSorted(this SortedList<float, Dictionary<int, Axial>> list)
        {
            int count = list.Count;
            (Dictionary<int, Axial> path, float weight)[] weights = new (Dictionary<int, Axial> path, float weight)[count];
            for (int i = 0; i < count; i++)
            {
                float distance = list.Keys[index: i];
                weights[i] = (list.Values[index: i], distance * distance);
            }

            return weights;
        }

        public static Dictionary<Axial, Cell> GenerateTiles(this HashSet<Axial> positions, HexagonMap map)
        {
            Dictionary<Axial, Cell> cells = new(positions.Count);
            foreach (Axial position in positions)
                cells[position] = map.GetOrCreateCell(position);

            return cells;
        }

        public static PooledObject<List<Axial>> GetDirectPathBetween(Axial left, Axial right, out List<Axial> path)
        {
            PooledObject<List<Axial>> pool = UnityEngine.Pool.ListPool<Axial>.Get(out path);
            using PooledObject<List<Axial>> secondPool = GetDirectionCollection(left: left, right: right, dir: out List<Axial> directions);
            
            path.Add(left);

            int count = directions.Count;
            Axial current = left;
            for (int i = 0; i < count; i++)
            {
                current += directions.TakeRandom();
                path.Add(current);
            }
            
            return pool;
        }

        private static PooledObject<List<Axial>> GetDirectionCollection(Axial left, Axial right, out List<Axial> dir)
        {
            PooledObject<List<Axial>> directionsPool = UnityEngine.Pool.ListPool<Axial>.Get(out List<Axial> directions);
            Axial vector = right - left;
            Axial qr = new(q1: 1, r1: -1),
                qs = new(q1: 1, r1: 0),
                rq = new(q1: -1, r1: 1),
                rs = new(q1: 0, r1: 1),
                sq = new(q1: -1, r1: 0),
                sr = new(q1: 0, r1: -1);

            int q = vector.q, r = vector.r, s = vector.s;
            switch (q > 0, r > 0, s > 0)
            {
                case (true, true, false):
                    QsDir();
                    RsDir();
                    break;
                case (true, false, true):
                    QrDir();
                    SrDir();
                    break;
                case (false, true, true):
                    RqDir();
                    SqDir();
                    break;
                case (true, false, false):
                    QrDir();
                    QsDir();
                    break;
                case (false, true, false):
                    RqDir();
                    RsDir();
                    break;
                case (false, false, true):
                    SqDir();
                    SrDir();
                    break;
            }

            dir = directions;
            return directionsPool;
            
            void QsDir()
            {
                for (int i = q, j = s; i > 0 && j < 0; i--, j++)
                    directions.Add(qs);
            }

            void RsDir()
            {
                for (int i = r, j = s; i > 0 && j < 0; i--, j++)
                    directions.Add(rs);
            }

            void QrDir()
            {
                for (int i = q, j = r; i > 0 && j < 0; i--, j++)
                    directions.Add(qr);
            }

            void SrDir()
            {
                for (int i = s, j = r; i > 0 && j < 0; i--, j++)
                    directions.Add(sr);
            }

            void RqDir()
            {
                for (int i = r, j = q; i > 0 && j < 0; i--, j++)
                    directions.Add(rq);
            }

            void SqDir()
            {
                for (int i = s, j = q; i > 0 && j < 0; i--, j++)
                    directions.Add(sq);
            }
        }

        public static PooledObject<List<(Direction, int)>> GetDirectionCount(Axial left, Axial right,
            out List<(Direction, int)> results)
        {
            PooledObject<List<(Direction, int)>> pool = UnityEngine.Pool.ListPool<(Direction, int)>.Get(out List<(Direction, int)> directions);
            Axial vector = right - left;
            int q = vector.q, r = vector.r, s = vector.s;
            switch (q > 0, r > 0, s > 0)
            {
                case (true, true, false):
                    East();
                    SouthEast();
                    break;
                case (true, false, true):
                    NorthEast();
                    NorthWest();
                    break;
                case (false, true, true):
                    SouthWest();
                    West();
                    break;
                case (true, false, false):
                    NorthEast();
                    East();
                    break;
                case (false, true, false):
                    SouthWest();
                    SouthEast();
                    break;
                case (false, false, true):
                    West();
                    NorthWest();
                    break;
            }

            results = directions;
            return pool;
            
            void East()
            {
                int count = Mathf.Min(a: q, b: -s);
                directions.Add((Direction.East, count));
            }

            void SouthEast()
            {
                int count = Mathf.Min(a: r, b: -s);
                directions.Add((Direction.SouthEast, count));
            }

            void NorthEast()
            {
                int count = Mathf.Min(a: q, b: -r);
                directions.Add((Direction.NorthEast, count));
            }

            void NorthWest()
            {
                int count = Mathf.Min(a: s, b: -r);
                directions.Add((Direction.NorthWest, count));
            }

            void SouthWest()
            {
                int count = Mathf.Min(a: r, b: -q);
                directions.Add((Direction.SouthWest, count));
            }

            void West()
            {
                int count = Mathf.Min(a: s, b: -q);
                directions.Add((Direction.East, count));
            }
        }

        public static PooledObject<List<Axial>> GetParallelDirections(Axial left, Axial right, out List<Axial> results)
        {
            int referenceDistance = ManhattanDistance(left, right);
            PooledObject<List<Axial>> poolToReturn = UnityEngine.Pool.ListPool<Axial>.Get(out results);
            Axial east = ToAxial(direction: Direction.East);
            Axial southEast = ToAxial(direction: Direction.SouthEast);
            Axial northEast = ToAxial(direction: Direction.NorthEast);
            Axial west = ToAxial(direction: Direction.West);
            Axial southWest = ToAxial(direction: Direction.SouthWest);
            Axial northWest = ToAxial(direction: Direction.NorthWest);
            if (ManhattanDistance(east + left, right) == referenceDistance)
                results.Add(east);
            if (ManhattanDistance(southEast + left, right) == referenceDistance)
                results.Add(southEast);
            if (ManhattanDistance(northEast + left, right) == referenceDistance)
                results.Add(northEast);
            if (ManhattanDistance(west + left, right) == referenceDistance)
                results.Add(west);
            if (ManhattanDistance(southWest + left, right) == referenceDistance)
                results.Add(southWest);
            if (ManhattanDistance(northWest + left, right) == referenceDistance)
                results.Add(northWest);

            return poolToReturn;
        }

        public static PooledObject<HashSet<Axial>> GetInBetweenTiles(Axial left, Axial right, out HashSet<Axial> tiles)
        {
            PooledObject<HashSet<Axial>> pool = HashSetPool<Axial>.Get(out tiles);
            using PooledObject<List<(Direction, int)>> directionPool = GetDirectionCount(left: left, right: right, results: out List<(Direction, int)> directions);
            if (directions.Count == 1)
            {
                (Direction direction, int count) = directions[index: 0];
                for (int i = 1; i < count; i++)
                    tiles.Add(left + ToAxial(direction: direction) * i);
            }
            else
            {
                (Direction outsideDir, int outsideCount) = directions[index: 0];
                (Direction insideDir, int insideCount) = directions[index: 1];
                for (int i = 0; i < outsideCount; i++)
                    for (int j = 0; j < insideCount; j++)
                        tiles.Add(left + ToAxial(direction: outsideDir) * i + ToAxial(direction: insideDir) * j);

                tiles.Remove(right);
                tiles.Remove(left);
            }

            return pool;
        }
        
        private static readonly IReadOnlyDictionary<Direction, float> PolarAngleToCenter = new Dictionary<Direction, float>
        {
            { Direction.East, 0f },
            { Direction.NorthEast, 60f },
            { Direction.NorthWest, 120f },
            { Direction.West, 180f },
            { Direction.SouthWest, 240f },
            { Direction.SouthEast, 300f }
        };
        
        private static readonly IReadOnlyDictionary<Direction, Direction> OppositeDirections = new Dictionary<Direction, Direction>
        {
            { Direction.East, Direction.West },
            { Direction.NorthEast, Direction.SouthWest },
            { Direction.NorthWest, Direction.SouthEast },
            { Direction.West, Direction.East },
            { Direction.SouthWest, Direction.NorthEast },
            { Direction.SouthEast, Direction.NorthWest }
        };

        public static Direction PolarToDirection(this float polar)
        {
            polar = NormalizePolar(polar);
            return polar switch
            {
                < 30f  => Direction.East,
                < 90f  => Direction.NorthEast,
                < 150f => Direction.NorthWest,
                < 210f => Direction.West,
                < 270f => Direction.SouthWest,
                < 330f => Direction.SouthEast,
                _      => Direction.East
            };
        }

        public static Axial PolarToAxial(this float polar) => ToAxial(PolarToDirection(polar));

        public static float NormalizePolar(this float polar)
        {
            int threshold = Mathf.FloorToInt(polar / 360f);
            polar -= threshold * 360f;
            return polar;
        }

        public static float ToPolarAngle(this Direction direction) => PolarAngleToCenter[direction];
        public static Direction GetOpposite(this Direction original) => OppositeDirections[original];

        private static readonly HashSet<Cell> ReusableSet = new(); 

        [MustUseReturnValue]
        public static PooledObject<HashSet<Cell>> GetCellsInVision(Cell currentCell, int visionLength, out HashSet<Cell> cellsInVision)
        {
            PooledObject<HashSet<Cell>> pool = HashSetPool<Cell>.Get(out cellsInVision);
            cellsInVision.Add(currentCell);
            ReusableSet.Clear();
            ReusableSet.Add(currentCell);
            for (int i = 0; i < visionLength; i++)
            {
                FixedEnumerable<Cell> fixedEnumerable = ReusableSet.FixedEnumerate();
                ValueListPool<Cell>.Enumerator fixedEnumerator = fixedEnumerable.GetEnumerator();
                ReusableSet.Clear();
                try
                {
                    while (fixedEnumerator.MoveNext())
                    {
                        Cell cell = fixedEnumerator.Current;
                        if (cell!.IsObstacle)
                            continue;

                        foreach (Cell neighbor in cell.Neighbors.Values)
                        {
                            cellsInVision.Add(neighbor);
                            ReusableSet.Add(neighbor);
                        }
                    }
                }
                finally
                {
                    fixedEnumerable.Dispose();
                }
            }

            return pool;
        }

    }
}