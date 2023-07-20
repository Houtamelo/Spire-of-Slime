using System;
using System.Collections;
using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Local_Map;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Utils.Patterns;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public static class RestEventHandler
    {
        public static bool LOG;
        
        [NotNull]
        public static CoroutineWrapper HandleRest(float restMultiplier, float restMultiplierAmplitude, int lustDecrease, int exhaustionDecrease, int orgasmRestore, Option<RestEventBackground> backgroundPrefab)
        {
            CoroutineWrapper wrapper;
            if (backgroundPrefab.IsSome)
            {
                Reference<GameObject> backgroundGameObjectRef = new(null);
                IEnumerator restRoutine = RestRoutine(restMultiplier, restMultiplierAmplitude, lustDecrease, exhaustionDecrease, orgasmRestore, backgroundPrefab.Value, backgroundGameObjectRef);
                wrapper = new CoroutineWrapper(restRoutine, nameof(RestRoutine), context: null, autoStart: true);
                wrapper.Finished += (_, _) =>
                {
                    if (backgroundGameObjectRef.Value != null)
                        Object.Destroy(backgroundGameObjectRef.Value);
                };
            }
            else
            {
                if (LOG)
                    Debug.LogWarning("No background prefab found for rest event.");
                
                wrapper = new CoroutineWrapper(RestRoutineNoBackground(restMultiplier, restMultiplierAmplitude, lustDecrease, exhaustionDecrease, orgasmRestore), nameof(RestRoutineNoBackground), context: null, autoStart: true);
            }
            
            return wrapper;
        }
        
        private static IEnumerator RestRoutine(float restMultiplier, float restMultiplierAmplitude, int lustDecrease, int exhaustionDecrease, int orgasmRestore, RestEventBackground backgroundPrefab, Reference<GameObject> backgroundGameObjectRef)
        {
            if (Save.AssertInstance(out Save save) == false)
                yield break;

            if (GameManager.AssertInstance(out GameManager gameManager))
                yield return gameManager.FadePanel.FadeUp().WaitForCompletion();

            if (LocalMapManager.AssertInstance(out LocalMapManager localMapManager) == false)
                yield break;

            Transform parent = localMapManager.GenericEventsParent;
            RestEventBackground spawnedBackground = backgroundPrefab.InstantiateWithFixedLocalScaleAndPosition(parent);
            backgroundGameObjectRef.Value = spawnedBackground.gameObject;
            
            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeDown().WaitForCompletion();

            Option<RestDialogue> dialogue = RestEventsDatabase.GetAvailableDialogue();
            if (dialogue.IsSome && DialogueController.AssertInstance(out DialogueController dialogueController))
                yield return dialogueController.Play(dialogue.Value.SceneName);

            yield return new WaitForSeconds(1f);
            

            Option<int> ethelLustDelta = Option.None;
            Option<int> nemaLustDelta = Option.None;
            ReadOnlySpan<IReadonlyCharacterStats> allCharacters = save.GetAllReadOnlyCharacterStats();
            for (int i = 0; i < allCharacters.Length; i++)
            {
                IReadonlyCharacterStats stats = allCharacters[i];
                ProcessStatRecovery(lustDecrease, orgasmRestore, save, stats, restMultiplier, restMultiplierAmplitude, out int actualLustDelta);

                if (actualLustDelta == 0)
                    continue;
                
                if (stats.Key == Ethel.GlobalKey)
                    ethelLustDelta = Option<int>.Some(actualLustDelta);
                else if (stats.Key == Nema.GlobalKey)
                    nemaLustDelta = Option<int>.Some(actualLustDelta);
            }

            save.ChangeNemaExhaustion(-exhaustionDecrease);
            
            if (WorldTextCueManager.AssertInstance(out WorldTextCueManager cueManager))
            {
                const float stayDuration = 2.5f;
                const float fadeDuration = 0.5f;
                Vector3 speed = Vector3.up * 0.3f;
                
                Option<Sequence> anyCue = Option<Sequence>.None;
                if (ethelLustDelta.IsSome)
                {
                    string ethelLustString = ethelLustDelta.Value.WithSymbol();
                    WorldCueOptions worldCueOptions = new($"{ethelLustString} Lust", size: 35f, spawnedBackground.EthelWorldPosition, 
                                                          Color.magenta, stayDuration, fadeDuration, speed, HorizontalAlignmentOptions.Center, stopOthers: false);
                    anyCue = Option<Sequence>.Some(cueManager.Show(worldCueOptions));
                }
                
                if (nemaLustDelta.IsSome)
                {
                    string nemaLustString = nemaLustDelta.Value.WithSymbol();
                    WorldCueOptions worldCueOptions = new($"{nemaLustString} Lust", 35f, spawnedBackground.NemaWorldPosition, Color.magenta,
                                                          stayDuration, fadeDuration, speed, HorizontalAlignmentOptions.Center, stopOthers: false);
                    anyCue = Option<Sequence>.Some(cueManager.Show(worldCueOptions));
                }
                
                if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                    globalSounds.LustReduction.Play();
                
                if (anyCue.IsSome)
                    yield return anyCue.Value.WaitForCompletion();
            }

            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeUp().WaitForCompletion();

            if (spawnedBackground != null)
                Object.Destroy(spawnedBackground.gameObject);
            
            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeDown().WaitForCompletion();
        }

        private static void ProcessStatRecovery(int lustDecrease, int orgasmRestore, [NotNull] Save save, [NotNull] IReadonlyCharacterStats stats, float restMultiplier, float restMultiplierAmplitude, out int actualLustDelta)
        {
            save.ChangeOrgasmCount(stats.Key, orgasmRestore);
            
            float actualMultiplier = Random.Range(restMultiplier - restMultiplierAmplitude, restMultiplier + restMultiplierAmplitude);
            int lustDelta = (int)(-1 * lustDecrease * actualMultiplier);
            int lustBeforeChange = stats.Lust;
            save.ChangeLust(stats.Key, lustDelta);
            int lustAfterChange = save.GetStat(stats.Key, GeneralStat.Lust);
            actualLustDelta = lustAfterChange - lustBeforeChange;
        }

        private static IEnumerator RestRoutineNoBackground(float restMultiplier, float restMultiplierAmplitude, int lustDecrease, int exhaustionDecrease, int orgasmRestore)
        {
            if (Save.AssertInstance(out Save save) == false)
                yield break;
            
            if (GameManager.AssertInstance(out GameManager gameManager))
                yield return gameManager.FadePanel.FadeUp().WaitForCompletion();

            Option<RestDialogue> dialogue = RestEventsDatabase.GetAvailableDialogue();
            if (dialogue.IsSome && DialogueController.AssertInstance(out DialogueController dialogueController))
                yield return dialogueController.Play(dialogue.Value.SceneName);
            
            ReadOnlySpan<IReadonlyCharacterStats> allCharacters = save.GetAllReadOnlyCharacterStats();
            for (int i = 0; i < allCharacters.Length; i++)
            {
                IReadonlyCharacterStats stats = allCharacters[i];
                ProcessStatRecovery(lustDecrease, orgasmRestore, save, stats, restMultiplier, restMultiplierAmplitude, out _);
            }
            
            save.ChangeNemaExhaustion(-exhaustionDecrease);
            
            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.LustReduction.Play();
            
            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeDown().WaitForCompletion();
        }
    }
}