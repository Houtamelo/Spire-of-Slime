using Core.Local_Map.Scripts.Enums;
using Core.World_Map.Scripts;
using Main_Database.Local_Map;
using Save_Management;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public class DefaultRestEvent : ILocalMapEvent
    {
        public const float RestMultiplier = 0.5f;
        public const float RestMultiplierDelta = 0.1f;
        public const int LustDecrease = 50;
        public const float ExhaustionDecrease = 0.3f;
        public const int OrgasmRestore = +1;

        public CleanString Key => "rest_default";
        public bool AllowSaving => false;
        public IconType GetIconType(in Option<float> multiplier) => IconType.Rest;

        public CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier)
        {
            Result<BothWays> location = tileInfo.GetBothWaysPath();
            Option<RestEventBackground> background = location.IsOk ? RestEventsDatabase.GetBackgroundPrefab(location.Value) : Option<RestEventBackground>.None;
            return RestEventHandler.HandleRest(RestMultiplier, RestMultiplierDelta, LustDecrease, ExhaustionDecrease, background);
        }
    }
}