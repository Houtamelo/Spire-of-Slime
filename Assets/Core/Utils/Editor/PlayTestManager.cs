using Core.Game_Manager.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Utils.Editor
{
    [InitializeOnLoad]
    public static class PlayTestManager
    {
        static PlayTestManager()
        {
            EditorApplication.playModeStateChanged += LoadEssentialScenes;
        }

        private static void LoadEssentialScenes(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.name.ToLowerInvariant().Contains("test") || activeScene.name == SceneRef.StartScreen.Name)
                    return;

                LoadSceneWithIndex(0);
            }
        }

        private static void LoadSceneWithIndex(int count)
        {
            if (count >= GameManager.CrucialScenes.Length)
                return;
                
            SceneRef sceneName = GameManager.CrucialScenes[count];
            Scene scene = SceneManager.GetSceneByName(sceneName.Name);
            if (!scene.isLoaded)
            {
                AsyncOperation handle = SceneManager.LoadSceneAsync(sceneName.Name, LoadSceneMode.Additive);
                handle.completed += _ => { LoadSceneWithIndex(count + 1); };
            }
            else
            {
                LoadSceneWithIndex(count + 1);
            }
        }
    }
}