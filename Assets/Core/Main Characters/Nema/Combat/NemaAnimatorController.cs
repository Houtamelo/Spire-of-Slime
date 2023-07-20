using Core.Combat.Scripts.Behaviour.Rendering;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Characters.Nema.Combat.Skills.Calm.Anim;
using Core.Main_Characters.Nema.Combat.Skills.Serenity.Anim;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Objects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Characters.Nema.Combat
{
    public class NemaAnimatorController : BaseCharacterAnimatorController
    {
        private const string GawkyTrigger = "Gawky";
        private const string FocusTrigger = "Focus";
        private const string CalmTrigger = "Calm";
        private const string SerenityTrigger = "Serenity";
        private const string WoeTrigger = "Woe";
        private const string PrideTrigger = "Pride";
        private const string WrathTrigger = "Wrath";

        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, GawkyTrigger, FocusTrigger, CalmTrigger, SerenityTrigger, WoeTrigger, PrideTrigger, WrathTrigger };
        private delegate (string variableName, CustomAudioSource[] soundsList)[] SoundsDebugDelegate(NemaAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new (string variableName, CustomAudioSource[] soundsList)[]
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(gawkyBaseSounds), controller.gawkyBaseSounds),
                (nameof(gawkyHitSounds), controller.gawkyHitSounds),
                (nameof(focusBaseSounds), controller.focusBaseSounds),
                (nameof(focusHitSounds), controller.focusHitSounds),
                (nameof(woeBaseSounds), controller.woeBaseSounds),
                (nameof(woeHitSounds), controller.woeHitSounds),
                (nameof(prideBaseSounds), controller.prideBaseSounds),
                (nameof(prideHitSounds), controller.prideHitSounds),
            };
            
        [SerializeField]
        private bool allowValidation = true;
        
        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] gawkyBaseSounds, gawkyHitSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] focusBaseSounds, focusHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] woeBaseSounds, woeHitSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] prideBaseSounds, prideHitSounds;

        [SerializeField, Required]
        private CalmAnimator calmAnimator;

        [SerializeField, Required]
        private SerenityAnimator serenityAnimator;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case GawkyTrigger:
                    if (gawkyBaseSounds.HasElements())
                        gawkyBaseSounds.GetRandom().Play();
                    
                    if (gawkyHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        gawkyHitSounds.GetRandom().Play();
                    break;
                case FocusTrigger:
                    if (focusBaseSounds.HasElements())
                        focusBaseSounds.GetRandom().Play();
                    
                    if (focusHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        focusHitSounds.GetRandom().Play();
                    break;
                case CalmTrigger:
                    calmAnimator.AnimateCalm(combatAnimation.CasterContext);
                    break;
                case SerenityTrigger:
                    serenityAnimator.AnimateSerenity(combatAnimation.CasterContext);
                    break;
                case WoeTrigger:
                    if (woeBaseSounds.HasElements())
                        woeBaseSounds.GetRandom().Play();
                    
                    if (woeHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        woeHitSounds.GetRandom().Play();
                    break;
                case PrideTrigger:
                    if (prideBaseSounds.HasElements())
                        prideBaseSounds.GetRandom().Play();
                    
                    if (prideHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        prideHitSounds.GetRandom().Play();
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