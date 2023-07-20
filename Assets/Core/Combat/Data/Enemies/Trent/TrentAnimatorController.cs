using Core.Combat.Scripts.Behaviour.Rendering;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Objects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Data.Enemies.Trent
{
    public class TrentAnimatorController : BaseCharacterAnimatorController
    {
        private const string SporesTrigger = "Spores";
        private const string AssembleTrigger = "Summon";
        private const string VineWhipTrigger = "VineWhip";

        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, SporesTrigger, AssembleTrigger, VineWhipTrigger };
        private delegate (string variableName, CustomAudioSource[] soundsList)[] SoundsDebugDelegate(TrentAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new (string variableName, CustomAudioSource[] soundsList)[]
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(sporesBaseSounds), controller.sporesBaseSounds),
                (nameof(sporesHitSounds), controller.sporesHitSounds),
                (nameof(assembleSounds), controller.assembleSounds),
                (nameof(vineWhipBaseSounds), controller.vineWhipBaseSounds),
                (nameof(vineWhipHitSounds), controller.vineWhipHitSounds)
            };
            
        [SerializeField]
        private bool allowValidation = true;
        
        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] sporesBaseSounds, sporesHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] assembleSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] vineWhipBaseSounds, vineWhipHitSounds;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case SporesTrigger:
                    if (sporesBaseSounds.HasElements())
                        sporesBaseSounds.GetRandom().Play();
                    
                    if (sporesHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        sporesHitSounds.GetRandom().Play();
                    break;
                case AssembleTrigger:
                    if (assembleSounds.HasElements())
                        assembleSounds.GetRandom().Play();
                    break;
                case VineWhipTrigger:
                    if (vineWhipBaseSounds.HasElements())
                        vineWhipBaseSounds.GetRandom().Play();
                    
                    if (vineWhipHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        vineWhipHitSounds.GetRandom().Play();
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (allowValidation == false)
                return;

            foreach ((string variableName, CustomAudioSource[] soundsList) in SoundsDebugList.Invoke(this))
            {
                if (soundsList.IsNullOrEmpty())
                    Debug.LogWarning($"No {variableName} sounds were assigned to {name}!", this);
            }

            if (animator == null)
                return;

            UnityEditor.Animations.AnimatorController animatorController = (UnityEditor.Animations.AnimatorController) animator.runtimeAnimatorController;
            List<string> missingTriggers = new(TriggerDebugList);
            foreach (AnimatorControllerParameter parameter in animatorController.parameters)
            {
                if (missingTriggers.Contains(parameter.name))
                    missingTriggers.Remove(parameter.name);
            }
            
            foreach (string missingTrigger in missingTriggers)
                Debug.LogWarning($"Missing trigger {missingTrigger} in {animator.name}!", this);
        }
#endif
    }
}