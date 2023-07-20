using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Animation;
using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.Enums;
using Core.Local_Map.Scripts.Events;
using Core.Local_Map.Scripts.HexagonObject;
using Core.Local_Map.Scripts.PathCreating;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Local_Map;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Objects;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using static Core.Local_Map.Scripts.Coordinates.PathUtils;
using Random = UnityEngine.Random;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Local_Map.Scripts
{
    public sealed class LocalMapManager : Singleton<LocalMapManager>
    {
        public static bool LOG;
        
        public const float CameraLerpDuration = 0.75f;
        private const int VisionLength = 12;

        [OdinSerialize, SceneObjectsOnly, Required]
        public readonly HexagonMap GameObjectMap;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly PlayerIcon _playerIcon;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly CameraAnimator _cameraAnimator;

        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly ArrowAnimator _arrowAnimator;

        [OdinSerialize, Required]
        private Dictionary<IconType, Sprite> _icons;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource playerMoveToEmptyTileSound, playerMoveToEventTileSound;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource cellMouseOverAudioSource;

        [SerializeField, Required, SceneObjectsOnly]
        private Transform genericEventsParent;
        public Transform GenericEventsParent => genericEventsParent;

        public Option<WorldPath> CurrentSource { get; private set; } = Option.None;

        private Tween _cameraTween, _playerMoveTween;
        private CoroutineWrapper _currentEventRoutine;
        public bool RunningEvent => _currentEventRoutine is { Running: true };
        public LocationEnum Origin { get; private set; }
        public LocationEnum Destination { get; private set; }
        public Cell CurrentPlayerTile { get; private set; }
        private TweenCallback _onPlayerMoveTile;
        private Cell _lastCell;
        private readonly HashSet<Cell> _previousCellsInVision = new();

        protected override void Awake()
        {
            base.Awake();
            _arrowAnimator.SetScale(cellSize: GameObjectMap.CellSize);
            _onPlayerMoveTile = OnCameraTweenComplete;
        }
        private void OnEnable()
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.NotifyEvent(MusicEvent.Exploration);
        }
        private void OnDisable()
        {
            if (MusicManager.Instance.TrySome(out MusicManager musicManager))
                musicManager.LocalMapEnds();
        }

        public Sprite GetIcon(IconType type) => _icons[type];
        
        private Tween SetPlayerTile([NotNull] Cell cell)
        {
            CurrentPlayerTile = cell;
            _playerMoveTween = _playerIcon.GoToCell(CurrentPlayerTile).OnComplete(_onPlayerMoveTile);
            CheckTilesInVision();
            return _cameraAnimator.LerpCamera(worldPos: cell.transform.position, speed: CameraLerpDuration, isSpeedBased: false);
        }

        private void SetPlayerTileImmediateEventless([NotNull] Cell cell)
        {
            cell.ClearEvent();
            cell.SetAlreadyExplored(true);
            CurrentPlayerTile = cell;
            _playerIcon.GoToCellImmediate(cell);
            CheckTilesInVision();
            _cameraAnimator.LerpCamera(worldPos: cell.transform.position, speed: 0.25f, isSpeedBased: false);
        }

        public void CellPointerClick(Cell cell)
        {
            if (_currentEventRoutine is { Running: true } || cell.IsObstacle)
                return;

            if (CurrentPlayerTile.IsNeighbor(cell) == false || _cameraTween is { active: true } || _playerMoveTween is { active: true })
                return;

            if (Save.AssertInstance(out Save save))
            {
                NemaStatus status = save.GetFullNemaStatus();
                
                if (status.SetToClearMist.current && status.Exhaustion.current < NemaStatus.HighExhaustion)
                    save.ChangeNemaExhaustion(+2);

                Option<int> baseDelta = (status.SetToClearMist.current, status.Exhaustion.current) switch
                {
                    (false, _)                             => 5,
                    (true, >= NemaStatus.HighExhaustion)   => 5,
                    (true, >= NemaStatus.MediumExhaustion) => 3,
                    (true, >= NemaStatus.LowExhaustion)    => 1,
                    _                                               => Option.None
                };

                if (baseDelta.IsSome)
                {
                    int delta = baseDelta.Value;
                    save.ChangeLust(Ethel.GlobalKey, delta + Random.Range(-1, 2));
                    save.ChangeLust(Nema.GlobalKey,  delta + Random.Range(-1, 2));
                }
            }

            if (cell.AssignedEvent.IsSome)
                playerMoveToEventTileSound.Play();
            else
                playerMoveToEmptyTileSound.Play();

            _cameraTween = SetPlayerTile(cell);
        }

        private void OnCameraTweenComplete()
        {
            bool shouldPlayEvent = CurrentPlayerTile.AssignedEvent.IsSome && CurrentPlayerTile.AlreadyExplored == false;
            CurrentPlayerTile.SetAlreadyExplored(true);
            if (shouldPlayEvent)
                HandleEvent(CurrentPlayerTile);
            else
                CurrentPlayerTile.ClearEvent();
        }

        [Button]
        private void CheckTilesInVision()
        {
            if (CurrentPlayerTile == null || Save.AssertInstance(out Save save) == false)
                return;
            
            using FixedEnumerator<Cell> previousInVision = _previousCellsInVision.FixedEnumerate();
            using PooledObject<HashSet<Cell>> _ = GetCellsInVision(CurrentPlayerTile, VisionLength, out HashSet<Cell> cellsInVision);
            foreach (Cell cell in cellsInVision)
            {
                cell.SetVisible(visible: true);
                _previousCellsInVision.Add(cell);
            }
            
            foreach (Cell cell in previousInVision)
            {
                if (cellsInVision.Contains(cell))
                    continue;
                    
                cell.SetVisible(visible: false);
                _previousCellsInVision.Remove(cell);
            }
        }

        private void HandleEvent(Cell cell)
        {
            if (LocalMapEventHandler.AssertInstance(out LocalMapEventHandler handler) && cell.AssignedEvent.AssertSome(out (ILocalMapEvent mapEvent, float multiplier) tuple))
            {
                cell.ClearEvent();
                _currentEventRoutine = handler.HandleEvent(tuple.mapEvent, tuple.multiplier, cell.TileInfo);
            }
        }

        [NotNull]
        public LocalMapRecord GenerateRecord() => LocalMapRecord.FromLocalMap(localMapManager: this);

        public void CellPointerEnter([NotNull] Cell cell)
        {
            cellMouseOverAudioSource.Play();
            if (cell.IsObstacle || cell.AlreadyExplored || !CurrentPlayerTile.IsNeighbor(cell))
                return;
            
            _arrowAnimator.transform.SetParent(cell.transform);
            _arrowAnimator.SetScale(cellSize: GameObjectMap.CellSize);
            _arrowAnimator.gameObject.SetActive(true);
        }

        public void CellPointerExit([NotNull] Cell cell)
        {
            if (_arrowAnimator.transform.parent == cell.transform)
            {
                _arrowAnimator.transform.SetParent(p: GameObjectMap.transform);
                _arrowAnimator.gameObject.SetActive(false);
            }
        }

        public void GenerateMap(Option<WorldPath> source, ReadOnlySpan<PathBetweenNodesBlueprint> nodes, in FullPathInfo fullPathInfo, LocationEnum origin, LocationEnum destination)
        {
            if (Save.AssertInstance(out Save save) == false)
                return;
            
            CurrentSource = source;
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.NotifyEvent(MusicEvent.Exploration);

            Origin = origin;
            Destination = destination;
            GameObjectMap.ClearCells();
            const int mapSize = 6;

            int totalLength = 0;
            foreach (PathBetweenNodesBlueprint path in nodes)
                totalLength += path.Length;

            List<HexagonPath> allPaths = new();
            Axial firstCellPosition = Axial.Zero;
            Axial secondCellPosition = fullPathInfo.PolarEndAngle.PolarToAxialLength(length: 1);
            Axial lastNode = Axial.FromPolarAngleDeg(fullPathInfo.PolarEndAngle, totalLength);

            Dictionary<Axial, Cell> map = GenerateMapLayout(firstCellPosition, lastNode, mapSize + nodes.Max(selector: info => info.Length), GameObjectMap);
            foreach (Cell cell in map.Values)
                cell.FindNeighbors(map);

            Cell firstCell = map[firstCellPosition];
            firstCell.SetVisuals(fullPathInfo.PathInfo.WalkableTileInfo, fullPathInfo.PathInfo.WalkableTileInfo.GetRandomSprite());

            Cell secondCell = map[secondCellPosition];
            secondCell.SetVisuals(fullPathInfo.PathInfo.WalkableTileInfo, fullPathInfo.PathInfo.WalkableTileInfo.GetRandomSprite());
            
            Axial nodeBehindFirst = fullPathInfo.PolarEndAngle.PolarToDirection().GetOpposite().ToAxial() + firstCellPosition;
            Cell behindFirstCell = map[nodeBehindFirst];
            behindFirstCell.SetForcedObstacle(false);
            behindFirstCell.TrySetObstacle(false);
            behindFirstCell.SetVisuals(fullPathInfo.StartCellInfo, fullPathInfo.StartCellInfo.GetRandomSprite());
            behindFirstCell.SetEvent(MapEventDatabase.GetDefaultReachLocationEvent(origin), multiplier: 1f, overrideExisting: false);

            List<(Cell dangerCell, Cell restCell)> mainTiles = new() { (map[firstCellPosition], map[secondCellPosition]) };
            
            SetMainNodesEvents(Destination, nodes, fullPathInfo.PolarEndAngle, firstCellPosition, secondCellPosition, ref mainTiles, lastNode, ref map);
            
            CheckMainTilesAccessibility(mainTiles, map);
            
            GeneratePaths(nodes, fullPathInfo.PathInfo, map[secondCellPosition], mainTiles, ref map, ref allPaths);

            SetObstacles(fullPathInfo, allPaths, map, mainTiles, behindFirstCell);

            AssignEventsToCells(allPaths);
            
            _lastCell = map[lastNode];

            Cell playerCell = map[firstCellPosition];
            SetPlayerTileImmediateEventless(playerCell);
            
            GameObjectMap.ClearUnreachableCells();
            
            SavePoint.RecordLocalMap(manager: this);

            GC.Collect();
        }

        public void GenerateMap([NotNull] LocalMapRecord record)
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.NotifyEvent(MusicEvent.Exploration);

            GameObjectMap.GenerateMap(record.Cells);
            Origin = record.Origin;
            Destination = record.Destination;
            Axial position = new(record.PlayerCell.Q, record.PlayerCell.R);
            if (GameObjectMap.TryGetCell(position, out Cell playerCell) == false)
            {
                Debug.LogWarning($"Failed to get player cell on position: {position}, returning to main menu...", this);
                GameManager.Instance.Value.PauseMenuToMainMenu();
                return;
            }

            SetPlayerTileImmediateEventless(playerCell);
            GC.Collect();
        }

        private static void AssignEventsToCells([NotNull] List<HexagonPath> allPaths)
        {
            Dictionary<Cell, HashSet<HexagonPath>> cellsOnMultiplePaths = new();
            foreach (HexagonPath outerPath in allPaths) // checking cells that are on multiple paths
            {
                foreach (Cell outerCell in outerPath.Cells)
                {
                    foreach (HexagonPath innerPath in allPaths)
                    {
                        if (innerPath == outerPath)
                            continue;

                        foreach (Cell innerCell in innerPath.Cells)
                        {
                            if (innerCell != outerCell)
                                continue;

                            if (cellsOnMultiplePaths.TryGetValue(innerCell, out HashSet<HexagonPath> set))
                            {
                                set.Add(outerPath);
                                set.Add(innerPath);
                            }
                            else
                            {
                                cellsOnMultiplePaths[innerCell] = new HashSet<HexagonPath> { innerPath, outerPath };
                            }

                            break;
                        }
                    }
                }
            }

            foreach ((Cell cell, HashSet<HexagonPath> containerPaths) in cellsOnMultiplePaths)
            {
                if (cell.HasEvent || Random.value >= HexagonPath.CombatQuantity)
                    continue;

                float threat = 0;
                int count = 0;
                foreach (HexagonPath path in containerPaths)
                {
                    threat += path.DesiredThreat;
                    count++;
                }

                threat /= count;
                cell.SetEvent(MapEventDatabase.DefaultCombatEvent, threat, false);
            }

            HashSet<Cell> alreadyAssigned = new(capacity: cellsOnMultiplePaths.Count);
            foreach (HexagonPath path in allPaths)
                path.AssignEvents(alreadyPicked: alreadyAssigned);
        }

        private static void SetObstacles(in FullPathInfo fullPathInfo, [NotNull] List<HexagonPath> allPaths, [NotNull] Dictionary<Axial, Cell> map, List<(Cell dangerCell, Cell restCell)> mainTiles, Cell behindFirstCell)
        {
            foreach (HexagonPath path in allPaths)
            {
                foreach (Cell tile in path.Cells)
                    tile.TrySetObstacle(false);
            }

            foreach (Cell cell in map.Values)
            {
                if (cell == behindFirstCell)
                    continue;
                
                bool dangerOrRest = false;
                foreach ((Cell dangerCell, Cell restCell) m in mainTiles)
                {
                    if (m.dangerCell == cell || m.restCell == cell)
                    {
                        dangerOrRest = true;
                        break;
                    }
                }

                if (dangerOrRest)
                {
                    cell.SetForcedObstacle(false);
                    cell.TrySetObstacle(false);
                    continue;
                }

                bool found = false;
                foreach (HexagonPath pathClass in allPaths)
                {
                    if (pathClass.Cells.Contains(cell))
                    {
                        cell.SetForcedObstacle(false);
                        cell.TrySetObstacle(false);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    cell.TrySetObstacle(true);

                if (cell.IsObstacle)
                {
                    TileInfo tileInfo = fullPathInfo.PathInfo.ObstacleInfo;
                    cell.SetVisuals(tileInfo, tileInfo.GetRandomSprite());
                }
            }
        }

        private static void GeneratePaths(ReadOnlySpan<PathBetweenNodesBlueprint> nodes, PathInfo pathInfo, Cell secondNode, 
                                          List<(Cell dangerCell, Cell restCell)> mainTiles, ref Dictionary<Axial, Cell> map, ref List<HexagonPath> allPaths)
        {
            Cell nodeStart = secondNode;
            for (int index = 0; index < mainTiles.Count - 1; index++)
            {
                PathBetweenNodesBlueprint nodeInfo = nodes[index];
                int pathsToNode = 2 + Random.Range(minInclusive: 0, maxExclusive: 2);
                (Cell dangerCell, Cell restCell) nextPair = mainTiles[index + 1];
                Cell pathEnd = nextPair.dangerCell;
                int totalDistance = ManhattanDistance(nodeStart.position, pathEnd.position);

                using CustomValuePooledList<(Direction direction, Axial axial)> startNeighbors = GetValidNeighbors(mainTiles, map, nodeStart);
                using CustomValuePooledList<Direction> mirroredEnds = GetMirror(originals: startNeighbors.AsSpan());
                
                List<int> startIndexes = new() { 0, 1, 2 };

                float startCenterDirectionAngle = GetCenterDirectionOffTuple(startNeighbors.AsSpan()).ToPolarAngle();
                float endCenterDirectionAngle = GetCenterDirection(mirroredEnds.AsSpan()).ToPolarAngle();

                (PathInfo pathInfo, Axial startPos, Cell[] intermediaryNodes)[] nodePaths = new (PathInfo, Axial, Cell[])[pathsToNode];
                for (int i = 0; i < nodePaths.Length; i++)
                {
                    int neighborIndex = startIndexes.TakeRandom<int, List<int>>();
                    (Direction startDirection, Axial pathStart) = startNeighbors[neighborIndex];
                    float firstDirectionAngle = MathExtensions.GetSmallestClockwiseAngle(start: startCenterDirectionAngle, end: startDirection.ToPolarAngle());
                    (float, float) firstRange = firstDirectionAngle switch
                    {
                        > 0 => (-45, 0),
                        < 0 => (0, 45),
                        _ => (-22.5f, 22.5f)
                    };

                    float firstAngle = startDirection.ToPolarAngle() + Random.Range(minInclusive: firstRange.Item1, maxInclusive: firstRange.Item2);
                    float firstMultiplier =  0.75f + (Mathf.Abs(firstDirectionAngle) * 0.00416667f); // same as dividing by 240 but more efficient
                    int firstLength = Mathf.CeilToInt((totalDistance / 3f) * (1.25f - (Random.value / 2f)) * firstMultiplier);
                    Axial firstIntermediary = firstAngle.PolarToAxialLength(length: firstLength) + pathStart;

                    Direction endDirection = mirroredEnds[neighborIndex];
                    float secondDirectionAngle = MathExtensions.GetSmallestClockwiseAngle(start: endCenterDirectionAngle, end: endDirection.ToPolarAngle());
                    (float, float) secondRange = secondDirectionAngle switch
                    {
                        > 0 => (-45, 0),
                        < 0 => (0, 45),
                        _ => (-22.5f, 22.5f)
                    };

                    float secondAngle = endDirection.ToPolarAngle() + Random.Range(minInclusive: secondRange.Item1, maxInclusive: secondRange.Item2);
                    float secondMultiplier =  0.75f + (Mathf.Abs(secondDirectionAngle) * 0.00416667f); // same as dividing by 240 but more efficient
                    int secondLength = Mathf.CeilToInt((totalDistance / 3f) * (1.25f - (Random.value / 2f)) * secondMultiplier);
                    Axial secondIntermediary = secondAngle.PolarToAxialLength(length: secondLength) + pathEnd.position;
                    nodePaths[i] = (pathInfo, pathStart, new[] { map[firstIntermediary], map[secondIntermediary] });
                }

                List<PathTrio> candidates = new();

                for (int tries = 0; tries < 1000; tries++)
                {
                    List<Cell> one = null, two = null, three = null;
                    using (PooledObject<Dictionary<Axial, (Cell cell, float weight)>> __ = GenerateRandomWeightMap(map, out Dictionary<Axial, (Cell cell, float weight)> weightMap))
                    {
                        bool success = true;
                        for (int i = 0; i < nodePaths.Length; i++)
                        {
                            (PathInfo _, Axial pathStart, Cell[] intermediaryNodes) = nodePaths[i];
                            if (PathCreator.CreatePath(weightMap, map[pathStart], pathEnd, maxLength: (int)(totalDistance * 1.6f), out List<Cell> tiles, intermediaryNodes) == false)
                            {
                                success = false;
                                break;
                            }

                            switch (i)
                            {
                                case 0:  one = tiles; break;
                                case 1:  two = tiles; break;
                                case 2:  three = tiles; break;
                            }
                        }
                        
                        if (success == false)
                            continue;

                        switch (Mathf.Min(nodePaths.Length, 3))
                        {
                            case 1: candidates.Add(new PathTrio(one)); break;
                            case 2: candidates.Add(new PathTrio(one, two)); break;
                            case 3: candidates.Add(new PathTrio(one, two, three)); break;
                        }
                    }
                }

                if (candidates.Count == 0)
                    throw new Exception("No valid candidates, improve your code.");

                candidates = candidates.OrderBy(trio => trio.Score).ToList();
                
                if (LOG)
                {
                    StringBuilder builder = new(candidates.Count.ToString());
                    builder.Append(" candidates found. \n");

                    Dictionary<int, int> similarCounts = new();
                    foreach (PathTrio trio in candidates)
                    {
                        int score = trio.Score;
                        if (similarCounts.TryGetValue(score, out int count))
                            similarCounts[score] = count + 1;
                        else
                            similarCounts.Add(score, 1);
                    }

                    builder.AppendLine("Score | Count");
                    IOrderedEnumerable<KeyValuePair<int, int>> orderedScores = similarCounts.OrderBy(pair => pair.Key);
                    foreach (KeyValuePair<int, int> pair in orderedScores)
                        builder.AppendLine(pair.Key.ToString("000"), "   |   ", pair.Value.ToString("000"));
                    
                    Debug.Log(builder.ToString());
                }

                PathTrio best = candidates[0];
                List<PathTrio> tiesForBest = new() { best };
                for (int i = 1; i < candidates.Count; i++)
                {
                    PathTrio candidate = candidates[i];
                    if (candidate.Score == best.Score)
                        tiesForBest.Add(candidate);
                    else
                        break;
                }
                
                best = tiesForBest.GetRandom();
                
                for (int i = 0; i < nodePaths.Length; i++)
                {
                    List<Cell> tiles = i switch
                    {
                        0 => best.One,
                        1 => best.Two,
                        _ => best.Three
                    };
                    
                    (PathInfo info, Axial _, Cell[] _) = nodePaths[i];
                    HexagonPath path = new(tiles, info, nodeInfo);
                    allPaths.Add(path);
                }

                nodeStart = nextPair.restCell;
            }

            return;

            [MustUseReturnValue]
            static CustomValuePooledList<(Direction direction, Axial axial)> GetValidNeighbors(List<(Cell dangerCell, Cell restCell)> mainTiles, Dictionary<Axial, Cell> map, [NotNull] Cell start)
            {
                CustomValuePooledList<(Direction, Axial)> startNeighbors = start.position.GetClockWiseOrderedNeighbors();
                for (int i = 0; i < startNeighbors.Count; i++)
                {
                    (Direction _, Axial axial) = startNeighbors.ToArray()[i];
                    Cell tile = map[axial];
                    bool equalsMain = false;
                    foreach ((Cell dangerAxial, Cell restAxial) in mainTiles)
                    {
                        if (dangerAxial == tile || restAxial == tile)
                        {
                            equalsMain = true;
                            break;
                        }
                    }

                    if (tile.ForcedObstacle || equalsMain)
                    {
                        startNeighbors.RemoveAt(index: i);
                        i--;
                    }
                }

                return startNeighbors;
            }
            
            static Direction GetCenterDirectionOffTuple(ReadOnlySpan<(Direction direction, Axial)> directions)
            {
                Direction best = directions[0].direction;
                float bestDistance = float.MaxValue;
                foreach ((Direction outer, _) in directions)
                {
                    float distance = 0;
                    foreach ((Direction inner, _) in directions) 
                        distance += Mathf.Abs(MathExtensions.GetSmallestClockwiseAngle(start: outer.ToPolarAngle(), end: inner.ToPolarAngle()));

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = outer;
                    }
                }

                return best;
            }
            
            static Direction GetCenterDirection(ReadOnlySpan<Direction> directions)
            {
                Direction best = directions[0];
                float bestDistance = float.MaxValue;
                foreach (Direction outer in directions)
                {
                    float distance = 0;
                    foreach (Direction inner in directions) 
                        distance += Mathf.Abs(MathExtensions.GetSmallestClockwiseAngle(start: outer.ToPolarAngle(), end: inner.ToPolarAngle()));

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = outer;
                    }
                }

                return best;
            }
            
            static CustomValuePooledList<Direction> GetMirror(ReadOnlySpan<(Direction direction, Axial)> originals)
            {
                CustomValuePooledList<Direction> mirror = new(capacity: 3);
                Span<Direction> array = stackalloc Direction[3];
                for (int i = 0; i < 3; i++) 
                    mirror.Add(originals[i].direction.GetOpposite());

                Direction originalCenter = GetCenterDirectionOffTuple(originals);
                int indexOfOriginalCenter = originals[0].direction == originalCenter ? 0 : originals[1].direction == originalCenter ? 1 : 2; //messy but avoids memory allocation
                Direction mirrorCenter = GetCenterDirection(mirror.AsSpan());
                array[indexOfOriginalCenter] = mirrorCenter;
                for (int index = 0; index < mirror.Count; index++)
                {
                    if (index == indexOfOriginalCenter)
                        continue;
                    
                    Direction outer = mirror[index];

                    float originalDistance = MathExtensions.GetSmallestClockwiseAngle(start: originalCenter.ToPolarAngle(), end: originals[index].direction.ToPolarAngle());
                    float bestDistance = float.MaxValue;
                    Direction best = outer;
                    foreach (Direction inner in mirror)
                    {
                        if (inner == outer || inner == mirrorCenter)
                            continue;

                        float candidateDistance = MathExtensions.GetSmallestClockwiseAngle(start: mirrorCenter.ToPolarAngle(), end: inner.ToPolarAngle()) + originalDistance;
                        if (Mathf.Abs(candidateDistance) < bestDistance)
                        {
                            bestDistance = candidateDistance;
                            best = inner;
                        }
                    }

                    array[index] = best;
                }

                mirror.Clear();
                foreach (Direction direction in array) 
                    mirror.Add(direction);

                return mirror;
            }

            static PooledObject<Dictionary<Axial, (Cell cell, float weight)>> GenerateRandomWeightMap([NotNull] IReadOnlyDictionary<Axial, Cell> source, out Dictionary<Axial, (Cell cell, float weight)> weightMap)
            {
                PooledObject<Dictionary<Axial, (Cell cell, float weight)>> pool = DictionaryPool<Axial, (Cell cell, float weight)>.Get(out weightMap);
                foreach (Cell cell in source.Values)
                    weightMap[cell.position] = (cell, cell.ForcedObstacle ? 9999999f : cell.IsObstacle ? 70f : Random.Range(1f, 10f));

                return pool;
            }
        }

        private static void CheckMainTilesAccessibility([NotNull] List<(Cell dangerCell, Cell restCell)> mainTiles, IReadOnlyDictionary<Axial, Cell> map)
        {
            foreach ((Cell dangerTile, Cell restTile) in mainTiles)
            {
                dangerTile.SetForcedObstacle(false);
                dangerTile.TrySetObstacle(false);
                restTile.SetForcedObstacle(false);
                restTile.TrySetObstacle(false);
                
                foreach ((Direction _, Cell tile) in dangerTile.Neighbors)
                    tile.TrySetObstacle(false);

                foreach ((Direction _, Cell tile) in restTile.Neighbors)
                    tile.TrySetObstacle(false);

                foreach ((Direction direction, Cell adjacent) in dangerTile.Neighbors)
                {
                    if (adjacent.IsNeighbor(restTile) == false || adjacent == restTile)
                        continue;
                    
                    adjacent.SetForcedObstacle(true);
                    Axial axialDirection = direction.ToAxial();
                    Axial wall = dangerTile.position + (axialDirection * 2);
                    while (map.TryGetValue(wall, out Cell wallTile))
                    {
                        wallTile.SetForcedObstacle(true);
                        wall += axialDirection;
                    }
                }

                foreach ((Direction direction, Cell adjacent) in restTile.Neighbors)
                {
                    if (adjacent.IsNeighbor(dangerTile) == false || adjacent == dangerTile)
                        continue;
                    
                    adjacent.SetForcedObstacle(true);
                    Axial axialDirection = direction.ToAxial();
                    Axial wall = restTile.position + (axialDirection * 2);
                    while (map.TryGetValue(wall, out Cell wallTile))
                    {
                        wallTile.SetForcedObstacle(true);
                        wall += axialDirection;
                    }
                }
            }
        }

        [NotNull]
        private static Dictionary<Axial, Cell> GenerateMapLayout(Axial firstNode, Axial lastNode, int mapSize, HexagonMap gameObjectMap)
        {
            Offset firstOff = firstNode.ToOffset();
            Offset lastOff = lastNode.ToOffset();
            int width = Mathf.Abs(value: lastOff.col - firstOff.col) + mapSize;
            int height = Mathf.Abs(value: lastOff.row - firstOff.row) + mapSize;
            Offset centerOff = new((firstOff.col + lastOff.col) / 2, (firstOff.row + lastOff.row) / 2);
            Axial center = centerOff.ToAxial();
            HexagonGrid hexagonGrid = new(MapShape.Rectangle, width, height, HexOrientation.Pointy, center);
            Dictionary<Axial, Cell> map = hexagonGrid.Tiles.GenerateTiles(gameObjectMap);
            return map;
        }

        private static void SetMainNodesEvents(LocationEnum destination, ReadOnlySpan<PathBetweenNodesBlueprint> nodes, float polarEndAngle, Axial firstNode, Axial secondNode,
                                               [NotNull] ref List<(Cell dangerCell, Cell restCell)> mainTiles, Axial lastDangerAxial, [NotNull] ref Dictionary<Axial, Cell> map)
        {
            (Axial _, Axial restAxial) = (firstNode, secondNode);
            for (int index = 0; index < nodes.Length - 1; index++)
            {
                PathBetweenNodesBlueprint nodeInfo = nodes[index];
                Axial nextDangerAxial = polarEndAngle.PolarToAxialLength(length: nodeInfo.Length - 1) + restAxial;
                Axial nextRestAxial = polarEndAngle.PolarToAxialLength(length: 1) + nextDangerAxial;
                
                Cell restCell = map[nextRestAxial];
                restCell.SetVisuals(nodeInfo.RestCellInfo, nodeInfo.RestCellInfo.GetRandomSprite());
                (ILocalMapEvent mapEvent, float threat) restEvent = nodeInfo.RestEvent.TrySome(out restEvent) ? restEvent : (MapEventDatabase.DefaultRestEvent, 1f);
                restCell.SetEvent(restEvent.mapEvent, restEvent.threat, overrideExisting: true);

                Cell dangerCell = map[nextDangerAxial];
                dangerCell.SetVisuals(nodeInfo.DangerCellInfo, nodeInfo.DangerCellInfo.GetRandomSprite());
                (ILocalMapEvent mapEvent, float threat) dangerEvent = nodeInfo.DangerEvent.TrySome(out dangerEvent) ? dangerEvent : (MapEventDatabase.DefaultCombatEvent, MapEventDatabase.DangerMultiplier);
                dangerCell.SetEvent(dangerEvent.mapEvent, dangerEvent.threat, overrideExisting: true);

                mainTiles.Add((dangerCell, restCell));
                restAxial = nextRestAxial;
            }

            PathBetweenNodesBlueprint lastNodeInfo = nodes[^1];

            Cell lastDangerCell = map[lastDangerAxial];
            lastDangerCell.SetVisuals(lastNodeInfo.DangerCellInfo, lastNodeInfo.DangerCellInfo.GetRandomSprite());
            (ILocalMapEvent mapEvent, float threat) lastDangerEvent = lastNodeInfo.DangerEvent.TrySome(out lastDangerEvent) ? lastDangerEvent : (MapEventDatabase.DefaultCombatEvent, MapEventDatabase.DangerMultiplier);
            lastDangerCell.SetEvent(lastDangerEvent.mapEvent, lastDangerEvent.threat, overrideExisting: true);

            Cell reachLocationCell = map[polarEndAngle.PolarToAxialLength(length: 1) + lastDangerAxial];
            reachLocationCell.SetVisuals(lastNodeInfo.RestCellInfo, lastNodeInfo.RestCellInfo.GetRandomSprite());
            (ILocalMapEvent mapEvent, float threat) lastRestEvent = lastNodeInfo.RestEvent.TrySome(out lastRestEvent) ? lastRestEvent : (MapEventDatabase.GetDefaultReachLocationEvent(destination), 1f);
            reachLocationCell.SetEvent(lastRestEvent.mapEvent, lastRestEvent.threat, overrideExisting: true);

            mainTiles.Add((lastDangerCell, reachLocationCell));
        }


#if UNITY_EDITOR


        [Button]
        private void GoToLast()
        {
            SetPlayerTile(_lastCell);
        }

#endif
    }
}