using System;
using System.Collections.Generic;
using Core.Local_Map.Scripts.HexagonObject;
using Core.Main_Database.Local_Map;
using Core.Utils.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Local_Map.Scripts.PathCreating
{
    [Serializable]
    public class HexagonPath
    {
        private const float ThreatAmplitude = 0.2f;
        public const float CombatQuantity = 0.2f;
        
        public readonly List<Cell> Cells;
        public readonly PathInfo PathInfo;
        public readonly PathBetweenNodesBlueprint NodeInfo;
        public float DesiredThreat => NodeInfo.Length * NodeInfo.PathAverageMultiplier / Cells.Count;


        public HexagonPath(List<Cell> tiles, PathInfo pathInfo, PathBetweenNodesBlueprint nodeInfo)
        {
            Cells = tiles;
            PathInfo = pathInfo;
            NodeInfo = nodeInfo;

            foreach (Cell cell in Cells)
            {
                TileInfo tileInfo = PathInfo.WalkableTileInfo;
                cell.SetVisuals(info: tileInfo, sprite: tileInfo.GetRandomSprite());
            }
        }

        public void AssignEvents(HashSet<Cell> alreadyPicked)
        {
            float desiredThreat = (float) NodeInfo.Length / Cells.Count;
            float averageThreat = desiredThreat;
            int divider = 1;
            int totalCombats = Mathf.FloorToInt(Cells.Count * CombatQuantity);

            List<Cell> cells = new(Cells);

            for (int i = 0; i < totalCombats; i++)
            {
                if (cells.Count == 0)
                    break;
                
                Cell cell = cells.TakeRandom<Cell, List<Cell>>();
                if (alreadyPicked.Contains(cell))
                {
                    i--;
                    continue;
                }

                divider++;
                float multiplier = desiredThreat / averageThreat;
                float threat = desiredThreat * Random.Range(minInclusive: 1 - ThreatAmplitude, maxInclusive: 1 + ThreatAmplitude) * Random.Range(minInclusive: 1, maxInclusive: multiplier);
                cell.SetEvent(MapEventDatabase.DefaultCombatEvent, threat, overrideExisting: false);
                averageThreat = ((averageThreat * (divider - 1)) + threat) / divider;
            }
        }
        
    }
}