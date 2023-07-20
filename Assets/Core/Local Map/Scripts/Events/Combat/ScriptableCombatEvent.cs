using Core.Local_Map.Scripts.Enums;
using Core.Utils.Async;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Local_Map.Scripts.Events.Combat
{
    public class ScriptableCombatEvent : ScriptableLocalMapEvent
    {
        public override bool AllowSaving => true;

        [NotNull]
        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) => CombatEventHandler.HandleCombat(tileInfo, multiplier, Option.None);

        public override IconType GetIconType(in Option<float> multiplier)
        {
            if (multiplier.IsNone)
                return IconType.NormalCombat;

            return multiplier.Value > 1.5f ? IconType.EliteCombat : IconType.NormalCombat;
        }
    }
}