using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Core.Combat.Scripts.Editor
{
    public static class CombatTestMenu
    {
        [MenuItem("Test/Combat")]
        private static async void StartTest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;
            
            EditorSceneManager.OpenScene("Assets/Core/Combat/Scene_Test_Combat.unity");

            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Delay(TimeSpan.FromSeconds(1f));
            
            EditorApplication.EnterPlaymode();
        }
    }
}