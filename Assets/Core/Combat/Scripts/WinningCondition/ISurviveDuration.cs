using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers;
using UnityEngine;

namespace Core.Combat.Scripts.WinningCondition
{
    public interface ISurviveDuration : IWinningCondition { }
    
    //fucking c#, COMPOSITION >>>>>>>> INHERITANCE
    public static class DefaultSurviveDurationImplementation
    {
        public static CombatStatus DefaultTick<T>(this T _, CombatManager combatManager, in float duration) where T : ISurviveDuration
        {
            bool anyLeftSideAlive = false;
            bool anyRightSideAlive = false;
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.FullPureEvaluate() is { Corpse: true } or { Defeated: true })
                    continue;

                if (character.PositionHandler.IsLeftSide)
                    anyLeftSideAlive = true;
                else
                    anyRightSideAlive = true;
                
                if (anyLeftSideAlive && anyRightSideAlive)
                    break;
            }

            if (anyLeftSideAlive == false)
                return CombatStatus.RightSideWon;

            if (combatManager.ElapsedTime >= duration || anyRightSideAlive == false)
                return CombatStatus.LeftSideWon;

            return CombatStatus.InProgress;
        }

        public static string DefaultDisplayName<T>(this T _, in float duration) where T : ISurviveDuration
        {
            return $"Survive for {duration.ToString("0")} seconds";
        }

        public static float DefaultTimeToDisplay<T>(this T _, CombatManager combatManager, in float duration) where T : ISurviveDuration
        {
            return Mathf.Clamp(duration - combatManager.ElapsedTime, 0f, duration);
        }
    }
}