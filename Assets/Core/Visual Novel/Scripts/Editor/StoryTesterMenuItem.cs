using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Core.Visual_Novel.Scripts.Editor
{
    public static class StoryTesterMenuItem
    {
        [MenuItem("Test/Story")]
        private static async void StartTest()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;

            EditorSceneManager.OpenScene("Assets/Core/Visual Novel/Scene_StoryTester.unity");

            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                await Task.Delay(TimeSpan.FromSeconds(1f));

            EditorApplication.EnterPlaymode();
        }
    }
}
