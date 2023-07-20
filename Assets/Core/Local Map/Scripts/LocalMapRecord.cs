using System.Collections.Generic;
using System.Text;
using Core.Local_Map.Scripts.Coordinates;
using Core.Local_Map.Scripts.HexagonObject;
using Core.Utils.Extensions;
using Core.World_Map.Scripts;
using JetBrains.Annotations;

namespace Core.Local_Map.Scripts
{
    public record LocalMapRecord(Cell.Record[] Cells, LocationEnum Origin, LocationEnum Destination, Cell.Record PlayerCell)
    {
        private static readonly List<Cell.Record> ReusableList = new();

        [NotNull]
        public static LocalMapRecord FromLocalMap([NotNull] LocalMapManager localMapManager)
        {
            ReusableList.Clear();
            IReadOnlyDictionary<Axial, Cell> allCells = localMapManager.GameObjectMap.AllCells;
            foreach (Cell cell in allCells.Values)
                ReusableList.Add(cell.GenerateRecord());

            LocationEnum origin = localMapManager.Origin;
            LocationEnum destination = localMapManager.Destination;
            Cell.Record playerCell = localMapManager.CurrentPlayerTile.GenerateRecord();
            return new LocalMapRecord(ReusableList.ToArray(), origin, destination, playerCell);
        }

        public bool IsDataValid(StringBuilder errors)
        {
            if (Cells == null)
            {
                errors.AppendLine("Invalid ", nameof(LocalMapRecord), ". Cells array is null");
                return false;
            }

            if (Cells.Length == 0)
            {
                errors.AppendLine("Invalid ", nameof(LocalMapRecord), ". Cells array is empty");
                return false;
            }

            for (int index = 0; index < Cells.Length; index++)
            {
                Cell.Record cell = Cells[index];
                if (cell == null)
                {
                    errors.AppendLine("Invalid ", nameof(LocalMapRecord), ". Cell at index ", index.ToString(), " is null");
                    return false;
                }

                if (cell.IsDataValid(errors) == false)
                    return false;
            }

            if (PlayerCell == null)
            {
                errors.AppendLine("Invalid ", nameof(LocalMapRecord), ". PlayerCell is null");
                return false;
            }

            if (Origin == Destination)
            {
                errors.AppendLine("Invalid ", nameof(LocalMapRecord), ". Origin and Destination are the same");
                return false;
            }

            return true;
        }
    }
}