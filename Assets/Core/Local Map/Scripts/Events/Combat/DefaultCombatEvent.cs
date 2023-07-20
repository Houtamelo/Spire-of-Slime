using Core.Local_Map.Scripts.Enums;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Local_Map.Scripts.Events.Combat
{
    public class DefaultCombatEvent : ILocalMapEvent
    {
        public CleanString Key => "combat_default";
        public bool AllowSaving => true;

        [NotNull]
        public CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) => CombatEventHandler.HandleCombat(tileInfo, multiplier, Option.None);

        public IconType GetIconType(in Option<float> multiplier)
        {
            if (multiplier.IsNone)
                return IconType.NormalCombat;

            return multiplier.Value >= 1.4f ? IconType.EliteCombat : IconType.NormalCombat;
        }
    }
}