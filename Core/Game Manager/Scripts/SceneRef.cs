using System.Linq;
using Ardalis.SmartEnum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Game_Manager.Scripts
{
    public class SceneRef : SmartEnum<SceneRef>
    {
        public static readonly SceneRef MainMenu = new(enumName: "Scene_MainMenu", value: 1);
        public static readonly SceneRef LocalMap = new(enumName: "Scene_LocalMap", value: 2);
        public static readonly SceneRef WorldMap = new(enumName: "Scene_WorldMap", value: 3);
        public static readonly SceneRef Combat = new(enumName: "Scene_Combat", value: 4);
        public static readonly SceneRef PauseMenu = new(enumName: "Scene_PauseMenu", value: 5);
        public static readonly SceneRef VisualNovel = new(enumName: "Scene_VisualNovel", value: 6);
        public static readonly SceneRef StartScreen = new(enumName: "Scene_StartScreen", value: 7);
        public static readonly SceneRef GameManager = new(enumName: "Scene_GameManager", value: 8);
        public static readonly SceneRef Audio = new(enumName: "Scene_Audio", value: 9);
        public static readonly SceneRef ScreenButtons = new(enumName: "Scene_ScreenButtons", value: 10);
        public static readonly SceneRef CharacterPanel = new(enumName: "Scene_CharacterPanel", value: 11);
        private SceneRef(string enumName, int value) : base(name: enumName, value: value){}

        public static implicit operator string(SceneRef name) => name.Name;
        public static implicit operator Scene(SceneRef name) => SceneManager.GetSceneByName(name.Name);

        public void SetObjectsActive(bool active)
        {
            Scene scene = SceneManager.GetSceneByName(Name);
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            
            foreach (GameObject obj in scene.GetRootGameObjects())
                obj.SetActive(active);
        }

        public bool IsLoaded()
        {
            Scene scene = SceneManager.GetSceneByName(Name);
            return scene.IsValid() && scene.isLoaded;
        }

        public bool IsLoadedAndActive()
        {
            Scene scene = SceneManager.GetSceneByName(Name);
            return scene.IsValid() && scene.isLoaded && scene.GetRootGameObjects().Any(obj => obj.activeInHierarchy);
        }
    }
}