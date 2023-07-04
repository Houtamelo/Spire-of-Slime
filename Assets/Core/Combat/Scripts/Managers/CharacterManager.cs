using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.NemaExhaustion;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Characters.Nema.Combat;
using JetBrains.Annotations;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Collections;
using Utils.Extensions;
using Utils.Patterns;
using static Utils.Patterns.Option<Core.Combat.Scripts.Behaviour.CharacterStateMachine>;
using Save = Save_Management.Save;

namespace Core.Combat.Scripts.Managers
{
    public class CharacterManager : MonoBehaviour
    {
        [SerializeField, Required, AssetsOnly]
        private CharacterDisplay characterDisplayPrefab;
        
        [SerializeField, Required, SceneObjectsOnly]
        private Transform charactersParent;
        
        [SerializeField, Required, SceneObjectsOnly]
        private TimelineIconsManager timelineIconsManager;

        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;
        
        public event Action<CharacterStateMachine> CharacterSetup;

        private readonly ReadOnlyReference<IndexableHashSet<CharacterStateMachine>> _onLeftSide = new(new IndexableHashSet<CharacterStateMachine>());
        public FixedEnumerable<CharacterStateMachine> FixedOnLeftSide => _onLeftSide.Value.FixedEnumerate();
        public IndexableHashSet<CharacterStateMachine> GetLeftEditable() => _onLeftSide.Value;
        public int LeftSideCount => _onLeftSide.Value.Count;
        
        private readonly ReadOnlyReference<IndexableHashSet<CharacterStateMachine>> _onRightSide = new(new IndexableHashSet<CharacterStateMachine>());
        public FixedEnumerable<CharacterStateMachine> FixedOnRightSide => _onRightSide.Value.FixedEnumerate();
        public IndexableHashSet<CharacterStateMachine> GetRightEditable() => _onRightSide.Value;
        public int RightSideCount => _onRightSide.Value.Count;

        [MustUseReturnValue]
        public FixedEnumerable<CharacterStateMachine> GetOnSide(bool isLeftSide) => isLeftSide ? FixedOnLeftSide : FixedOnRightSide;
        
        public IndexableHashSet<CharacterStateMachine> GetEditable(bool isLeftSide) => isLeftSide ? _onLeftSide.Value : _onRightSide.Value;
        
        [MustUseReturnValue]
        public FixedEnumerable<CharacterStateMachine> GetOnSide(CharacterStateMachine character) => character.PositionHandler.IsLeftSide ? FixedOnLeftSide : FixedOnRightSide;
        
        [MustUseReturnValue]
        public FixedEnumerable<CharacterStateMachine> GetEnemies(CharacterStateMachine character) => character.PositionHandler.IsLeftSide ? FixedOnRightSide : FixedOnLeftSide;
        
        public int IndexOf(CharacterStateMachine character)
        {
            int index = _onLeftSide.Value.IndexOf(character);
            if (index != -1)
                return index;

            return _onRightSide.Value.IndexOf(character);
        }

        public int IndexOf(CharacterStateMachine character, bool isLeftSide)
        {
            IndexableHashSet<CharacterStateMachine> characters = isLeftSide ? _onLeftSide.Value : _onRightSide.Value;
            return characters.IndexOf(character);
        }

        [MustUseReturnValue]
        public CharacterEnumerator GetAllFixed() => new(characterManager: this);
        
        public delegate void DefeatedDelegate(CharacterStateMachine character, Option<CharacterStateMachine> lastDamager);
        public event DefeatedDelegate DefeatedEvent;

        public Option<CharacterStateMachine> GetByGuid(Guid guid)
        {
            foreach (CharacterStateMachine character in GetAllFixed())
                if (character.Guid == guid)
                    return Some(character);

            return None;
        }

    #region Setup
        private CharacterDisplay InstantiateDisplay()
        {
            CharacterDisplay display = characterDisplayPrefab.InstantiateWithFixedLocalScaleAndAnchoredPosition(charactersParent);
            display.SetCombatManager(combatManager, timelineIconsManager.CreateIcon());
            return display;
        }

