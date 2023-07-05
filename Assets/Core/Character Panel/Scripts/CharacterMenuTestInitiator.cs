using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Game_Manager.Scripts;
using Core.Save_Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Character_Panel.Scripts
{
    public class CharacterMenuTestInitiator : MonoBehaviour
    {
        #if UNITY_EDITOR
        private IEnumerator Start()
        {
            SceneManager.LoadScene(SceneRef.GameManager.Name, LoadSceneMode.Additive);
            
            while (GameManager.Instance.IsNone)
                yield return null;

            GameManager gameManager = GameManager.Instance.Value;
            gameManager.LoadScenesForTesting();

            while (CharacterMenuManager.Instance.IsNone)
                yield return null;

            Save.StartSaveAsTesting();
            Save.Current.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
            Save.Current.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
            Save.Current.AwardExperienceRaw(ExperienceCalculator.ExperienceNeededForLevelUp);
            CharacterMenuManager.Instance.Value.Open();

            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private async void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (obj != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;

            while (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying)
                await Task.Delay(TimeSpan.FromSeconds(0.17f));
            
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Core/Character Panel/Scene_CharacterPanel.unity");
        }
#endif 
    }
}