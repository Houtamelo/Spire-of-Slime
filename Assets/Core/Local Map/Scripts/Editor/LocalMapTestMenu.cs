using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Core.Local_Map.Scripts.Editor
{
    public static class LocalMapTestMenu
    {
        [MenuItem("Test/Local Map")]
        private static async void StartTest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;
            
            EditorSceneManager.OpenScene("Assets/Core/Local Map/Scene_Test_LocalMap.unity");

            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Delay(TimeSpan.FromSeconds(1f));
            
            EditorApplication.EnterPlaymode();
        }
    }
}