        public CharacterStateMachine Create(ICharacterScript script, CombatSetupInfo.RecoveryInfo recoveryInfo, bool isLeftSide, Option<int> position, bool mistExists)
        {
            if (Save.AssertInstance(out Save save))
                save.SetVariable($"{VariablesName.EnemyMetPrefix.ToString()}{script.CharacterName}", true);
            
            CharacterDisplay display = InstantiateDisplay();
            CharacterStateMachine stateMachine = new(script, display, Guid.NewGuid(), isLeftSide, mistExists, recoveryInfo);
            List<CharacterStateMachine> sideList = isLeftSide ? _onLeftSide : _onRightSide;
            int index;
            if (position.IsSome)
            {
                sideList.Insert(position.Value, stateMachine);
                index = position.Value;
            }
            else
            {
                sideList.Add(stateMachine);
                index = sideList.Count - 1;
            }
            
            display.SetSortingOrder(1 - index % 2 - index);
            return stateMachine;
        }
        
        public bool SummonFromSkill(SummonToApply effectStruct)
        {
            bool isCasterLeft = effectStruct.Caster.PositionHandler.IsLeftSide;
            List<CharacterStateMachine> sideList = isCasterLeft ? _onLeftSide : _onRightSide;
            UnsubscribeDefeated();
            if (sideList.Count >= 4)
                return false;

            int casterIndex = sideList.IndexOf(effectStruct.Caster);
            Option<int> position = casterIndex != -1 ? Option<int>.Some(casterIndex) : Option<int>.None;
            CharacterStateMachine summoned = Create(script: effectStruct.CharacterToSummon, recoveryInfo: CombatSetupInfo.RecoveryInfo.Default, isLeftSide: isCasterLeft, position: position, combatManager.CombatSetupInfo.MistExists);
            CharacterDisplay display = summoned.Display.Value;
            summoned.ForceUpdateDisplay();
            display.SetBarsAlpha(0f);
            display.SetRendererAlpha(0f);
            display.MoveToDefaultPosition(baseDuration: Option<float>.None);
            CharacterSetup?.Invoke(summoned);
            
            IActionSequence currentAction = combatManager.Animations.CurrentAction;
            if (currentAction != null)
            {
                currentAction.UpdateCharactersStartPosition();
                currentAction.AddOutsider(summoned);
                currentAction.InstantMoveOutsideCharacters();
                return true;
            }

            Debug.LogWarning("Summoning character outside of action sequence", context: this);
            if (display != null)
            {
                display.SetBarsAlpha(1f);
                display.SetRendererAlpha(1f);
            }

            return true;
        }
        
        /// <returns> How many were created. </returns>
        public Option<Promise<int>> CreateOutsideSkill(ICharacterScript script, bool isLeftSide, Option<int> position, Option<int> createCount)
        {
            List<CharacterStateMachine> sideList = isLeftSide ? _onLeftSide : _onRightSide;
            UnsubscribeDefeated();
            if (sideList.Count >= 4)
                return Option<Promise<int>>.None;
            
            Promise<int> promise = new();
            Action underTheMist = () =>
            {
                createCount.TrySome(out int count);
                count = Mathf.Clamp(count, 1, 4);
                int successCount = 0;
                for (int i = 0; i < count && sideList.Count < 4; i++)
                {
                    CharacterStateMachine created = Create(script, recoveryInfo: CombatSetupInfo.RecoveryInfo.Default, isLeftSide, position, combatManager.CombatSetupInfo.MistExists);
                    created.ForceUpdateDisplay();
                    CharacterSetup?.Invoke(created);
                    successCount++;
                }
                
                promise.Resolve(Option<int>.Some(successCount));
            };

            combatManager.ActionAnimator.AnimateOverlayMist(underTheMist, None);
            return Option<Promise<int>>.Some(promise);
        }

