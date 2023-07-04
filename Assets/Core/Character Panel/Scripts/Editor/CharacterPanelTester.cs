using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Core.Character_Panel.Scripts.Editor
{
    public static class CharacterPanelTester
    {
        [MenuItem("Test/Character Panel")]
        private static async void StartTest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;
            
            EditorSceneManager.OpenScene("Assets/Core/Character Panel/Scene_Test_CharacterPanel.unity");

            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Delay(TimeSpan.FromSeconds(1f));
            
            EditorApplication.EnterPlaymode();
        }
    }
}