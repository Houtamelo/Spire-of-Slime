using System;
using Core.Local_Map.Scripts.Events;
using Core.Main_Database.Local_Map;
using Core.Misc;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using static Core.Utils.Patterns.Option<(Core.Local_Map.Scripts.Events.ILocalMapEvent, float)>;

namespace Core.Local_Map.Scripts.PathCreating
{
    [Serializable]
    public class PathBetweenNodesGenerator
    {
        [SerializeField]
        private bool randomizeLength;

        [SerializeField, LabelText(@"$LengthLabel"), Range(3, 20)]
        private int length;

        [SerializeField, LabelText("Max Length"), ShowIf(nameof(randomizeLength)), Range(4, 22)]
        private int lengthMax;

        [UsedImplicitly]
        private string LengthLabel => randomizeLength ? "Min Length" : "Length";

        [SerializeField]
        private bool randomizeMultiplier;

        [SerializeField, LabelText(@"$MultiplierLabel"), Range(0, 3f)]
        private float pathAverageMultiplier;

        [SerializeField, LabelText("Max Difficulty Multiplier"), ShowIf(nameof(randomizeMultiplier)), Range(0, 3f)]
        private float pathAverageMultiplierMax;

        [UsedImplicitly]
        private string MultiplierLabel => randomizeMultiplier ? "Min Difficulty Multiplier" : "Difficulty Multiplier";

        [SerializeField]
        private bool specifyDangerEvent;

        [SerializeField, ShowIf(nameof(specifyDangerEvent))]
        private SerializableTuple<ScriptableLocalMapEvent, float> dangerEvent;

        [SerializeField]
        private bool specifyRestEvent;

        [SerializeField, ShowIf(nameof(specifyRestEvent))]
        private SerializableTuple<ScriptableLocalMapEvent, float> restEvent;

        [SerializeField]
        private bool specifyCellInfo;

        [SerializeField, ShowIf(nameof(specifyCellInfo))]
        private TileInfo dangerCellInfo;

        [SerializeField, ShowIf(nameof(specifyCellInfo))]
        private TileInfo restCellInfo;

        public PathBetweenNodesGenerator(int length, float pathAverageMultiplier, (ScriptableLocalMapEvent mapEvent, float threat) dangerEvent, (ScriptableLocalMapEvent mapEvent, float threat) restEvent, TileInfo dangerCellInfo, TileInfo restCellInfo)
        {
            this.length = length;
            this.pathAverageMultiplier = pathAverageMultiplier;
            this.dangerEvent = dangerEvent;
            this.restEvent = restEvent;
            this.dangerCellInfo = dangerCellInfo;
            this.restCellInfo = restCellInfo;
        }

        public PathBetweenNodesBlueprint GetBluePrint(bool deterministic, OneWay path, bool isLast)
        {
            Option<(ILocalMapEvent, float)> actualDangerEvent = specifyDangerEvent ? Some(dangerEvent.ToValue()) : None;
            Option<(ILocalMapEvent, float)> actualRestEvent = specifyRestEvent ? Some(restEvent.ToValue()) : None;

            TileInfo actualDangerCellInfo;
            TileInfo actualRestCellInfo;
                
            if (specifyCellInfo)
            {
                actualDangerCellInfo = dangerCellInfo;
                actualRestCellInfo = restCellInfo;
            }
            else
            {
                TileInfo walkableTileInfo = TileInfoDatabase.GetWalkableTileInfo(path).TrySome(out walkableTileInfo) ? walkableTileInfo : TileInfoDatabase.FallbackTileInfo;
                actualDangerCellInfo = walkableTileInfo;

                if (isLast && TileInfoDatabase.GetWorldLocationTileInfo(path.Destination).AssertSome(out TileInfo lastCellInfo))
                    actualRestCellInfo = lastCellInfo;
                else
                    actualRestCellInfo = walkableTileInfo;
            }
            
            if (deterministic)
            {
                int actualLength = randomizeLength ? Mathf.FloorToInt((length + lengthMax) / 2f) : length;
                float actualMultiplier = randomizeMultiplier ? (pathAverageMultiplier + pathAverageMultiplierMax) / 2f : pathAverageMultiplier;
                return new PathBetweenNodesBlueprint(actualLength, actualMultiplier, actualDangerEvent, actualRestEvent, actualDangerCellInfo, actualRestCellInfo);
            }
            else
            {
                int actualLength = randomizeLength ? UnityEngine.Random.Range(length, lengthMax + 1) : length;
                float actualMultiplier = randomizeMultiplier ? UnityEngine.Random.Range(pathAverageMultiplier, pathAverageMultiplierMax) : pathAverageMultiplier;
                return new PathBetweenNodesBlueprint(actualLength, actualMultiplier, actualDangerEvent, actualRestEvent, actualDangerCellInfo, actualRestCellInfo);
            }
        }
    }
}