        public void SetupCharacters(in CombatSetupInfo combatSetupInfo)
        {
            if (Save.AssertInstance(out Save save) == false)
            {
                Debug.LogError("Save instance is null", context: this);
                return;
            }
            
            for (int index = 0; index < combatSetupInfo.Allies.Length; index++)
            {
                (ICharacterScript script, CombatSetupInfo.RecoveryInfo recoveryInfo, _, bool bindToSave) = combatSetupInfo.Allies[index];
                CharacterStateMachine character = Create(script, recoveryInfo, isLeftSide: true, position: Option<int>.Some(index), combatSetupInfo.MistExists);
                if (bindToSave)
                {
                    Option<IReadonlyCharacterStats> statsOption = save.GetReadOnlyStats(script.Key);
                    if (statsOption.TrySome(out IReadonlyCharacterStats stats))
                        character.SyncStats(stats);
                }
                
                if (combatSetupInfo.MistExists && script.Key == Nema.GlobalKey)
                    NemaExhaustion.CreateInstance(float.MaxValue, true, character);
            }

            for (int index = 0; index < combatSetupInfo.Enemies.Length; index++)
            {
                (ICharacterScript script, CombatSetupInfo.RecoveryInfo recoveryInfo) enemyInfo = combatSetupInfo.Enemies[index];
                Create(enemyInfo.script, enemyInfo.recoveryInfo, isLeftSide: false, position: index, combatSetupInfo.MistExists);
            }
            
            foreach (CharacterStateMachine character in GetAllFixed())
                CharacterSetup?.Invoke(character);

            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: Option.None);

            foreach (CharacterStateMachine character in GetAllFixed())
                character.ForceUpdateDisplay();
        }

        public void SetupCharactersFromSave(CombatRecord record)
        {
            foreach (CharacterRecord characterRecord in record.Characters)
            {
                CharacterDisplay display = InstantiateDisplay();
                Option<CharacterStateMachine> stateMachine = CharacterStateMachine.FromSave(characterRecord, display);
                if (stateMachine.IsNone)
                {
                    Debug.LogWarning($"Failed to load character with guid {characterRecord.Guid} from save...");
                    Destroy(display.gameObject);
                    continue;
                }
                
                List<CharacterStateMachine> targetList = stateMachine.Value.PositionHandler.IsLeftSide ? _onLeftSide : _onRightSide;
                targetList.Add(stateMachine.Value);
            }

            foreach (CharacterRecord characterRecord in record.Characters)
            {
                Option<CharacterStateMachine> character = GetByGuid(characterRecord.Guid);
                if (character.IsNone)
                {
                    Debug.LogWarning($"Failed to find character with guid {characterRecord.Guid} while loading combat from save...");
                    continue;
                }
                
                foreach (PerkRecord perkRecord in characterRecord.Perks)
                    perkRecord.CreateInstance(character.Value, GetAllFixed());
            }
            
            foreach (CharacterRecord characterRecord in record.Characters)
            {
                Option<CharacterStateMachine> character = GetByGuid(characterRecord.Guid);
                if (character.IsNone)
                {
                    Debug.LogWarning($"Failed to find character with guid {characterRecord.Guid} while loading combat from save...");
                    continue;
                }
                
                foreach (StatusRecord statusRecord in characterRecord.Statuses)
                {
                    CombatUtils.CreateStatusInstanceFromRecord(record: statusRecord, owner: character.Value, GetAllFixed());
                }
            }

            foreach (CharacterRecord characterRecord in record.Characters)
            {
                PlanRecord planRecord = characterRecord.SkillAction;
                if (planRecord is not { Enqueued: false, IsDone: false })
                    continue;
                
                Option<PlannedSkill> skillAction = PlannedSkill.CreateInstance(record: planRecord, combatManager);
                if (skillAction.IsNone)
                {
                    Debug.LogWarning("Failed to create skill action from json while loading combat from save...");
                    continue;
                }
                
                Option<CharacterStateMachine> character = GetByGuid(characterRecord.Guid);
                if (character.IsNone)
                {
                    Debug.LogWarning($"Failed to find character with guid {characterRecord.Guid} while loading combat from save...");
                    continue;
                }
                    
                character.Value.SkillModule.SetActionWithoutNotify(action: skillAction.Value);
            }

            foreach (CharacterStateMachine character in GetAllFixed())
                CharacterSetup?.Invoke(character);

            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: Option<float>.None);
            
