using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Character_Panel.Scripts;
using Core.Game_Manager.Scripts;
using Save_Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils.Async;
using Save = Save_Management.Save;

namespace Core.Visual_Novel.Scripts
{
    public class StoryTestInitiator : MonoBehaviour
    {
        public Button afterTutorial, bulgingPlantEvent, chapelAfterDragonkin;

        private static readonly Action<Save> LeavingTowardsDragonkin = save =>
        {
            save.SetVariable("Finished_Ch01_Awakening",                          true);
            save.SetVariable("Finished_Ch01_First_Fight",                        true);
            save.SetVariable("Finished_Ch01_Rescued_by_Mistress_Tender",         true);
            save.SetVariable("Finished_Ch01_Ethel_Nema_discuss_dragonkin_quest", true);
            save.SetVariable("Finished_Ch01_Leaving_chapel_towards_dragonkin",   true);
            save.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
            GameManager.Instance.Value.VisualNovelToWorldmap();
        };
        
        private static readonly Action<Save> BulgingPlantEvent = save =>
        {
            save.SetVariable("Finished_Ch01_Awakening",                          true);
            save.SetVariable("Finished_Ch01_First_Fight",                        true);
            save.SetVariable("Finished_Ch01_Rescued_by_Mistress_Tender",         true);
            save.SetVariable("Finished_Ch01_Ethel_Nema_discuss_dragonkin_quest", true);
            save.SetVariable("Finished_Ch01_Leaving_chapel_towards_dragonkin",   true);
            save.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
            DialogueController.Instance.Value.Play("Ch01_Nema_bulging_plant");
        };

        private static readonly Action<Save> ChapelAfterDragonkin = save =>
        {
            save.SetVariable("Finished_Ch01_Awakening",                          true);
            save.SetVariable("Finished_Ch01_First_Fight",                        true);
            save.SetVariable("Finished_Ch01_Rescued_by_Mistress_Tender",         true);
            save.SetVariable("Finished_Ch01_Ethel_Nema_discuss_dragonkin_quest", true);
            save.SetVariable("Finished_Ch01_Leaving_chapel_towards_dragonkin",   true);
            save.SetVariable("DragonFruitComplete", true);
            DialogueController.Instance.Value.Play("Ch01_ChapelAfterDragonkin");
        };

#if UNITY_EDITOR
        private void Start()
        {
            afterTutorial.onClick.AddListener(() => new CoroutineWrapper(LoadScenesThenModifySave(LeavingTowardsDragonkin), nameof(LoadScenesThenModifySave), context: null, autoStart: true));
            bulgingPlantEvent.onClick.AddListener(() => new CoroutineWrapper(LoadScenesThenModifySave(BulgingPlantEvent), nameof(LoadScenesThenModifySave), context: null, autoStart: true));
            chapelAfterDragonkin.onClick.AddListener(() => new CoroutineWrapper(LoadScenesThenModifySave(ChapelAfterDragonkin), nameof(LoadScenesThenModifySave), context: null, autoStart: true));
        }

        private IEnumerator LoadScenesThenModifySave(Action<Save> doToSave)
        {
            Destroy(gameObject);
            SceneManager.LoadScene(SceneRef.GameManager.Name, LoadSceneMode.Additive);
            
            while (GameManager.Instance.IsNone)
                yield return null;

            GameManager gameManager = GameManager.Instance.Value;
            gameManager.LoadScenesForTesting();

            while (CharacterMenuManager.Instance.IsNone)
                yield return null;

            while (DialogueController.Instance.IsNone)
                yield return null;

            Save.StartSaveAsTesting();
            Save save = Save.Current;
            doToSave?.Invoke(save);

            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static async void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (obj != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;

            while (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying)
                await Task.Delay(TimeSpan.FromSeconds(0.17f));
            
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Core/Visual Novel/Scene_StoryTester.unity");
        }
#endif
    }
}