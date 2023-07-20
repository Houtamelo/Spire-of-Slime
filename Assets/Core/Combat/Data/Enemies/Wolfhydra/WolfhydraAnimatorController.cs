using Core.Combat.Scripts.Behaviour.Rendering;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Objects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Data.Enemies.Wolfhydra
{
    public class WolfhydraAnimatorController : BaseCharacterAnimatorController
    {
        private const string LungeTrigger = "Lunge";
        private const string RoarTrigger = "Roar";

        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, LungeTrigger, RoarTrigger };
        private delegate (string variableName, CustomAudioSource[] soundsList)[] SoundsDebugDelegate(WolfhydraAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new (string variableName, CustomAudioSource[] soundsList)[]
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(lungeBaseSounds), controller.lungeBaseSounds),
                (nameof(lungeHitSounds), controller.lungeHitSounds),
                (nameof(roarBaseSounds), controller.roarBaseSounds),
                (nameof(roarHitSounds), controller.roarHitSounds)
            };
            
        [SerializeField]
        private bool allowValidation = true;
        
        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] lungeBaseSounds, lungeHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] roarBaseSounds, roarHitSounds;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case LungeTrigger:
                    if (lungeBaseSounds.HasElements())
                        lungeBaseSounds.GetRandom().Play();
                    
                    if (lungeHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        lungeHitSounds.GetRandom().Play();
                    break;
                case RoarTrigger:
                    if (roarBaseSounds.HasElements())
                        roarBaseSounds.GetRandom().Play();
                    
                    if (roarHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        roarHitSounds.GetRandom().Play();
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