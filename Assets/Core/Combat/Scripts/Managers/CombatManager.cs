﻿using System;
using System.Collections;
using System.Collections.Generic;
using Core.Audio.Scripts;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Timeline;
using Core.Combat.Scripts.UI;
using Core.Combat.Scripts.WinningCondition;
using Core.Game_Manager.Scripts;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Combat;
using Core.Misc;
using Core.Pause_Menu.Scripts;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Handlers;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Managers
{
    public class CombatManager : Singleton<CombatManager>
    {
        //public static TSpan TimePerStep => TSpan.FromSeconds(1.0 / PauseMenuManager.CombatTickRateHandler.Value);
        
        private static readonly TSpan IntervalBetweenExhaustionIncrease = TSpan.FromSeconds(1.3);
        
        public const float WinningConditionAnnounceDuration = 3f;
        public const float DefaultAnnounceDelay = 1f;

        // ReSharper disable once InconsistentNaming
        ///<summary> Allows you to control enemies. </summary>
        public static bool DEBUGMODE;
        
        [SerializeField, Required, SceneObjectsOnly]
        private Transform worldSpaceCamera;

        [SerializeField, Required, SceneObjectsOnly]
        private SpriteRenderer uiFill;

        [SerializeField, Required, SceneObjectsOnly]
        private PixelPerfectWithZoom cameraZoomController;

        [SerializeField, Required, SceneObjectsOnly] 
        private Announcer announcer;
        public Announcer Announcer => announcer;

        [SerializeField, Required, SceneObjectsOnly]
        private CombatInputManager inputHandler;
        public CombatInputManager InputHandler => inputHandler;

        [SerializeField, Required, SceneObjectsOnly]
        private LazyAnimationQueue animations;
        public LazyAnimationQueue Animations => animations;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterManager characters;
        public CharacterManager Characters => characters;
        
        [SerializeField, Required, SceneObjectsOnly]
        private PositionManager positionManager;
        public PositionManager PositionManager => positionManager;
        
        [SerializeField, Required, SceneObjectsOnly]
        private ExperienceScreen experienceScreen;
        
        [SerializeField, Required, SceneObjectsOnly]
        private ActionAnimator actionAnimator;
        public ActionAnimator ActionAnimator => actionAnimator;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text timeTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource announceSkillSound;

        [SerializeField, Required, SceneObjectsOnly]
        private Transform overlayAnimatorsParent;
        public Transform OverlayAnimatorsParent => overlayAnimatorsParent;

        [SerializeField, Required, SceneObjectsOnly]
        private TimelineManager timelineManager;
        public TimelineManager TimelineManager => timelineManager;

        [NonSerialized]
        public readonly FloatHandler SpeedHandler = new();

        [NonSerialized] 
        public readonly ValueHandler<bool> PauseHandler = new();

        public event Action RelevantPropertiesChanged;
        public event Action OnCombatBegin;

        public Option<CombatBackground> Background { get; private set; }

        public TSpan ElapsedTime { get; private set; }
        public TSpan AccumulatedExhaustionTime { get; private set; }

        public bool Running { get; private set; }
        public IWinningCondition WinningCondition { get; private set; }
        public CombatSetupInfo CombatSetupInfo { get; private set; }
        public Option<CombatTracker> Tracker { get; private set; }

        private Vector3 _defaultWorldSpaceCameraPosition;

        private void Start()
        {
            _defaultWorldSpaceCameraPosition = worldSpaceCamera.transform.position;
            RelevantPropertiesChanged += OnRelevantPropertiesChanged;
            
            if (InputManager.AssertInstance(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.PauseCombat].Add(TogglePauseInputReceived);
        }

        protected override void OnDestroy()
        {
            if (InputManager.Instance.TrySome(out InputManager inputManager))
                inputManager.PerformedActionsCallbacks[InputEnum.PauseCombat].Remove(TogglePauseInputReceived);
            
            this.DOKill();
            base.OnDestroy();
        }

        private void Update()
        {
            PerformTimeStep();
        }

        public void PauseTime()
        {
            if (PauseHandler.Value == false)
                PauseHandler.SetValue(true);
        }

        public void UnPauseTime()
        {
            if (PauseHandler.Value == true)
                PauseHandler.SetValue(false);
        }

        private void TogglePauseInputReceived()
        {
            if (DialogueController.Instance.TrySome(out DialogueController dialogueController) && dialogueController.IsDialogueRunning) // advancing dialogue also uses space bar
                return;
            
            TogglePause();
        }

        public void TogglePause()
        {
            if (PauseHandler.Value)
                UnPauseTime();
            else
                PauseTime();
        }

        [NotNull]
        public CombatRecord GenerateRecord() => CombatRecord.FromCombat(combatManager: this);

        public void AnnouncePlan([NotNull] PlannedSkill plan, float startDuration, float popDuration)
        {
            announceSkillSound.Play();
            announcer.Announce(plan, startDuration, popDuration, IActionSequence.SpeedMultiplier);
        }

        public void Announce(string text, Option<float> delay, float totalDuration) => announcer.Announce(text, delay, totalDuration, IActionSequence.SpeedMultiplier);

        private void PerformTimeStep()
        {
            if (CanKeepStepping() == false)
                return;
            
            if (Save.AssertInstance(out Save save) == false)
                return;

            CombatEvent nextEvent = timelineManager.UpdateEvents();
            TSpan timeStep = nextEvent.Time;
            
            ElapsedTime += timeStep;
            timeTmp.text = WinningCondition.GetTimeToDisplay().Seconds.ToString("0.00");

            if (CombatSetupInfo.MistExists)
            {
                NemaStatus nemaStatus = save.GetFullNemaStatus();
                if (nemaStatus.SetToClearMist.current && nemaStatus.IsInCombat.current && nemaStatus.IsStanding.current && nemaStatus.Exhaustion.current < NemaStatus.HighExhaustion)
                    AccumulatedExhaustionTime += timeStep;

                int exhaustionTicks = (int)(AccumulatedExhaustionTime.Seconds / IntervalBetweenExhaustionIncrease.Seconds);
                AccumulatedExhaustionTime -= TSpan.FromSeconds(exhaustionTicks * IntervalBetweenExhaustionIncrease.Seconds);
                save.ChangeNemaExhaustion(delta: exhaustionTicks * 1);
            }
            
            characters.PerformTimeStep(timeStep);

            RelevantPropertiesChanged?.Invoke();
        }

        private bool CanKeepStepping()
        {
            if (PauseHandler.Value || Running == false || announcer.IsBusy)
                return false;

            CombatStatus status = WinningCondition.Evaluate();
            
            if (status != CombatStatus.InProgress)
            {
                Running = false;
                PauseTime();
                StartCoroutine(EndRoutine(status));
                return false;
            }

            if (animations.Tick())
                return false;

            if (PauseHandler.Value || Running == false)
                return false;

            foreach (CharacterStateMachine character in characters.FixedOnLeftSide)
            {
                if (character.StateEvaluator.PureEvaluate() is CharacterState.Idle) // can step time if it's the player's turn
                    return false;
            }
            
            return true;
        }

        private IEnumerator EndRoutine(CombatStatus status)
        {
            characters.StopAllBarks();
            if (Save.AssertInstance(out Save save) == false || Tracker.AssertSome(out CombatTracker tracker) == false)
                yield break;
            
            tracker.AboutToFinish(status);

            if (CombatSetupInfo.Allies.IsNullOrEmpty())
            {
                tracker.SetDone(status, valid: true);
                yield break;
            }

            (ICharacterScript script, CombatSetupInfo.RecoveryInfo, int expAtStart, bool bindToSave)[] allies = CombatSetupInfo.Allies;
            List<(ICharacterScript script, int startExp, int currentExp)> saveBoundAllies = new(allies.Length);
            Option<int> expectedEarnings = Option.None;
            for (int index = 0; index < allies.Length; index++)
            {
                (ICharacterScript script, _, int startExp, bool bindToSave) = allies[index];
                if (bindToSave == false)
                    continue;
                
                Option<IReadonlyCharacterStats> statsOption = save.GetReadOnlyStats(script.Key);
                if (statsOption.AssertSome(out IReadonlyCharacterStats stats))
                {
                    int currentExp = stats.TotalExperience;
                    expectedEarnings = Option<int>.Some(currentExp - startExp);
                    break;
                }
            }

            for (int index = 0; index < allies.Length; index++)
            {
                (ICharacterScript script, _, int startExp, bool bindToSave) = allies[index];
                if (bindToSave == false)
                    continue;
                
                Option<IReadonlyCharacterStats> statsOption = save.GetReadOnlyStats(script.Key);
                if (statsOption.AssertSome(out IReadonlyCharacterStats stats) == false)
                    continue;

                int currentExp = stats.TotalExperience;
                saveBoundAllies.Add((script, startExp, currentExp));
                
                int earnedExp = currentExp - startExp;
                if (earnedExp != expectedEarnings.Value)
                    Debug.LogWarning($"Character {allies[0].script.CharacterName} earned {expectedEarnings.Value} exp but {script.CharacterName} earned {earnedExp} exp!");
            }

            foreach ((ICharacterScript script, _, _) in saveBoundAllies)
            {
                if (save.GetReadOnlyStats(script.Key).AssertSome(out IReadonlyCharacterStats stats) == false)
                    continue;

                // characters get a temptation reduction upon winning combat, but only if they weren't defeated (currently defeat is achieved by reaching the orgasm limit)
                // otherwise, they just get one orgasm "restoration" so that they can participate on the next combat.
                if (stats.OrgasmLimit > stats.OrgasmCount)
                {
                    int temptation = stats.Temptation;
                    temptation = temptation - 5 + (temptation / 5);
                    save.SetTemptation(stats.Key, temptation);
                }
                else
                {
                    save.SetOrgasmCount(stats.Key, stats.OrgasmLimit - 1);
                }
            }
            
            experienceScreen.Play(saveBoundAllies, onContinueClicked: () => tracker.SetDone(status, valid: true));
        }
        
        /// <summary> Does not start automatically. </summary>
        [NotNull]
        public CoroutineWrapper CameraZoomAt(float zoomLevel, Vector3 worldPosition, float zoomDuration, float stayDuration)
        {
            worldPosition.z = _defaultWorldSpaceCameraPosition.z;
            return new CoroutineWrapper(ZoomRoutine(zoomLevel, worldPosition, zoomDuration, stayDuration), nameof(ZoomRoutine), context: this, autoStart: false);
        }

        private IEnumerator ZoomRoutine(float zoomLevel, Vector3 worldPosition, float zoomDuration, float stayDuration)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(worldSpaceCamera.DOMove(worldPosition, zoomDuration));
            sequence.AppendInterval(stayDuration);
            sequence.Append(worldSpaceCamera.DOMove(_defaultWorldSpaceCameraPosition, zoomDuration));

            Coroutine zoomRoutine = StartCoroutine(cameraZoomController.TweenZoom(zoomLevel, zoomDuration, useUnscaledTime: false));
                
            yield return sequence.WaitForCompletion();
            yield return zoomRoutine;
                
            zoomRoutine = StartCoroutine(cameraZoomController.TweenZoom(1f, zoomDuration, useUnscaledTime: false));
            yield return zoomRoutine;
        }

        private void OnRelevantPropertiesChanged()
        {
            bool isPlayerLosing = false;
            foreach (CharacterStateMachine ally in characters.FixedOnLeftSide)
            {
                if (ally.Script is Nema or Ethel && (ally.StateEvaluator.PureEvaluate() is CharacterState.Grappled or CharacterState.Downed or CharacterState.Corpse or CharacterState.Defeated ||
                                                     (ally.StaminaModule.IsSome && ally.StaminaModule.Value.Percentage() <= 0.25f)))
                {
                    isPlayerLosing = true;
                    break;
                }
            }

            MusicEvent targetEvent = isPlayerLosing ? MusicEvent.Combat : MusicEvent.CombatLosing;
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.NotifyEvent(targetEvent);
        }

        public void SetupCombatFromBeginning(in CombatSetupInfo combatSetupInfo, CombatTracker tracker, [NotNull] WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey)
        {
            if (Tracker.TrySome(out CombatTracker oldTracker) && oldTracker is { IsDone: false })
                Debug.LogWarning("Trying to setup new combat without the previous one ending, this should not happen", this);

            CombatSetupInfo = combatSetupInfo;

            Background = BackgroundDatabase.SpawnBackground(worldSpaceCamera, backgroundKey);
            if (Background.AssertSome(out CombatBackground background))
                background.Generate();
            
            uiFill.color = Background.Value.FillColor;

            characters.SetupCharacters(combatSetupInfo);

            ElapsedTime = TSpan.Zero;

            Running = true;
            Tracker = tracker;
            WinningCondition = winningConditionGenerator.GenerateCondition(combatManager: this);
            timeTmp.text = WinningCondition.GetTimeToDisplay().ToString("0.00");
            
            Announce(WinningCondition.DisplayName, delay: Option<float>.Some(DefaultAnnounceDelay), WinningConditionAnnounceDuration);
            OnCombatBegin?.Invoke();

            SavePoint.RecordCombatStart();
            
            UnPauseTime();
        }

        public void SetupCombatFromSave(CombatRecord record, CombatTracker.FinishRecord onFinish)
        {
            if (GameManager.AssertInstance(out GameManager gameManager) == false)
                return;

            if (Running)
            {
                Debug.LogWarning("Combat is already setup while trying to load combat from a save, this should not be happening, returning to main menu...", this);
                gameManager.PauseMenuToMainMenu();
                return;
            }

            if (record.Background != null)
            {
                Option<CombatBackground> prefab = BackgroundDatabase.GetBackgroundPrefab(record.Background.Key);
                if (prefab.IsSome)
                {
                    Background = Instantiate(prefab.Value, worldSpaceCamera, worldPositionStays: true);
                    Background.Value.GenerateFromData(backgroundData: record.Background);
                    uiFill.color = Background.Value.FillColor;
                }
                else
                {
                    Debug.LogWarning($"Failed to find background with key {record.Background.Key} while loading combat from save...");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Failed to load background generator from saved data, the combat will still work but there will be no background.");
            }

            Option<CombatSetupInfo> setupInfo = CombatSetupInfo.FromRecord(record.SetupInfo);
            if (setupInfo.IsNone)
            {
                Debug.LogWarning("Failed to load combat setup info from saved data, returning to main menu...");
                gameManager.PauseMenuToMainMenu();
                return;
            }
            
            CombatSetupInfo = setupInfo.Value;
            
            characters.SetupCharactersFromSave(record);
            WinningCondition = record.WinningCondition.Deserialize(combatManager: this);
            ElapsedTime = record.ElapsedTime;
            AccumulatedExhaustionTime = record.AccumulatedExhaustionTime;

            timeTmp.text = WinningCondition.GetTimeToDisplay().ToString("0.00");

            Running = true;
            Tracker = new CombatTracker(onFinish);
            Announce(WinningCondition.DisplayName, delay: Option<float>.Some(DefaultAnnounceDelay), WinningConditionAnnounceDuration);
            
            PauseTime();
        }

        public void PlayerRequestsEscape() { throw new NotImplementedException(); }
    }
}