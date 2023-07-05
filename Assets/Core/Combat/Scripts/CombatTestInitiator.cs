using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Audio.Scripts;
using Core.Character_Panel.Scripts;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Combat.Scripts.WinningCondition;
using Core.Game_Manager.Scripts;
using Core.Local_Map.Scripts;
using Core.Local_Map.Scripts.PathCreating;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Local_Map;
using Core.Save_Management.SaveObjects;
using Core.Utils.Async;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using Data.Main_Characters.Ethel;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts
{
    public class CombatTestInitiator : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown[] enemyDropdowns;

        [SerializeField]
        private CharacterScriptable[] enemies;

        [SerializeField]
        private TMP_Dropdown[] allyDropdowns;

        [SerializeField]
        private CharacterScriptable[] allies;
        
        [SerializeField]
        private TMP_Dropdown backgroundDropdown;
        
        [SerializeField]
        private CombatBackground[] backgrounds;
        
        [SerializeField]
        private Toggle mistToggle;

        [SerializeField]
        private Toggle lustToggle;

        [SerializeField]
        private Toggle debugMode;

        [SerializeField]
        private TMP_Dropdown[] ethelSkillsDropdowns;

        [SerializeField]
        private SkillScriptable[] ethelSkills;

        [SerializeField]
        private TMP_Dropdown[] nemaSkillsDropdowns;
        
        [SerializeField]
        private SkillScriptable[] nemaSkills;
        
        [SerializeField]
        private Button startCombatButton;

        [SerializeField]
        private Button startLocalMapButton;
        
        [SerializeField]
        private PathBetweenNodesGenerator[] nodes;
        
        [SerializeField]
        private BothWays path;
        
        [SerializeField] 
        private float angle;

        [SerializeField]
        private Slider ethelLust, nemaLust;

#if UNITY_EDITOR
        private void Start()
        {
            for (int i = 0; i < allyDropdowns.Length; i++)
            {
                TMP_Dropdown dropdown = allyDropdowns[i];
                dropdown.ClearOptions();
                List<string> options = allies.Select(ally => ally.CharacterName).ToList();
                options.Insert(0, "none");
                dropdown.AddOptions(options);
                int index = i;
                dropdown.onValueChanged.AddListener(value => { PlayerPrefs.SetInt($"debug_combat_ally_{index}", value); });
                int value = PlayerPrefs.GetInt($"debug_combat_ally_{i}", defaultValue: 0);
                dropdown.SetValueWithoutNotify(value);
            }

            for (int i = 0; i < enemyDropdowns.Length; i++)
            {
                TMP_Dropdown dropdown = enemyDropdowns[i];
                dropdown.ClearOptions();
                List<string> options = enemies.Select(enemy => enemy.CharacterName).ToList();
                options.Insert(0, "none");
                dropdown.AddOptions(options);
                int index = i;
                dropdown.onValueChanged.AddListener(value => { PlayerPrefs.SetInt($"debug_combat_enemy_{index}", value); });
                int value = PlayerPrefs.GetInt($"debug_combat_enemy_{i}", defaultValue: 0);
                dropdown.SetValueWithoutNotify(value);
            }

            backgroundDropdown.ClearOptions();
            backgroundDropdown.AddOptions(backgrounds.Select(background => background.Key.Remove("background_").ToString()).ToList());
            backgroundDropdown.onValueChanged.AddListener(value => { PlayerPrefs.SetInt("debug_combat_background", value); });
            backgroundDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("debug_combat_background", defaultValue: 0));
            
            for (int i = 0; i < ethelSkillsDropdowns.Length; i++)
            {
                TMP_Dropdown dropdown = ethelSkillsDropdowns[i];
                dropdown.ClearOptions();
                List<string> options = ethelSkills.Select(skill => skill.DisplayName).ToList();
                options.Insert(0, "none");
                dropdown.AddOptions(options);
                int index = i;
                dropdown.onValueChanged.AddListener(value => { PlayerPrefs.SetInt($"debug_combat_ethel_skill_{index}", value); });
                int value = PlayerPrefs.GetInt($"debug_combat_ethel_skill_{i}", defaultValue: 0);
                dropdown.SetValueWithoutNotify(value);
            }

            for (int i = 0; i < nemaSkillsDropdowns.Length; i++)
            {
                TMP_Dropdown dropdown = nemaSkillsDropdowns[i];
                dropdown.ClearOptions();
                List<string> options = nemaSkills.Select(skill => skill.DisplayName).ToList();
                options.Insert(0, "none");
                dropdown.AddOptions(options);
                int index = i;
                dropdown.onValueChanged.AddListener(value => { PlayerPrefs.SetInt($"debug_combat_nema_skill_{index}", value); });
                int value = PlayerPrefs.GetInt($"debug_combat_nema_skill_{i}", defaultValue: 0);
                dropdown.SetValueWithoutNotify(value);
            }

            mistToggle.onValueChanged.AddListener(value => { PlayerPrefs.SetInt("debug_combat_mist", value ? 1 : 0); });
            mistToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("debug_combat_mist", defaultValue: 0) == 1);
            
            lustToggle.onValueChanged.AddListener(value => { PlayerPrefs.SetInt("debug_combat_lust", value ? 1 : 0); });
            lustToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("debug_combat_lust", defaultValue: 0) == 1);
            
            debugMode.onValueChanged.AddListener(value => { PlayerPrefs.SetInt("debug_combat_debug_mode", value ? 1 : 0); });
            debugMode.SetIsOnWithoutNotify(PlayerPrefs.GetInt("debug_combat_debug_mode", defaultValue: 0) == 1);
            
            startCombatButton.onClick.AddListener(() =>
            {
                PlayerPrefs.Save();
                CombatManager.DEBUGMODE = debugMode.isOn;

                if (false == AreEthelAndNemaSkillsValid(chosenEthelSkills: out HashSet<ISkill> chosenEthelSkills, chosenNemaSkills: out HashSet<ISkill> chosenNemaSkills))
                {
                    Debug.LogWarning("You must select at least one skill for each character");
                    return;
                }

                HashSet<CharacterScriptable> selectedAllies = new(); // hashset because we don't want ethel/nema duplicates
                allyDropdowns.DoForEach(dropdown =>
                {
                    int selected = dropdown.value;
                    if (selected != 0)
                        selectedAllies.Add(allies[selected - 1]);
                });
                
                if (selectedAllies.Count == 0)
                {
                    Debug.LogWarning("No allies selected");
                    return;
                }
                
                List<CharacterScriptable> selectedEnemies = new();
                enemyDropdowns.DoForEach(dropdown =>
                {
                    int selected = dropdown.value;
                    if (selected != 0)
                        selectedEnemies.Add(enemies[selected - 1]);
                });
                
                if (selectedEnemies.Count == 0)
                {
                    Debug.LogWarning("No enemies selected");
                    return;
                }
                
                CombatSetupInfo setupInfo = new(
                                                selectedAllies.Select(ally => ((ICharacterScript)ally, CombatSetupInfo.RecoveryInfo.Default, 0f, bindToSave: true)).ToArray(),
                                                selectedEnemies.Select(enemy => ((ICharacterScript)enemy, CombatSetupInfo.RecoveryInfo.Default)).ToArray(),
                                                mistExists: mistToggle.isOn,
                                                allowLust: lustToggle.isOn,
                                                GeneralPaddingSettings.Default
                                               );

                new CoroutineWrapper(LoadScenesThenCombat(setupInfo, WinningConditionGenerator.Default, backgrounds[backgroundDropdown.value].Key, chosenEthelSkills, chosenNemaSkills, (uint)ethelLust.value, (uint)nemaLust.value),
                                     nameof(LoadScenesThenCombat), context: null, autoStart: true);
                Destroy(gameObject);
            });
            
            startLocalMapButton.onClick.AddListener(() =>
            {
                PlayerPrefs.Save();
                CombatManager.DEBUGMODE = debugMode.isOn;
                
                HashSet<CharacterScriptable> selectedAllies = new(); // hashset because we don't want ethel/nema duplicates
                allyDropdowns.DoForEach(dropdown =>
                {
                    int selected = dropdown.value;
                    if (selected != 0)
                        selectedAllies.Add(allies[selected - 1]);
                });
                
                if (selectedAllies.Count < 2)
                {
                    Debug.LogWarning("You must select at least 2 allies, Ethel and Nema");
                    return;
                }

                if (selectedAllies.None(scriptable => scriptable.Key == Ethel.GlobalKey))
                {
                    Debug.LogWarning("Ethel is not selected");
                    return;
                }

                if (selectedAllies.None(scriptable => scriptable.Key == Nema.GlobalKey))
                {
                    Debug.LogWarning("Nema is not selected");
                    return;
                }

                List<CleanString> alliesOrder = new(selectedAllies.Count);
                foreach (TMP_Dropdown dropdown in allyDropdowns)
                {
                    if (dropdown.value == 0)
                        continue;
                    
                    CharacterScriptable ally = allies[dropdown.value - 1];
                    if (selectedAllies.Contains(ally))
                    {
                        alliesOrder.Add(ally.Key);
                        selectedAllies.Remove(ally);
                    }
                }

                if (alliesOrder.Count == 0)
                {
                    Debug.LogWarning("You must select at least 2 allies, Ethel and Nema");
                    return;
                }
                
                if (false == AreEthelAndNemaSkillsValid(chosenEthelSkills: out HashSet<ISkill> chosenEthelSkills, chosenNemaSkills: out HashSet<ISkill> chosenNemaSkills))
                {
                    Debug.LogWarning("You must select at least 1 skill for Ethel and Nema");
                    return;
                }
                
                new CoroutineWrapper(LoadScenesThenLocalMap(GeneratePathInfo(path), nodes, path, chosenEthelSkills, chosenNemaSkills, alliesOrder, (uint)ethelLust.value, (uint)nemaLust.value),
                                     nameof(LoadScenesThenLocalMap), context: null, autoStart: true);
                Destroy(gameObject);
            });
        }

        private bool AreEthelAndNemaSkillsValid(out HashSet<ISkill> chosenEthelSkills, out HashSet<ISkill> chosenNemaSkills)
        {
            chosenEthelSkills = new();
            chosenNemaSkills = new();
            HashSet<ISkill> ethelSkillsReference = chosenEthelSkills;
            ethelSkillsDropdowns.DoForEach(dropdown =>
            {
                int selected = dropdown.value;
                if (selected != 0)
                    ethelSkillsReference.Add(ethelSkills[selected - 1]);
            });

            if (chosenEthelSkills.Count == 0)
                return false;
            
            HashSet<ISkill> nemaSkillsReference = chosenNemaSkills;
            nemaSkillsDropdowns.DoForEach(dropdown =>
            {
                int selected = dropdown.value;
                if (selected != 0)
                    nemaSkillsReference.Add(nemaSkills[selected - 1]);
            });

            if (chosenNemaSkills.Count == 0)
                return false;
            
            return true;
        }

        private static IEnumerator LoadScenesThenCombat(CombatSetupInfo setupInfo, WinningConditionGenerator winningConditionGenerator, CleanString backgroundKey,
                                                        HashSet<ISkill> chosenEthelSkills, HashSet<ISkill> chosenNemaSkills, uint ethelLust, uint nemaLust)
        {
            SceneManager.LoadScene(SceneRef.GameManager.Name, LoadSceneMode.Additive);
            
            while (GameManager.Instance.IsNone)
                yield return null;

            GameManager gameManager = GameManager.Instance.Value;
            gameManager.LoadScenesForTesting();
            
            Save.StartSaveAsTesting();
            Save save = Save.Current;
            save.SetLust(Ethel.GlobalKey, ethelLust);
            save.SetLust(Nema.GlobalKey, nemaLust);
            int index = 0;
            foreach (ISkill skill in chosenEthelSkills)
            {
                save.OverrideSkill(Ethel.GlobalKey, skill, index);
                index++;
            }

            for (; index < 4; index++)
                save.UnassignSkill(Ethel.GlobalKey, slotIndex: index);
            
            index = 0;
            foreach (ISkill skill in chosenNemaSkills)
            {
                save.OverrideSkill(Nema.GlobalKey, skill, index);
                index++;
            }
            
            for (; index < 4; index++)
                save.UnassignSkill(Nema.GlobalKey, slotIndex: index);

            while (MusicManager.Instance.IsNone)
                yield return null;
            
            MusicManager.Instance.Value.NotifyEvent(MusicEvent.Combat);

            SceneManager.LoadScene(SceneRef.Combat.Name, LoadSceneMode.Additive);

            while (CombatManager.Instance.IsNone)
                yield return null;
            
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            CombatTracker combatState = new(OnFinish: new CombatTracker.StandardLocalMap());

            CombatManager.Instance.Value.SetupCombatFromBeginning(setupInfo, combatState, winningConditionGenerator, backgroundKey);
        }
        
        private static async void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (obj != UnityEditor.PlayModeStateChange.ExitingPlayMode)
                return;

            while (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying)
                await Task.Delay(TimeSpan.FromSeconds(0.17f));
            
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Core/Combat/Scene_Test_Combat.unity");
        }

        private static IEnumerator LoadScenesThenLocalMap(FullPathInfo generatePathInfo, PathBetweenNodesGenerator[] pathBetweenNodesGenerators, BothWays bothWays, HashSet<ISkill> chosenEthelSkills, HashSet<ISkill> chosenNemaSkills,
                                                          List<CleanString> alliesOrder, uint ethelLust, uint nemaLust)
        {
            SceneManager.LoadScene(SceneRef.GameManager.Name, LoadSceneMode.Additive);
            
            while (GameManager.Instance.IsNone)
                yield return null;

            GameManager gameManager = GameManager.Instance.Value;
            gameManager.LoadScenesForTesting();

            while (CharacterMenuManager.Instance.IsNone)
                yield return null;

            Save.StartSaveAsTesting();
            Save.Current.SetLocation(bothWays.One);
            Save save = Save.Current;
            save.SetLust(Ethel.GlobalKey, ethelLust);
            save.SetLust(Nema.GlobalKey, nemaLust);
            
            int index = 0;
            foreach (ISkill skill in chosenEthelSkills)
            {
                save.OverrideSkill(Ethel.GlobalKey, skill, index);
                index++;
            }

            for (; index < 4; index++)
                save.UnassignSkill(Ethel.GlobalKey, slotIndex: index);
            
            index = 0;
            foreach (ISkill skill in chosenNemaSkills)
            {
                save.OverrideSkill(Nema.GlobalKey, skill, index);
                index++;
            }
            
            for (; index < 4; index++)
                save.UnassignSkill(Nema.GlobalKey, slotIndex: index);
            
            save.SetCombatOrder(alliesOrder);

            PathBetweenNodesBlueprint[] blueprints = new PathBetweenNodesBlueprint[pathBetweenNodesGenerators.Length];
            OneWay direction = new(bothWays.One, bothWays.Two);
            for (int i = 0; i < pathBetweenNodesGenerators.Length; i++)
            {
                PathBetweenNodesGenerator nodesGenerator = pathBetweenNodesGenerators[i];
                bool isLast = i == pathBetweenNodesGenerators.Length - 1;
                blueprints[i] = nodesGenerator.GetBluePrint(deterministic: false, direction, isLast);
            }
            
            FullPathInfo fullPathInfo = generatePathInfo;

            SceneManager.LoadScene(SceneRef.LocalMap.Name, LoadSceneMode.Additive);

            while (LocalMapManager.Instance.IsNone)
                yield return null;
            
            LocalMapManager.Instance.Value.GenerateMap(source: null, blueprints, fullPathInfo, bothWays.One, bothWays.Two);

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
#endif
    }
}