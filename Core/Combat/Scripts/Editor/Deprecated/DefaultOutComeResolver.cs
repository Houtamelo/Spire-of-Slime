/*using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Interfaces.Modules;
using UnityEngine;

namespace Core.Combat.Scripts.VisualPrompts
{
    public static class DefaultOutComeResolver
    {
        private static readonly LustGrappledScript LustGrappledScript = new(Permanent: false, BaseDuration: 5f, TriggerName: string.Empty, GraphicalX: 0f, BaseLustPerTime: 12);

        public static void Resolve(in LustPromptOutcomeStruct lustPromptOutcomeStruct)
        {
            CharacterStateMachine girl = lustPromptOutcomeStruct.Passive;
            switch (lustPromptOutcomeStruct.Outcome)
            {
                case LustPromptRequest.Outcome.Failure:
                    LustGrappledToApply record = (LustGrappledToApply)LustGrappledScript.GetStatusToApply(caster: lustPromptOutcomeStruct.Active,
                        target: girl, crit: false, skill: null);
                    record.TriggerName = lustPromptOutcomeStruct.AnimationTrigger;
                    record.GraphicalX = lustPromptOutcomeStruct.GraphicalX;
                    record.ApplyEffect();
                    break;
                case LustPromptRequest.Outcome.Success:
                    break;
                case LustPromptRequest.Outcome.Perfect:
                    if (girl.LustModule.TrySome(out ILustModule lustModule))
                    {
                        int delta = -Mathf.CeilToInt(lustModule.Current * 0.075f);
                        lustModule.Change(delta);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown outcome enum {lustPromptOutcomeStruct.Outcome}");
            }
        }
            
    }
}*/