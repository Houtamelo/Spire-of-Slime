using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour.Rendering;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Objects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Data.Enemies.Crabdra
{
    public class CrabdraAnimatorController : BaseCharacterAnimatorController
    {
        private const string CrushTrigger = "Claw";
        private const string GlareTrigger = "Glare";
        
        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, CrushTrigger, GlareTrigger };
        private delegate (string variableName, CustomAudioSource[] soundsList)[] SoundsDebugDelegate(CrabdraAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new (string variableName, CustomAudioSource[] soundsList)[]
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(crushBaseSounds), controller.crushBaseSounds),
                (nameof(crushHitSounds), controller.crushHitSounds),
                (nameof(glareBaseSounds), controller.glareBaseSounds),
                (nameof(glareHitSounds), controller.glareHitSounds)
            };
            
        [SerializeField]
        private bool allowValidation = true;
        
        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] crushBaseSounds, crushHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] glareBaseSounds, glareHitSounds;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case CrushTrigger:
                    if (crushBaseSounds.HasElements())
                        crushBaseSounds.GetRandom().Play();
                    
                    if (crushHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        crushHitSounds.GetRandom().Play();
                    break;
                case GlareTrigger:
                    if (glareBaseSounds.HasElements())
                        glareBaseSounds.GetRandom().Play();
                    
                    if (glareHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        glareHitSounds.GetRandom().Play();
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