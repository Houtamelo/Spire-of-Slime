using System;
using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.Events;
using Core.Local_Map.Scripts.Events.Combat;
using Core.Local_Map.Scripts.HexagonObject;
using Core.Utils.Extensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Local_Map.Scripts
{
    public sealed class HexagonMap : SerializedMonoBehaviour
    {
        [OdinSerialize, AssetsOnly, Required]
        private readonly Cell _cellPrefab;
        
        private readonly Dictionary<Axial, Cell> _map = new();
        public IReadOnlyDictionary<Axial, Cell> AllCells => _map;

        public Cell GetOrCreateCell(Axial position)
        {
            if (_map.TryGetValue(position, out Cell cell))
                return cell;
            
            cell = Instantiate(_cellPrefab, parent: transform);
            _map[position] = cell;
            
            cell.Initialize();
            cell.SetPosition(position);
            cell.RefreshPositionAndSize(origin: transform.position);
            return cell;
        }

        public bool TryGetCell(Axial position, out Cell cellObject) => _map.TryGetValue(position, out cellObject);

        public float CellSize => _cellPrefab.Size;

        public void ClearUnreachableCells()
        {
            Cell[] allCells = _map.Values.ToArray();
            foreach (Cell cell in allCells)
            {
                if (cell.Neighbors.Values.Any(neighbor => !neighbor.IsObstacle))
                    continue;

                Axial position = cell.position;
                _map.Remove(position);
                Destroy(cell.gameObject);
            }

            foreach (Cell cell in _map.Values) 
                cell.FindNeighbors(_map);
        }

        public void ClearCells()
        {
#if UNITY_EDITOR
            foreach (Cell cell in GetComponentsInChildren<Cell>())
                if (Application.isPlaying)
                    Destroy(obj: cell.gameObject);
                else
                    DestroyImmediate(obj: cell.gameObject);
#endif
            
            foreach ((_, Cell cell) in _map)
            {
                if (cell == null)
                    continue;
                
                if (Application.isPlaying)
                    Destroy(obj: cell.gameObject);
                else
                    DestroyImmediate(obj: cell.gameObject);
            }

            _map.Clear();
            GC.Collect(); // collecting because depending on the number of existing cells the memory allocated could be high
        }

        public void GenerateMap(Cell.Record[] cells)
        {
            ClearCells();
            foreach (Cell.Record record in cells)
            {
                Axial position = new(record.Q, record.R);
                Cell cell = GetOrCreateCell(position);
                cell.LoadData(record);
            }
            
            foreach (Cell cell in _map.Values)
                cell.FindNeighbors(_map);
        }
        
        
        #if UNITY_EDITOR
        [Button]
        private void ArrangeTiles()
        {
            Cell[] cells = GetComponentsInChildren<Cell>(includeInactive: true);
            int col = 0;
            int row = 0;
            foreach (Cell cell in cells)
            {
                cell.SetPosition(new Offset(col, row).ToAxial());
                col++;
                if (col == 10)
                {
                    col = 0;
                    row++;
                }
            }
        }

        [Button]
        private void ClearAllCombats()
        {
            _map.Values.DoForEach(cell =>
            {
                if (cell.AssignedEvent.TrySome(out (ILocalMapEvent mapEvent, float multiplier) assignedEvent) && assignedEvent.mapEvent is DefaultCombatEvent)
                    cell.SetEvent(null, 0f, true);
            });
        }
        #endif
    }
}