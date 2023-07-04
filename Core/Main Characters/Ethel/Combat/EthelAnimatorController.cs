using Core.Combat.Scripts.Behaviour.Rendering;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Characters.Ethel.Combat.Skills.Challenge.Anim;
using Core.Main_Characters.Ethel.Combat.Skills.Poison_Dagger.Anim;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Objects;

namespace Core.Main_Characters.Ethel.Combat
{
    public class EthelAnimatorController : BaseCharacterAnimatorController
    {
        private const string ClashTrigger = "Clash";
        private const string JoltTrigger = "Jolt";
        private const string SafeguardTrigger = "Safeguard";
        private const string SeverTrigger = "Sever";
        private const string BaitTrigger = "Bait";
        private const string ChallengeTrigger = "Challenge";
        private const string PoisonDagger = "DaggerThrow";
        private const string PierceTrigger = "Pierce";
        private const string RiposteTrigger = "Riposte";

        private static readonly IReadOnlyCollection<string> TriggerDebugList = new[] { CombatAnimation.Param_Hit, ClashTrigger, JoltTrigger, SafeguardTrigger, SeverTrigger, BaitTrigger, ChallengeTrigger, PoisonDagger, PierceTrigger, RiposteTrigger };
        private delegate (string variableName, CustomAudioSource[] soundsList)[] SoundsDebugDelegate(EthelAnimatorController controller);

        private static readonly SoundsDebugDelegate SoundsDebugList = controller =>
            new (string variableName, CustomAudioSource[] soundsList)[]
            {
                (nameof(selfHitSounds), controller.selfHitSounds),
                (nameof(clashBaseSounds), controller.clashBaseSounds),
                (nameof(clashHitSounds), controller.clashHitSounds),
                (nameof(joltBaseSounds), controller.joltBaseSounds),
                (nameof(joltHitSounds), controller.joltHitSounds),
                (nameof(safeguardSounds), controller.safeguardSounds),
                (nameof(severBaseSounds), controller.severBaseSounds),
                (nameof(severHitSounds), controller.severHitSounds),
                (nameof(baitSounds), controller.baitSounds),
                (nameof(pierceBaseSounds), controller.pierceBaseSounds),
                (nameof(pierceHitSounds), controller.pierceHitSounds),
                (nameof(riposteBaseSounds), controller.riposteBaseSounds),
                (nameof(riposteHitSounds), controller.riposteHitSounds)
            };
            
        [SerializeField]
        private bool allowValidation = true;

        [SerializeField, Required]
        private CustomAudioSource[] selfHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] clashBaseSounds, clashHitSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] joltBaseSounds, joltHitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] safeguardSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] severBaseSounds, severHitSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] baitSounds;

        [SerializeField, Required]
        private CustomAudioSource[] pierceBaseSounds, pierceHitSounds;
        
        [SerializeField, Required]
        private CustomAudioSource[] riposteBaseSounds, riposteHitSounds;

        [SerializeField, Required]
        private PoisonDaggerAnimator daggerAnimator;
        
        [SerializeField, Required]
        private ChallengeAnimator challengeAnimator;

        public override void SetAnimation(in CombatAnimation combatAnimation)
        {
            animator.SetTrigger(combatAnimation.ParameterId);
            switch (combatAnimation.ParameterName)
            {
                case CombatAnimation.Param_Hit when selfHitSounds.HasElements():
                    if (combatAnimation.TargetContext.IsNone || combatAnimation.TargetContext.Value.Result.Hit)
                        selfHitSounds.GetRandom().PlayScheduled(AudioSettings.dspTime + CombatAnimation.SelfHitAudioDelay);
                    break;
                case ClashTrigger:
                    if (clashBaseSounds.HasElements())
                        clashBaseSounds.GetRandom().Play();
                    
                    if (clashHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out CasterContext actionContext) && actionContext.AnyHit)
                        clashHitSounds.GetRandom().Play();
                    
                    break;
                case JoltTrigger:
                    if (joltBaseSounds.HasElements())
                        joltBaseSounds.GetRandom().Play();
                    
                    if (joltHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        joltHitSounds.GetRandom().Play();
                    
                    break;
                case SafeguardTrigger:
                    if (safeguardSounds.HasElements())
                        safeguardSounds.GetRandom().Play();
                    
                    break;
                
                case SeverTrigger:
                    if (severBaseSounds.HasElements())
                        severBaseSounds.GetRandom().Play();
                    
                    if (severHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        severHitSounds.GetRandom().Play();
                    
                    break;
                case BaitTrigger:
                    if (baitSounds.HasElements())
                        baitSounds.GetRandom().Play();
                    
                    break;
                case ChallengeTrigger when combatAnimation.CasterContext.TrySome(out actionContext):
                    challengeAnimator.AnimateChallenge(actionContext);
                    break;
                case PoisonDagger when combatAnimation.CasterContext.TrySome(out actionContext):
                    daggerAnimator.AnimateDagger(actionContext);
                    break;
                case PierceTrigger:
                    if (pierceBaseSounds.HasElements())
                        pierceBaseSounds.GetRandom().Play();
                    
                    if (pierceHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        pierceHitSounds.GetRandom().Play();
                    
                    break;
                case RiposteTrigger:
                    if (riposteBaseSounds.HasElements())
                        riposteBaseSounds.GetRandom().Play();
                    
                    if (riposteHitSounds.HasElements() && combatAnimation.CasterContext.TrySome(out actionContext) && actionContext.AnyHit)
                        riposteHitSounds.GetRandom().Play();
                    
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