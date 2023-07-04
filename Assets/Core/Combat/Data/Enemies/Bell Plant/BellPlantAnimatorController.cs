using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour.Rendering;
using Core.Combat.Scripts.Skills.Action;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Objects;

namespace Core.Combat.Data.Enemies.Bell_Plant
{
    public class BellPlantAnimatorController : BaseCharacterAnimatorController
    {
        private const string LureTrigger = "Lure";
        private const string InvigoratingFluidsTrigger = "Heal";
        private const string VaporsTrigger = "Vapors";
        
        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, LureTrigger, InvigoratingFluidsTrigger, VaporsTrigger };
        private delegate List<(string variableName, CustomAudioSource[] soundsList)> SoundsDebugDelegate(BellPlantAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new List<(string variableName, CustomAudioSource[] soundsList)>
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(lureSounds), controller.lureSounds),
                (nameof(invigoratingFluidsSounds), controller.invigoratingFluidsSounds),
                (nameof(vaporsBaseSounds), controller.vaporsBaseSounds),
                (nameof(vaporsHitSounds), controller.vaporsHitSounds)
            };
        
        [SerializeField]
        private bool allowValidation = true;
        
        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] lureSounds;

        [SerializeField, Required]
        private CustomAudioSource[] invigoratingFluidsSounds;

        [SerializeField, Required]
        private CustomAudioSource[] vaporsBaseSounds, vaporsHitSounds;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case LureTrigger when lureSounds.HasElements():
                    lureSounds.GetRandom().Play(); break;
                case InvigoratingFluidsTrigger when invigoratingFluidsSounds.HasElements():
                    invigoratingFluidsSounds.GetRandom().Play(); break;
                case VaporsTrigger:
                    if (vaporsBaseSounds.HasElements())
                        vaporsBaseSounds.GetRandom().Play();
                    
                    if (vaporsHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        vaporsHitSounds.GetRandom().Play();
                    
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
            HashSet<string> missingTriggers = new(TriggerDebugList);
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