using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Character_Panel.Scripts;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts.PathCreating;
using Core.World_Map.Scripts;
using Main_Database.Local_Map;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Local_Map.Scripts
{
    public class LocalMapTestInitiator : MonoBehaviour
    {
        [SerializeField, Required] 
        private PathBetweenNodesGenerator[] nodes;
        
        [SerializeField] 
        private BothWays path;
        
        [SerializeField] 
        private float angle;
        
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
            Save.Current.SetLocation(path.One);
            
            PathBetweenNodesBlueprint[] blueprints = new PathBetweenNodesBlueprint[nodes.Length];
            OneWay direction = new(path.One, path.Two);
            for (int i = 0; i < nodes.Length; i++)
            {
                bool isLast = i == nodes.Length - 1;
                blueprints[i] = nodes[i].GetBluePrint(deterministic: false, direction, isLast);
            }
            
            FullPathInfo fullPathInfo = GeneratePathInfo(path);

            SceneManager.LoadScene(SceneRef.LocalMap.Name, LoadSceneMode.Additive);

            while (LocalMapManager.Instance.IsNone)
                yield return null;
            
            LocalMapManager.Instance.Value.GenerateMap(source: Option.None, blueprints, fullPathInfo, path.One, path.Two);

            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private FullPathInfo GeneratePathInfo(BothWays bothWays)
        {
            float polarAngle = angle < 0 ? Mathf.Abs(angle) + 180 : angle;

            Option<PathInfo> pathInfo = PathDatabase.GetPathInfo(path);
            Option<TileInfo> startTileInfo = TileInfoDatabase.GetWorldLocationTileInfo(bothWays.One);
            if (startTileInfo.IsNone || pathInfo.IsNone)
                throw new Exception();

            FullPathInfo fullPathInfo = new(pathInfo.Value, polarAngle, startTileInfo.SomeOrDefault());
            return fullPathInfo;
        }

        private async void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (obj != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;

            while (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying)
                await Task.Delay(TimeSpan.FromSeconds(0.17f));
            
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Core/Local Map/Scene_LocalMap.unity");
        }
#endif 
    }
}