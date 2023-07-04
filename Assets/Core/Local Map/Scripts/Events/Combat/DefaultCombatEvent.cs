using Core.Local_Map.Scripts.Enums;
using Save_Management;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.Combat
{
    public class DefaultCombatEvent : ILocalMapEvent
    {
        public CleanString Key => "combat_default";
        public bool AllowSaving => true;

        public CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier)
        {
            return CombatEventHandler.HandleCombat(tileInfo, multiplier, Option.None);
        }

        public IconType GetIconType(in Option<float> multiplier)
        {
            if (multiplier.IsNone)
                return IconType.NormalCombat;

            return multiplier.Value >= 1.4f ? IconType.EliteCombat : IconType.NormalCombat;
        }
    }
}