using System;
using System.Collections;
using Core.Audio.Scripts;
using Core.Game_Manager.Scripts;
using Core.Main_Characters.Nema.Combat;
using Core.Visual_Novel.Scripts;
using Data.Main_Characters.Ethel;
using DG.Tweening;
using Main_Database.Local_Map;
using Save_Management;
using TMPro;
using UnityEngine;
using Utils.Async;
using Utils.Extensions;
using Utils.Math;
using Utils.Patterns;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Save = Save_Management.Save;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public static class RestEventHandler
    {
        public static bool LOG;
        
        public static CoroutineWrapper HandleRest(float restMultiplier, float restMultiplierDelta, int lustDecrease, float exhaustionDecrease, Option<RestEventBackground> backgroundPrefab)
        {
            CoroutineWrapper wrapper;
            if (backgroundPrefab.IsSome)
            {
                Reference<GameObject> backgroundGameObjectRef = new(null);
                IEnumerator restRoutine = RestRoutine(restMultiplier, restMultiplierDelta, lustDecrease, exhaustionDecrease, backgroundPrefab.Value, backgroundGameObjectRef);
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
                
                wrapper = new CoroutineWrapper(RestRoutineNoBackground(restMultiplier, restMultiplierDelta, lustDecrease, exhaustionDecrease), nameof(RestRoutineNoBackground), context: null, autoStart: true);
            }
            
            return wrapper;
        }
        
        private static IEnumerator RestRoutine(float restMultiplier, float restMultiplierDelta, int lustDecrease, float exhaustionDecrease, RestEventBackground backgroundPrefab, Reference<GameObject> backgroundGameObjectRef)
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

            float lowerBound = restMultiplier - restMultiplierDelta;
            float upperBound = restMultiplier + restMultiplierDelta;

            Option<int> ethelLustDelta = Option.None;
            Option<int> nemaLustDelta = Option.None;
            ReadOnlySpan<IReadonlyCharacterStats> allCharacters = save.GetAllReadOnlyCharacterStats();
            for (int i = 0; i < allCharacters.Length; i++)
            {
                IReadonlyCharacterStats stats = allCharacters[i];
                save.SetOrgasmCount(stats.Key, stats.OrgasmCount - 1);
                float multiplier = Random.Range(minInclusive: lowerBound, maxInclusive: upperBound);
                int lustDelta = -1 * Mathf.CeilToInt(lustDecrease * multiplier);
                uint lustBeforeChange = stats.Lust;
                save.ChangeLust(stats.Key, lustDelta);
                int delta = (int) save.GetStat(stats.Key, GeneralStat.Lust);
                if (delta == 0)
                    continue;
                
                if (stats.Key == Ethel.GlobalKey)
                    ethelLustDelta = Option<int>.Some((int)save.EthelStats.Lust - (int)lustBeforeChange);
                else if (stats.Key == Nema.GlobalKey)
                    nemaLustDelta = Option<int>.Some((int)save.NemaStats.Lust - (int)lustBeforeChange);
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
                {
                    yield return anyCue.Value.WaitForCompletion();
                }
            }

            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeUp().WaitForCompletion();

            if (spawnedBackground != null)
                Object.Destroy(spawnedBackground.gameObject);
            
            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeDown().WaitForCompletion();
        }

        private static IEnumerator RestRoutineNoBackground(float restMultiplier, float restMultiplierDelta, int lustDecrease, float exhaustionDecrease)
        {
            if (Save.AssertInstance(out Save save) == false)
                yield break;
            
            if (GameManager.AssertInstance(out GameManager gameManager))
                yield return gameManager.FadePanel.FadeUp().WaitForCompletion();

            Option<RestDialogue> dialogue = RestEventsDatabase.GetAvailableDialogue();
            if (dialogue.IsSome && DialogueController.AssertInstance(out DialogueController dialogueController))
                yield return dialogueController.Play(dialogue.Value.SceneName);

            float lowerBound = restMultiplier - restMultiplierDelta;
            float upperBound = restMultiplier + restMultiplierDelta;
            float olderMultiplier = Random.Range(minInclusive: lowerBound,   maxInclusive: upperBound);
            float youngerMultiplier = Random.Range(minInclusive: lowerBound, maxInclusive: upperBound);

            int ethelLust = Mathf.CeilToInt(lustDecrease * olderMultiplier);
            save.ChangeLust(Ethel.GlobalKey, -ethelLust);

            int nemaLust = Mathf.CeilToInt(lustDecrease * youngerMultiplier);
            save.ChangeLust(Nema.GlobalKey, -nemaLust);
            
            save.ChangeNemaExhaustion(-exhaustionDecrease);
            
            if (GlobalSounds.AssertInstance(out GlobalSounds globalSounds))
                globalSounds.LustReduction.Play();
            
            if (GameManager.AssertInstance(out gameManager))
                yield return gameManager.FadePanel.FadeDown().WaitForCompletion();
        }
    }
}