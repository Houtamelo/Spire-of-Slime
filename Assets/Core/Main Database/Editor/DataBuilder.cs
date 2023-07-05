using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Core.Audio.Scripts.MusicControllers;
using Core.Combat.Scripts;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Local_Map.Scripts;
using Core.Local_Map.Scripts.Enums;
using Core.Local_Map.Scripts.Events;
using Core.Local_Map.Scripts.Events.Rest;
using Core.Main_Characters.Nema.Combat;
using Core.Main_Database.Audio;
using Core.Main_Database.Combat;
using Core.Main_Database.Local_Map;
using Core.Main_Database.Visual_Novel;
using Core.Main_Database.World_Map;
using Core.Save_Management.SaveObjects;
using Core.Visual_Novel.Scripts.Animations;
using Core.World_Map.Scripts;
using CsvHelper;
using Data.Main_Characters.Ethel;
using KGySoft.CoreLibraries;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Main_Database.Editor
{
    public static partial class DataBuilder
    {
        private const string DescriptionsPath = "/Core/Txt/Descriptions.txt";
        private const string BarksPath = "/Core/Txt/Barks.txt";
        private const string URL = "https://docs.google.com/spreadsheets/d/";
        private const string DescriptionsSheetId = "1vFNkr6ypq_MS9i-rnAmLQp1J-48-nJVhAmW4ndfkEq0";
        private const string BarksSheetId = "1PLGO_Visp2IbBYOiTLFJl4QtSkfdrgm_HGuabYGRQUs";
        
        private static readonly List<IEnumerator> CoroutineInProgress = new();
        private static int _currentExecute;
        private static void StartCoroutine(IEnumerator routine) => CoroutineInProgress.Add(routine);

        static DataBuilder() => EditorApplication.update += ExecuteCoroutine;


        private static void ExecuteCoroutine()
        {
            if (CoroutineInProgress.Count <= 0)
                return;
            
            _currentExecute = (_currentExecute + 1) % CoroutineInProgress.Count;
            bool finish = !CoroutineInProgress[_currentExecute].MoveNext();
            if (finish) 
                CoroutineInProgress.RemoveAt(_currentExecute);
        }


        [MenuItem("Tools/Download Then Build")]
        private static void DownloadThenBuild()
        {
            if (CoroutineInProgress.Count == 0)
                StartCoroutine(DownloadThenBuildRoutine());
        }

        private static IEnumerator DownloadThenBuildRoutine()
        {
            DownloadAll();
            while (CoroutineInProgress.Count > 1)
                yield return null;

            Build();
        }

        private static void DownloadAll()
        {
            StartCoroutine(DownloadData($"{Application.dataPath}{DescriptionsPath}", DescriptionsSheetId));
            StartCoroutine(DownloadData($"{Application.dataPath}{BarksPath}", BarksSheetId));
        }

        private static IEnumerator DownloadData(string filePath, string docID)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get($"{URL}{docID}/export?format=csv");
            UnityWebRequestAsyncOperation handle = webRequest.SendWebRequest();
            while (!handle.isDone)
                yield return null;

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("...Download Error: " + webRequest.error);
            }
            else if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
            {
                File.WriteAllText(filePath, webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log("Empty string on: " + filePath);
            }
        }

        private static void Build()
        {
            MusicControllers();
            WorldYarnScenes();
            MapEvents();
            WorldPaths();
            TileInfos();
            Monsters();
            Backgrounds();
            Characters();
            Skills();
            Perks();
            Barks();
            Portraits();
            CGs();
            AudioFiles();
            CombatEvents();
            RestEvents();
            Variables();
            YarnChecker.CheckYarnFiles();

            Debug.Log("Data built");
        }

        private static void MusicControllers()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out MusicDatabase musicDatabase);
            List<MusicController> musicControllers = global::Core.Main_Database.Editor.Utils.FindAssetsByType<MusicController>();
            musicDatabase.AssignData(musicControllers);
        }

        private static void Portraits()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out PortraitDatabase portraitDatabase);
            List<Sprite> sprites = global::Core.Main_Database.Editor.Utils.GetAllAssetsOfTypeInFoldersWithName<Sprite>("Portraits", "Assets/Core");
            portraitDatabase.AssignData(sprites.ToHashSet());
        }

        private static void MapEvents()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out MapEventDatabase database);
            List<ScriptableLocalMapEvent> mapEvents = global::Core.Main_Database.Editor.Utils.FindAssetsByType<ScriptableLocalMapEvent>();
            database.AssignData(mapEvents);
        }

        private static void WorldPaths()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out WorldPathDatabase worldPathDatabase);
            List<WorldPath> worldPaths = global::Core.Main_Database.Editor.Utils.FindAssetsByType<WorldPath>();
            worldPathDatabase.AssignData(worldPaths);
        }

        private static void TileInfos()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out TileInfoDatabase tileInfoDatabase);
            
            List<TileInfo> tileInfos = global::Core.Main_Database.Editor.Utils.FindAssetsByType<TileInfo>();
            tileInfoDatabase.AssignData(tileInfos);
            
            LocationEnum[] allWorldLocations = Enum<LocationEnum>.GetValues();
            StringBuilder stringBuilder = new();
            
            foreach (LocationEnum location in allWorldLocations)
            {
                if (tileInfos.Find(t => t.Type == TileType.WorldLocation && t.GetWorldLocation().Value == location))
                    continue;
                
                stringBuilder.AppendLine(Enum<LocationEnum>.ToString(location));
            }
            
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Insert(0, "Missing WorldLocation tiles: ");
                Debug.LogWarning(stringBuilder.ToString());
            }
        }

        private static void Monsters()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out MonsterTeamDatabase monsterTeamDatabase);
            List<MonsterTeam> monsterTeams = global::Core.Main_Database.Editor.Utils.FindAssetsByType<MonsterTeam>();
            monsterTeamDatabase.AssignData(monsterTeams);
        }

        private static void Backgrounds()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out BackgroundDatabase backgroundDatabase);
            List<CombatBackground> backgrounds = global::Core.Main_Database.Editor.Utils.FindComponentsByType<CombatBackground>();
            backgroundDatabase.AssignData(backgrounds);
        }

        private static void Characters()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out CharacterDatabase characterDatabase);
            List<CharacterScriptable> characters = global::Core.Main_Database.Editor.Utils.FindAssetsByType<CharacterScriptable>();
            characterDatabase.AssignData(characters);
        }
        
        private static void Skills()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out SkillDatabase skillDatabase);

            List<SkillScriptable> skills = global::Core.Main_Database.Editor.Utils.FindAssetsByType<SkillScriptable>();
            Dictionary<CleanString, SkillScriptable> skillDictionary = skills.ToDictionary(s => s.Key);

            using (StreamReader textReader = File.OpenText($"{Application.dataPath}{DescriptionsPath}"))
            using (CsvReader csvReader = new(textReader))
            {
                foreach (global::Core.Main_Database.Editor.DataBuilder.DescriptionData data in csvReader.GetRecords<global::Core.Main_Database.Editor.DataBuilder.DescriptionData>())
                {
                    if (skillDictionary.TryGetValue(data.Key, out SkillScriptable skill) == false)
                        continue;
                    
                    skill.AssignData(data.DisplayName, data.FlavorText); // skill descriptions are auto-generated
                }
            }

            skillDatabase.AssignData(skills);

            List<SkillScriptable> skillsMissingIcons = new();
            List<Ethel> ethelScripts = global::Core.Main_Database.Editor.Utils.FindAssetsByType<Ethel>();
            SkillScriptable[] ethelSkills = skillDictionary.Where(pair => pair.Key.StartsWith("skill_ethel")).Select(pair => pair.Value).ToArray();
            foreach (SkillScriptable skill in ethelSkills)
            {
                string path = AssetDatabase.GetAssetPath(skill);
                string directory = $"{path[..path.LastIndexOf('/')]}/Icons/";
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { directory });
                Sprite iconBackground = null, iconBase = null, iconBaseFx = null, iconHigh = null, iconHighFx = null;
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string assetName = assetPath[(assetPath.LastIndexOf('/') + 1)..];
                    switch (assetName)
                    {
                        case "icon_background.png":
                            iconBackground = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            break;
                        case "icon_base.png":
                            iconBase = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            break;
                        case "icon_base_fx.png":
                            iconBaseFx = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            break;
                        case "icon_high.png":
                            iconHigh = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            break;
                        case "icon_high_fx.png":
                            iconHighFx = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                            break;
                    }
                }

                if (iconBackground == null)
                    skillsMissingIcons.Add(skill);
                else
                    skill.AssignIcons(iconBackground, iconBase, iconBaseFx, iconHigh, iconHighFx);
            }
            
            foreach (Ethel ethel in ethelScripts)
                ethel.AssignAllPossibleSkills(ethelSkills);
            
            List<Nema> nemaScripts = global::Core.Main_Database.Editor.Utils.FindAssetsByType<Nema>();
            SkillScriptable[] nemaSkills = skillDictionary.Where(pair => pair.Key.StartsWith("skill_nema")).Select(pair => pair.Value).ToArray();
            foreach (SkillScriptable skill in nemaSkills)
            {
                string path = AssetDatabase.GetAssetPath(skill);
                string directory = $"{path[..path.LastIndexOf('/')]}/Icons/";
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { directory });
                Sprite iconBackground = null, iconBase = null, iconBaseFx = null, iconHigh = null, iconHighFx = null;
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string assetName = assetPath[(assetPath.LastIndexOf('/') + 1)..];
                    switch (assetName)
                    {
                        case "icon_background.png": iconBackground = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); break;
                        case "icon_base.png":       iconBase = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); break;
                        case "icon_base_fx.png":    iconBaseFx = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); break;
                        case "icon_high.png":       iconHigh = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); break;
                        case "icon_high_fx.png":    iconHighFx = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath); break;
                    }
                }

                if (iconBackground == null)
                    skillsMissingIcons.Add(skill);
                else
                    skill.AssignIcons(iconBackground, iconBase, iconBaseFx, iconHigh, iconHighFx);
            }
            
            foreach (Nema nema in nemaScripts)
                nema.AssignAllPossibleSkills(nemaSkills);
            
            if (skillsMissingIcons.Count > 0)
            {
                StringBuilder builder = new();
                builder.AppendLine("-------------The following skills are missing icons-------------");
                foreach (SkillScriptable skill in skillsMissingIcons)
                    builder.AppendLine(skill.name);
                
                Debug.LogWarning(builder.ToString());
            }
        }

        private static void Perks()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out PerkDatabase perkDatabase);

            List<PerkScriptable> perks = global::Core.Main_Database.Editor.Utils.FindAssetsByType<PerkScriptable>();
            Dictionary<CleanString, PerkScriptable> perkDictionary = perks.ToDictionary(p => p.Key);
            Dictionary<CleanString, PerkScriptable> perksMissingDescription = perkDictionary.ToDictionary(p => p.Key, p => p.Value);
            
            List<PerkScriptable> perksMissingIcon = new();
            List<Sprite> icons = global::Core.Main_Database.Editor.Utils.GetAllAssetsOfTypeWhereNameStartsWith<Sprite>("icon_");
            foreach (PerkScriptable perk in perkDictionary.Values)
            {
                bool found = false;
                foreach (Sprite sprite in icons)
                {
                    if (perk.name.Contains(sprite.name.Replace("icon_", "").Trim().ToLowerInvariant()) == false)
                        continue;
                    
                    perk.AssignIcon(sprite);
                    found = true;
                    break;
                }
                
                if (found == false)
                    perksMissingIcon.Add(perk);
            }

            using StreamReader textReader = File.OpenText($"{Application.dataPath}{DescriptionsPath}");
            using CsvReader csvReader = new(textReader);
            foreach (global::Core.Main_Database.Editor.DataBuilder.DescriptionData data in csvReader.GetRecords<global::Core.Main_Database.Editor.DataBuilder.DescriptionData>())
            {
                if (perkDictionary.TryGetValue(data.Key, out PerkScriptable perk) == false)
                    continue;
                
                perk.AssignData(data.DisplayName, data.FlavorText, data.Description);
                perksMissingDescription.Remove(data.Key);
            }

            perkDatabase.AssignData(perks);

            StringBuilder stringBuilder = new();
            
            foreach (KeyValuePair<CleanString, PerkScriptable> pair in perksMissingDescription)
                if (pair.Key.Contains("ethel") || pair.Key.Contains("nema"))
                    stringBuilder.AppendLine(pair.Key.ToString());

            foreach (PerkScriptable perk in perksMissingIcon)
                if (perk.Key.Contains("ethel") || perk.Key.Contains("nema"))
                    stringBuilder.AppendLine(perk.Key.ToString());
            
            if (stringBuilder.Length > 0) 
                Debug.LogWarning($"-------The following perks are missing descriptions-------\n{stringBuilder}");
        }

        private static void Barks()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out BarkDatabase barkDatabase);

            Dictionary<(string, BarkType), List<string>> barkDictionary = new();

            using StreamReader textReader = File.OpenText($"{Application.dataPath}{BarksPath}");
            using CsvReader csvReader = new(textReader);
            foreach (global::Core.Main_Database.Editor.DataBuilder.BarkData data in csvReader.GetRecords<global::Core.Main_Database.Editor.DataBuilder.BarkData>())
            {
                if (barkDictionary.ContainsKey((data.CharacterKeyOne, data.BarkType)) == false)
                    barkDictionary[(data.CharacterKeyOne, data.BarkType)] = new List<string> { data.BarkOne };
                else
                    barkDictionary[(data.CharacterKeyOne, data.BarkType)].Add(data.BarkOne);
                
                if (barkDictionary.ContainsKey((data.CharacterKeyTwo, data.BarkType)) == false)
                    barkDictionary[(data.CharacterKeyTwo, data.BarkType)] = new List<string> { data.BarkTwo };
                else
                    barkDictionary[(data.CharacterKeyTwo, data.BarkType)].Add(data.BarkTwo);
            }

            barkDatabase.AssignData(barkDictionary);
        }

        private static void AudioFiles()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out AudioPathsDatabase audioDatabase);
            Dictionary<string, string> audioPaths = new(); // Key: file name, value: file path
            AudioClip[] allAudioFiles = Resources.LoadAll<AudioClip>("");
            foreach (AudioClip audioClip in allAudioFiles)
            {
                string path = AssetDatabase.GetAssetPath(audioClip);
                int resourcesIndex = path.IndexOf("Resources/", StringComparison.Ordinal);
                int extensionIndex = path.LastIndexOf(".", StringComparison.Ordinal);
                
                // get relative path to resources without extension
                
                string relativePath = path[(resourcesIndex + 10)..extensionIndex];
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                audioPaths[fileName] = relativePath;
            }

            audioDatabase.AssignData(audioPaths);
            EditorUtility.SetDirty(audioDatabase);
        }

        private static void CGs()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out CgDatabase cgDatabase);
            List<Sprite> sprites = global::Core.Main_Database.Editor.Utils.GetAllAssetsOfTypeWhereNameStartsWith<Sprite>("CG_");
            List<VisualNovelAnimation> animations = global::Core.Main_Database.Editor.Utils.FindComponentsByType<VisualNovelAnimation>();
            Dictionary<string, string> animationFilePaths = new(); // Key: file name, value: file path
            foreach (VisualNovelAnimation animation in animations)
            {
                string path = AssetDatabase.GetAssetPath(animation);
                int resourcesIndex = path.IndexOf("Resources/", StringComparison.Ordinal);
                int extensionIndex = path.LastIndexOf(".", StringComparison.Ordinal);
                
                // get relative path to resources without extension
                
                string relativePath = path[(resourcesIndex + 10)..extensionIndex];
                
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                animationFilePaths.Add(fileName, relativePath);
            }
            
            cgDatabase.AssignData(sprites, animationFilePaths);
        }

        private static void CombatEvents()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out CombatScriptDatabase combatScriptDatabase);
            List<ScriptableCombatSetupInfo> combatScriptableInfos = global::Core.Main_Database.Editor.Utils.FindAssetsByType<ScriptableCombatSetupInfo>();
            combatScriptDatabase.AssignData(combatScriptableInfos);
        }
        
        private static void RestEvents()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out RestEventsDatabase restEventsDatabase);
            List<RestDialogue> restDialogues = global::Core.Main_Database.Editor.Utils.FindAssetsByType<RestDialogue>();
            List<RestEventBackground> restEventBackgrounds = global::Core.Main_Database.Editor.Utils.FindComponentsByType<RestEventBackground>();
            restEventsDatabase.AssignData(restDialogues, restEventBackgrounds);
        }

        private static void Variables()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out VariableDatabase variableDatabase);
            List<SerializedVariable> variables = global::Core.Main_Database.Editor.Utils.FindAssetsByType<SerializedVariable>();
            variableDatabase.AssignData(variables);
        }

        private static void WorldYarnScenes()
        {
            global::Core.Main_Database.Editor.Utils.TryFindAssetWithType(out WorldScenesDatabase worldYarnDatabase);
            List<WorldYarnScene> worldYarnScenes = global::Core.Main_Database.Editor.Utils.FindAssetsByType<WorldYarnScene>();
            worldYarnDatabase.AssignData(worldYarnScenes);
        }
    }
}