            foreach (CharacterStateMachine character in GetAllFixed())
                character.ForceUpdateDisplay();
        }
    #endregion

    #region StateMachine
        public void PerformTimeStep(float timeStep)
        {
            foreach (CharacterStateMachine character in GetAllFixed())
                character.SecondaryTick(timeStep);
            
            foreach (CharacterStateMachine character in GetAllFixed())
                character.PrimaryTick(timeStep);

            foreach (CharacterStateMachine character in GetAllFixed())
                character.AfterTickUpdate(timeStep);
        }
        
        public bool RequestSideSwitch(CharacterStateMachine requester)
        {
            if (requester.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return false;

            UnsubscribeDefeated();
            List<CharacterStateMachine> desiredList = requester.PositionHandler.IsLeftSide ? _onRightSide : _onLeftSide;
            if (desiredList.Count >= 4)
                return false;
            
            List<CharacterStateMachine> sourceList = requester.PositionHandler.IsLeftSide ? _onLeftSide : _onRightSide;
            sourceList.Remove(requester);
            desiredList.Add(requester);
            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: PositionManager.CharacterMoveDuration);
            return true;
        }

        public void UnsubscribeDefeated()
        {
            foreach (CharacterStateMachine character in FixedOnLeftSide)
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated)
                    continue;

                character.Unsubscribe();
                _onLeftSide.Value.Remove(character);
            }
            
            foreach (CharacterStateMachine character in FixedOnRightSide)
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated)
                    continue;

                character.Unsubscribe();
                _onRightSide.Value.Remove(character);
            }
        }

        public void NotifyStunned(CharacterStateMachine character)
        {
            float chargeInitialDuration = character.ChargeModule.GetInitialDuration();
            if (chargeInitialDuration > 0)
            {
                float chargeRemaining = character.ChargeModule.GetRemaining();
                float chargePenalty = Mathf.Min(character.StunModule.GetRemaining(), chargeInitialDuration - chargeRemaining);
                chargePenalty = Mathf.Min(chargePenalty, chargeInitialDuration * 0.3333333f);
                if (chargePenalty > 0)
                {
                    character.ChargeModule.SetBoth(chargeInitialDuration, chargeRemaining + chargePenalty);
                }
            }

            combatManager.Animations.CancelActionsOfCharacter(character, compensateChargeLost: false);
            foreach (StatusInstance status in character.StatusModule.GetAll)
                if (status is Riposte riposte)
                    riposte.RequestDeactivation();

            foreach (StatusInstance status in character.StatusModule.GetAllRelated)
            {
                if (status is LustGrappled lustGrappled && lustGrappled.Restrainer == character)
                {
                    lustGrappled.RestrainerStunned();
                }
                else if (status is Guarded guarded && guarded.Caster == character)
                {
                    status.RequestDeactivation();
                }
            }
        }

        public void NotifyDowned(CharacterStateMachine character)
        {
            character.SkillModule.CancelPlan(compensateChargeLost: false);
            combatManager.Animations.CancelActionsOfCharacter(character, compensateChargeLost: false);
            //combatManager.Animations.CancelLustPromptsOfActiveCharacter(character);
            foreach (StatusInstance status in character.StatusModule.GetAll)
                if (status is Riposte riposte)
                    riposte.RequestDeactivation();

            foreach (StatusInstance status in character.StatusModule.GetAllRelated)
                if ((status is LustGrappled lustGrappled && lustGrappled.Restrainer == character) || status is Guarded guarded && guarded.Caster == character)
                    status.RequestDeactivation();

            character.RecoveryModule.Reset();
            character.ChargeModule.Reset();
            if (character.Script.Key == Nema.GlobalKey && Save.AssertInstance(out Save save))
                save.CheckNemaCombatStatus();
        }

        public void NotifyGrappled(CharacterStateMachine character)
        {
            character.SkillModule.CancelPlan(compensateChargeLost: false);
            combatManager.Animations.CancelActionsOfCharacter(character, compensateChargeLost: false);
            //combatManager.Animations.CancelLustPromptsOfActiveCharacter(character);
            foreach (StatusInstance status in character.StatusModule.GetAll)
                if (status is Riposte riposte)
                    riposte.RequestDeactivation();

            foreach (StatusInstance status in character.StatusModule.GetAllRelated)
                if ((status is LustGrappled lustGrappled && lustGrappled.Restrainer == character) || status is Guarded guarded && guarded.Caster == character)
                    status.RequestDeactivation();

            if (character.DownedModule.TrySome(out IDownedModule downedModule))
                downedModule.Reset();
            
            character.RecoveryModule.Reset();
            character.ChargeModule.Reset();
            if (character.Script.Key == Nema.GlobalKey && Save.AssertInstance(out Save save))
                save.CheckNemaCombatStatus();
            
            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: PositionManager.CharacterMoveDuration);
        }

        public void NotifyGrappling(CharacterStateMachine restrainer, CharacterStateMachine target)
        {
            restrainer.SkillModule.CancelPlan(compensateChargeLost: false);
            combatManager.Animations.CancelActionsOfCharacter(restrainer, compensateChargeLost: false);
            foreach (StatusInstance status in restrainer.StatusModule.GetAll)
                if (status is Riposte riposte)
                    riposte.RequestDeactivation();

            foreach (StatusInstance status in restrainer.StatusModule.GetAllRelated)
                if ((status is LustGrappled lustGrappled && lustGrappled.Restrainer == restrainer && lustGrappled.Owner != target) || (status is Guarded guarded && guarded.Caster == restrainer))
                    status.RequestDeactivation();
            
            restrainer.RecoveryModule.Reset();
            restrainer.ChargeModule.Reset();
        }

        public void NotifyDefeated(CharacterStateMachine defeated, Option<CharacterStateMachine> lastDamager, bool becomesCorpseOnDefeat)
        {
            Save save = Save.Current;
            if (save != null && defeated.PositionHandler.IsRightSide)
                save.AwardExperienceFromDefeatedEnemy(defeated.Script.Key);

            foreach (CharacterStateMachine character in GetAllFixed())
                character.SkillModule.UnplanIfTargeting(defeated);
            
            combatManager.Animations.CancelActionsOfCharacter(defeated, compensateChargeLost: false);
            defeated.SkillModule.CancelPlan(compensateChargeLost: false);

            foreach (CharacterStateMachine character in GetAllFixed())
                foreach (StatusInstance status in character.StatusModule.GetAll)
                    status.CharacterDefeated(defeated, becomesCorpseOnDefeat);

            if (becomesCorpseOnDefeat == false)
            {
                _onLeftSide.Value.Remove(defeated);
                _onRightSide.Value.Remove(defeated);
            }

            if (lastDamager.IsSome)
                lastDamager.Value.PlayBark(BarkType.DefeatedEnemy);
            
            DefeatedEvent?.Invoke(defeated, lastDamager);
            combatManager.PositionManager.MoveAllToDefaultPosition(baseDuration: PositionManager.CharacterMoveDuration);
        }
    #endregion
        
        public Option<CharacterDisplay> GetCharacterOverMouse()
        {
            foreach (CharacterStateMachine character in GetAllFixed())
                if (character.Display.TrySome(out CharacterDisplay display) && display.IsMouseOver())
                    return Option<CharacterDisplay>.Some(display);

            return Option<CharacterDisplay>.None;
        }

        public Option<CharacterStateMachine> GetCharacterAt(int position, bool isLeftSide)
        {
            List<CharacterStateMachine> list = isLeftSide ? _onLeftSide : _onRightSide;
            if (position >= list.Count)
                return None;

            return Some(list[position]);
        }

        public bool AnyBarkPlaying()
        {
            foreach (CharacterStateMachine character in GetAllFixed())
                if (character.Display.TrySome(out CharacterDisplay display) && display.gameObject.activeInHierarchy && display.BarkPlayer.IsBusy)
                    return true;

            return false;
        }

        public void StopAllBarks()
        {
            foreach (CharacterStateMachine character in GetAllFixed())
                if (character.Display.TrySome(out CharacterDisplay display))
                    display.BarkPlayer.Stop();
        }
    }
}