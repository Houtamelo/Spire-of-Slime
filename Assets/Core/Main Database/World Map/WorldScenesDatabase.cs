using System;
using System.Collections.Generic;
using System.Linq;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Main_Database.World_Map
{
    public class WorldScenesDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;

        [SerializeField]
        private YarnProject yarnProject;

        [SerializeField, Required]
        private WorldYarnScene[] serializedScenes;

        private readonly Dictionary<LocationEnum, WorldYarnScene[]> _mappedScenes = new(Enum<LocationEnum>.GetValues().Length);

        public void Initialize()
        {
            Dictionary<LocationEnum, List<WorldYarnScene>> worldScenesTemp = Enum<LocationEnum>.GetValues().ToDictionary(keySelector: l => l, elementSelector: _ => new List<WorldYarnScene>());
            foreach (WorldYarnScene scene in serializedScenes)
            {
                worldScenesTemp[scene.Location].Add(scene);
#if UNITY_EDITOR
                if (yarnProject.Program.Nodes.ContainsKey(scene.SceneName) == false)
                    Debug.LogWarning($"Scene {scene.name} has no corresponding Yarn node", context: scene);
#endif
            }

            foreach ((LocationEnum location, List<WorldYarnScene> unsortedList) in worldScenesTemp)
            {
                WorldYarnScene[] sortedArray = unsortedList.ToArray();
                Array.Sort(sortedArray, (a, b) => b.Priority.CompareTo(a.Priority));
                _mappedScenes[location] = sortedArray;
                
#if UNITY_EDITOR
                for (int i = 0; i < sortedArray.Length - 1; i++)
                {
                    if (sortedArray[i] == sortedArray[i + 1])
                        Debug.LogWarning($"Two scenes with same priority and location: {sortedArray[i].name} - {sortedArray[i + 1].name}", context: sortedArray[i]);
                }
#endif
            }
        }

        public static Option<WorldYarnScene> GetWorldScene(LocationEnum location)
        {
            if (Save.AssertInstance(out Save save) == false)
                return Option<WorldYarnScene>.None;
            
            foreach (WorldYarnScene scene in Instance.WorldScenesDatabase._mappedScenes[location])
            {
                if (scene.AreRequirementsMet(save))
                    return Option<WorldYarnScene>.Some(scene);
            }
            
            return Option<WorldYarnScene>.None;
        }

#if UNITY_EDITOR
        public void AssignData([NotNull] IEnumerable<WorldYarnScene> worldYarnScenes)
        {
            serializedScenes = worldYarnScenes.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        // Bellow is some over engineered unecessary bs, didn't delete cause it's actually impressive
        
        /*private const string WorldSceneTag = "world";
        private const string PriorityTag = "priority";
        private const string LocationTag = "location";
        private const string RequirementTag = "req";
        private static readonly string[] ComparisonOperators = {"<<", ">>", "<=", ">=", "==", "!="};
        public void Initialize()
        {
            _worldScenes.Add(Enum<LocationEnum>.GetValues().ToDictionary(keySelector: l => l, elementSelector: _ => new List<WorldSceneData>()));

            _yarnProgram = yarnProject.Program;
            foreach ((string key, Node node) in _yarnProgram.Nodes)
            {
                bool isWorld = false;
                bool hasPriority = false;
                bool hasLocation = false;
                LocationEnum location = default;
                int priority = 0;
                foreach (string nodeTag in node.Tags)
                {
                    if (nodeTag.Contains(WorldSceneTag))
                    {
                        isWorld = true;
                    }
                    else if (nodeTag.Contains(PriorityTag))
                    {
                        ReadOnlySpan<char> span = nodeTag.AsSpan();
                        ReadOnlySpan<char> priorityString = span[(span.IndexOf(':') + 1)..];
                        Option<int> priorityOption = priorityString.ParseInt();
                        if (priorityOption.IsSome)
                        {
                            priority = priorityOption.Value;
                            hasPriority = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Could not parse number from priority tag: {nodeTag}");
                        }
                    }
                    else if (nodeTag.Contains(LocationTag))
                    {
                        ReadOnlySpan<char> span = nodeTag.AsSpan();
                        ReadOnlySpan<char> locationString = span[(span.IndexOf(':') + 1)..];
                        Option<LocationEnum> locationOption = locationString.ToString().ParseEnum<LocationEnum>();
                        if (locationOption.IsSome)
                        {
                            location = locationOption.Value;
                            hasLocation = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Could not parse location from location tag: {nodeTag}");
                        }
                    }
                }
                
                switch (isWorld)
                {
                    case true when hasPriority && hasLocation:
                        _worldScenes[location].Add(new WorldSceneData(key, priority));
                        break;
                    case true when !hasPriority && !hasLocation:
                        Debug.LogWarning($"Node has world tag but no priority or location tags! Key: {key}");
                        break;
                    case true when !hasPriority:
                        Debug.LogWarning($"Node has world tag but no priority tag! Key: {key}");
                        break;
                    case true:
                        Debug.LogWarning($"Node has world tag but no location tag! Key: {key}");
                        break;
                }
            }
            
            foreach ((_, List<WorldSceneData> list) in _worldScenes) 
                list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            AssertAllScenesHaveCorrectRequirementOperators();
        }

        public static Option<string> GetWorldScene(LocationEnum location)
        {
            WorldScenesDatabase database = Instance.WorldScenesDatabase;
            Save save = Save.Current;
            if (save == null)
            {
                Debug.LogWarning("Save is null!", database);
                return Option<string>.None;
            }
            
            List<WorldSceneData> list = database._worldScenes[location];
            if (list.Count == 0)
                return Option<string>.None;

            for (int i = 0; i < list.Count; i++)
            {
                WorldSceneData worldSceneData = list[i];
                Node node = database._yarnProgram.Nodes[worldSceneData.Key];
                bool passedRequirement = true;
                foreach (string nodeTag in node.Tags)
                {
                    if (!nodeTag.Contains(RequirementTag))
                        continue;
                    
                    ReadOnlySpan<char> span = nodeTag.AsSpan();
                    int doubleCommaIndex = span.IndexOf(':');
                    int comparisonIndex = 0;
                    for (int j = 0; j < ComparisonOperators.Length; j++)
                    {
                        int index = span.IndexOf(ComparisonOperators[j]);
                        if (index != -1)
                        {
                            comparisonIndex = index;
                            break;
                        }
                    }
                    string comparisonOperator = span[comparisonIndex..(comparisonIndex + 2)].ToString();
                    string variableName = span[(doubleCommaIndex + 1)..comparisonIndex].ToString();
                    ReadOnlySpan<char> valueToCompare = span[(comparisonIndex + 2)..];
                    
                    Option<float> parsedFloat = valueToCompare.ParseFloat();
                    if (parsedFloat.IsSome)
                    {
                        Option<float> variableOption = save.GetVariable<float>(variableName);
                        if (variableOption.IsSome)
                        {
                            float variable = variableOption.Value;
                            float parsedValue = parsedFloat.Value;
                            bool result = comparisonOperator switch
                            {
                                "<<" => variable < parsedValue,
                                ">>" => variable > parsedValue,
                                "<=" => variable <= parsedValue,
                                ">=" => variable >= parsedValue,
                                "==" => Math.Abs(variable - parsedValue) < 0.00001f,
                                "!=" => Math.Abs(variable - parsedValue) > 0.00001f,
                                _    => throw new ArgumentOutOfRangeException(comparisonOperator, "Impossible")
                            };
                            
                            if (result)
                                continue;
                            
                            passedRequirement = false;
                            break;
                        }
                    }
                    
                    Option<bool> parsedBool = valueToCompare.ParseBool();
                    if (parsedBool.IsSome)
                    {
                        Option<bool> variableOption = save.GetVariable<bool>(variableName);
                        if (variableOption.IsSome)
                        {
                            bool variable = variableOption.Value;
                            bool parsedValue = parsedBool.Value;
                            bool result = comparisonOperator switch
                            {
                                "==" => variable == parsedValue,
                                "!=" => variable != parsedValue,
                                _    => throw new ArgumentOutOfRangeException(comparisonOperator, "Cannot compare bools with this operator")
                            };

                            if (result)
                                continue;
                            
                            passedRequirement = false;
                            break;
                        }
                    }

                    {
                        Option<string> variableOption = save.GetVariable<string>(variableName);
                        if (variableOption.IsSome)
                        {
                            string variable = variableOption.Value;
                            string parsedValue = valueToCompare.ToString();
                            bool result = comparisonOperator switch
                            {
                                "==" => variable == parsedValue,
                                "!=" => variable != parsedValue,
                                _    => throw new ArgumentOutOfRangeException(comparisonOperator, "Cannot compare strings with this operator")
                            };
                            
                            if (result)
                                continue;
                            
                            passedRequirement = false;
                            break;
                        }

                        Debug.LogWarning($"Could not find variable {variableName} in save!", database);
                    }
                }
                
                if (passedRequirement)
                    return worldSceneData.Key;
            }
            
            return Option<string>.None;
        }

        private void AssertAllScenesHaveCorrectRequirementOperators()
        {
            foreach (KeyValuePair<LocationEnum, List<WorldSceneData>> worldSceneData in _worldScenes)
            {
                foreach (WorldSceneData data in worldSceneData.Value)
                {
                    Node node = _yarnProgram.Nodes[data.Key];
                    foreach (string nodeTag in node.Tags)
                    {
                        if (!nodeTag.Contains(RequirementTag))
                            continue;
                        
                        ReadOnlySpan<char> span = nodeTag.AsSpan();
                        int comparisonIndex = 0;
                        for (int i = 0; i < ComparisonOperators.Length; i++)
                        {
                            int index = span.IndexOf(ComparisonOperators[i]);
                            if (index != -1)
                            {
                                comparisonIndex = index;
                                break;
                            }
                        }
                        
                        if (comparisonIndex == 0)
                        {
                            Debug.LogError($"Node {data.Key} has a requirement tag but no comparison operator!", this);
                            return;
                        }
                        
                        ReadOnlySpan<char> valueToCompare = span[(comparisonIndex + 2)..];
                        string comparisonOperator = span[comparisonIndex..(comparisonIndex + 2)].ToString();
                        
                        Option<float> parsedFloat = valueToCompare.ParseFloat();
                        if (parsedFloat.IsSome)
                            continue;
                        
                        Option<bool> parsedBool = valueToCompare.ParseBool();
                        if (parsedBool.IsSome)
                        {
                            switch (comparisonOperator)
                            {
                                case "==" or "!=":
                                    continue;
                                default:
                                    Debug.LogError($"Node {data.Key} has a requirement tag with a bool value but an invalid comparison operator: {comparisonOperator}", this);
                                    return;
                            }
                        }
                        
                        switch (comparisonOperator)
                        {
                            case "==" or "!=":
                                continue;
                            default:
                                Debug.LogError($"Node {data.Key} has a requirement tag with a string value but an invalid comparison operator: {comparisonOperator}", this);
                                return;
                        }
                    }
                }
            }
        }

        private readonly struct WorldSceneData
        {
            public readonly string Key;
            public readonly int Priority;
            public WorldSceneData(string key, int priority)
            {
                Key = key;
                Priority = priority;
            }
        }*/
    }
}