using System.Collections;
using Core.Audio.Scripts;
using Core.Combat.Scripts;
using Core.Game_Manager.Scripts;
using DG.Tweening;
using JetBrains.Annotations;
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;
using Yarn.Unity;
using Save = Save_Management.Save;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace Core.Visual_Novel.Scripts
{
    /// <summary> Class responsible for delegating Yarn commands. </summary>
    public static class YarnCommandHandler
    {
        [YarnCommand(YarnCommands.VarIncrement)]
        public static void IncrementVariable(string variableName, float delta)
        {
            if (Save.AssertInstance(out Save save) == false)
                return;

            save.TryGetFloat(variableName, out float current);
            save.SetVariable(variableName, current + delta);
        }
        
        [YarnCommand(YarnCommands.Cg)]
        public static void SetCg(string fileName)
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler)) 
                cgHandler.Set(fileName);
        }

        [YarnCommand(YarnCommands.CgEnd)]
        public static void EndCg()
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler))
                cgHandler.End();
        }

        [YarnCommand(YarnCommands.CgAnim)]
        public static IEnumerator SetCgAnim(string fileName)
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler))
                return cgHandler.SetAnim(fileName);

            return null;
        }

        [YarnCommand(YarnCommands.CgAnimAsync)]
        public static void SetCgAnimAsync(string fileName)
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler))
                cgHandler.SetAnimAsync(fileName);
        }

        [YarnCommand(YarnCommands.FadeIn)]
        public static IEnumerator FadeIn(float duration)
        {
            if (SceneFader.AssertInstance(out SceneFader sceneFader))
                return sceneFader.FadeIn(duration);
            
            return null;
        }

        [YarnCommand(YarnCommands.FadeInAsync)]
        public static void FadeInAsync(float duration)
        {
            if (SceneFader.AssertInstance(out SceneFader sceneFader))
                sceneFader.FadeInAsync(duration);
        }

        [YarnCommand(YarnCommands.FadeOut)]
        public static IEnumerator FadeOut(float duration)
        {
            if (SceneFader.AssertInstance(out SceneFader sceneFader))
                return sceneFader.FadeOut(duration);

            return null;
        }

        [YarnCommand(YarnCommands.FadeOutAsync)]
        public static void FadeOutAsync(float duration)
        {
            if (SceneFader.AssertInstance(out SceneFader sceneFader))
                sceneFader.FadeOutAsync(duration);
        }

        [YarnCommand(YarnCommands.FadeInHard)]
        public static IEnumerator FadeInHard() // this one uses the game manager fade which goes above everything.
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
            {
                Tween tween = gameManager.FadePanel.FadeUp();
                return new YieldableCommandWrapper(tween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
            }

            return null;
        }
        
        [YarnCommand(YarnCommands.FadeOutHard)]
        public static IEnumerator FadeOutHard() // this one uses the game manager fade which goes above everything.
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
            {
                Tween tween = gameManager.FadePanel.FadeDown();
                return new YieldableCommandWrapper(tween.WaitForCompletion().AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: tween.CompleteIfActive);
            }
            
            return null;
        }

        [YarnCommand(YarnCommands.Sfx)]
        public static void PlaySfx(string fileName, float volume = 1f)
        {
            if (SfxManager.AssertInstance(out SfxManager sfxManager))
                sfxManager.PlaySingle(fileName, volume);
        }

        [YarnCommand(YarnCommands.SfxMulti)]
        public static void PlaySfxMulti(string fileName, float volume = 1f)
        {
            if (SfxManager.AssertInstance(out SfxManager sfxManager))
                sfxManager.PlayMulti(fileName, volume);
        }

        [YarnCommand(YarnCommands.SfxWait)]
        public static IEnumerator PlaySfxWait(string fileName, float volume = 1f)
        {
            if (SfxManager.AssertInstance(out SfxManager sfxManager))
                return sfxManager.PlayAndWait(fileName, volume);
            
            return null;
        }

        [YarnCommand(YarnCommands.AmbienceSet)]
        public static void SetAmbience(string fileName, float volume = 1f, bool loop = true)
        {
            if (AmbienceManager.AssertInstance(out AmbienceManager ambienceManager))
                ambienceManager.Set(fileName, volume, loop);
        }

        [YarnCommand(YarnCommands.AmbienceAdd)]
        public static void AddAmbience(string fileName, float volume = 1f, bool loop = true)
        {
            if (AmbienceManager.AssertInstance(out AmbienceManager ambienceManager))
                ambienceManager.Add(fileName, volume, loop);
        }

        [YarnCommand(YarnCommands.AmbienceEnd)]
        public static void EndAmbience()
        {
            if (AmbienceManager.AssertInstance(out AmbienceManager ambienceManager))
                ambienceManager.End();
        }

        [YarnCommand(YarnCommands.Music)]
        public static void PlayMusic(string key, float volume = 1f)
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.SetController(key, volume);
        }

        [YarnCommand(YarnCommands.MusicEnd)]
        public static void EndMusic()
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.StopAny();
        }

        [YarnCommand(YarnCommands.MusicAllowCombatOrLocalMap)]
        public static void AllowLocalMapOrCombatMusic(bool value)
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.AllowLocalMapOrCombatMusic(value);
        }

        [YarnCommand(YarnCommands.Mist)]
        public static void SetMist(float transparencyPercentage)
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler))
                cgHandler.SetMist(transparencyPercentage);
        }

        [YarnCommand(YarnCommands.MistEnd)]
        public static void EndMist()
        {
            if (CgHandler.AssertInstance(out CgHandler cgHandler))
                cgHandler.EndMist();
        }

        [YarnCommand(YarnCommands.WorldMap), UsedImplicitly]
        public static void StartWorldMap()
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
                gameManager.VisualNovelToWorldmap();
        }

        [YarnCommand(YarnCommands.LocalMap), UsedImplicitly]
        public static void StartLocalMap()
        {
            if (GameManager.AssertInstance(out GameManager gameManager))
                gameManager.VisualNovelToLocalMap();
        }

        [YarnCommand(YarnCommands.SetUI), UsedImplicitly]
        public static IEnumerator SetUI(bool active, float duration = DialogueUIManager.FadeUIDefaultDuration)
        {
            if (DialogueUIManager.AssertInstance(out DialogueUIManager dialogueUIManager))
                return dialogueUIManager.SetUI(active, duration);

            return null;
        }

        [YarnCommand(YarnCommands.SetUIAsync), UsedImplicitly]
        public static void SetUIAsync(bool active, float duration = DialogueUIManager.FadeUIDefaultDuration)
        {
            if (DialogueUIManager.AssertInstance(out DialogueUIManager dialogueUIManager))
                dialogueUIManager.SetUIAsync(active, duration);
        }

        [YarnCommand(YarnCommands.Combat), UsedImplicitly]
        public static void StartCombat(string combatScriptName, string winScene, string lossScene)
        {
            Option<ScriptableCombatSetupInfo> combatOption = CombatScriptDatabase.GetCombat(combatScriptName);
            if (combatOption.AssertSome(out ScriptableCombatSetupInfo script) == false)
            {
                Debug.LogWarning($"Combat script {combatScriptName} not found, we will assume the player won.");
                if (DialogueController.AssertInstance(out DialogueController dialogueController))
                    dialogueController.Play(winScene);

                return;
            }

            CombatTracker tracker = new(OnFinish: new CombatTracker.PlayScene(winScene, lossScene));
            
            if (GameManager.AssertInstance(out GameManager gameManager))
                gameManager.VisualNovelToCombat(script.ToStruct(), tracker, script.WinningConditionGenerator, script.BackgroundPrefab.Key, script.MusicController);
        }
        
        [YarnCommand(YarnCommands.AwardLevel), UsedImplicitly]
        public static void AwardLevel()
        {
            if (Save.AssertInstance(out Save save))
                save.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
        }

        [YarnCommand(YarnCommands.AwardPrimaryUpgradePoint)]
        public static void AwardPrimaryUpgradePoint(string characterKey, int points)
        {
            if (Save.AssertInstance(out Save save))
                save.AwardPrimaryPoint(characterKey, (uint)points);
        }
        
        [YarnCommand(YarnCommands.AwardSecondaryUpgradePoint)]
        public static void AwardSecondaryUpgradePoint(string characterKey, int points)
        {
            if (Save.AssertInstance(out Save save))
                save.AwardSecondaryPoint(characterKey, (uint)points);
        }

        [YarnCommand(YarnCommands.AwardPerk)]
        public static void AwardPerk(string key)
        {
            if (Save.AssertInstance(out Save save))
                save.UnlockPerk(key);
        }
    }
}