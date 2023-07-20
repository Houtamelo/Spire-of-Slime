using Core.Local_Map.Scripts.Enums;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Local_Map.Scripts
{
    [CreateAssetMenu(menuName = "Database/Local Map/Tile Info", fileName = "tile-info_")]
    public class TileInfo : ScriptableObject
    {
        public CleanString Key => name;

        [field: SerializeField, HideIf(nameof(HideTwo))]
        public bool IsOneWayPath { get; private set; }

        [SerializeField, LabelText(@"$LabelOne"), ValidateInput(nameof(IsOneDifferentThanTwo))]
        private LocationEnum one;

        [SerializeField, HideIf(nameof(HideTwo)), LabelText(@"$LabelTwo"), ValidateInput(nameof(IsOneDifferentThanTwo))]
        private LocationEnum two;

        [field: SerializeField]
        public TileType Type { get; private set; }

        [SerializeField, Required]
        private Sprite[] mapSprites;

        public Sprite GetRandomSprite() => mapSprites.GetRandom();
        public Option<Sprite> GetSpriteByIndex(int index) => index >= 0 && index < mapSprites.Length ? mapSprites[index] : Option<Sprite>.None;
        public int GetSpriteIndex(Sprite sprite)
        {
            for (var i = 0; i < mapSprites.Length; i++)
            {
                if (mapSprites[i] == sprite)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// This operation is valid even if it's a one-way path.
        /// </summary>
        [Pure]
        public Result<BothWays> GetBothWaysPath()
        {
            if (Type == TileType.WorldLocation)
                return Result<BothWays>.Error("World location tiles don't have a path");

            return Result<BothWays>.Ok((one, two));
        }

        [Pure]
        public Result<OneWay> GetOneWayPath()
        {
            if (!IsOneWayPath)
                return Result<OneWay>.Error("This tile is not a one way path");

            if (Type == TileType.WorldLocation)
                return Result<OneWay>.Error("World location tiles don't have a path");

            return Result<OneWay>.Ok((one, two));
        }

        [Pure]
        public Result<LocationEnum> GetWorldLocation()
        {
            if (Type != TileType.WorldLocation)
                return Result<LocationEnum>.Error("Only world location tiles have a single location");
            
            return one;
        }

        private bool HideTwo => Type == TileType.WorldLocation;

        [UsedImplicitly, NotNull]
        private string LabelOne => Type == TileType.WorldLocation ? "World Location" : IsOneWayPath ? "Origin" : "End/Start Location";

        [UsedImplicitly, NotNull]
        private string LabelTwo => IsOneWayPath ? "Destination" : "End/Start Location";
        
        private bool IsOneDifferentThanTwo() => one != two;
    }
}