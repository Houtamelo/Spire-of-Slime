/*using Core.Combat.Scripts.Behaviour;
using JetBrains.Annotations;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.VisualPrompts
{
    public class LustPromptRequest
    {
        private readonly string _animationTrigger;
        private readonly float _graphicalX;
        public readonly CharacterStateMachine PassiveCharacter;
        public readonly CharacterStateMachine ActiveCharacter;
        public readonly int Lust;
        public Outcome Result { get; private set; }
        public bool IsDone { get; private set; }
        private bool _resolved;
        
        private LustPromptRequest(CharacterStateMachine passiveCharacter, CharacterStateMachine activeCharacter, string animationTrigger, float graphicalX)
        {
            _animationTrigger = animationTrigger;
            _graphicalX = graphicalX;
            PassiveCharacter = passiveCharacter;
            ActiveCharacter = activeCharacter;
            if (passiveCharacter.LustModule.IsSome)
                Lust = passiveCharacter.LustModule.Value.Current;
            else
            {
                Debug.LogWarning($"Trying to create a lust prompt request with a character that has no lust module.");
                Lust = 0;
            }
        }
        
        public static Option<LustPromptRequest> Create([NotNull] CharacterStateMachine passive, [NotNull] CharacterStateMachine active, string parameter, float graphicalX)
        {
            if (passive.LustModule.IsNone)
                return Option<LustPromptRequest>.None;
            
            return Option<LustPromptRequest>.Some(new LustPromptRequest(passive, active, parameter, graphicalX));
        }

        public void SetDone(Outcome result)
        {
            if (IsDone)
            {
                Debug.LogWarning($"Trying to resolve a prompt request that has already been resolved.");
                return;
            }

            Result = result;
            IsDone = true;
        }

        public void Resolve()
        {
            if (!IsDone || _resolved)
            {
                Debug.LogWarning($"Trying to execute resolve on a prompt request that is done or already resolved. Done: {IsDone.ToString()}, Resolved: {_resolved.ToString()}");
                return;
            }
            
            _resolved = true;
            if (Result is Outcome.Canceled)
                return;
            
            LustPromptOutcomeStruct lustOutcome = new(PassiveCharacter, ActiveCharacter, Result, _animationTrigger, _graphicalX);
            Option<ILustPromptResolver> resolver = PassiveCharacter.LustModule.Value.GetPromptResolver();
            if (resolver.IsSome)
                resolver.Value.Resolve(lustOutcome);
            else
                DefaultOutComeResolver.Resolve(lustOutcome);
        }

        public enum Outcome
        {
            Failure,
            Success,
            Perfect,
            Canceled
        }
    }
}*/