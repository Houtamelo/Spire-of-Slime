using Core.Local_Map.Scripts.Enums;
using Core.Main_Database.Local_Map;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public class DefaultRestEvent : ILocalMapEvent
    {
        public const float RestMultiplier = 0.5f;
        public const float RestMultiplierAmplitude = 0.1f;
        public const int LustDecrease = 50;
        public const int ExhaustionDecrease = 30;
        public const int OrgasmRestore = +1;

        public CleanString Key => "rest_default";
        public bool AllowSaving => false;
        public IconType GetIconType(in Option<float> multiplier) => IconType.Rest;

        [NotNull]
        public CoroutineWrapper Execute([NotNull] TileInfo tileInfo, in Option<float> multiplier)
        {
            Result<BothWays> location = tileInfo.GetBothWaysPath();
            Option<RestEventBackground> background = location.IsOk ? RestEventsDatabase.GetBackgroundPrefab(location.Value) : Option.None;
            return RestEventHandler.HandleRest(RestMultiplier, RestMultiplierAmplitude, LustDecrease, ExhaustionDecrease, OrgasmRestore, background);
        }
    }
}