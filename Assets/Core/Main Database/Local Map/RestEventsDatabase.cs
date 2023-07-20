using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Events.Rest;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Database.Local_Map
{
    public class RestEventsDatabase : SerializedScriptableObject
    {
        public static bool LOG;
        
        private static readonly System.Random Randomizer = new();
        private static readonly List<RestDialogue> ReusableDialogueList = new();
        
        private static DatabaseManager Instance => DatabaseManager.Instance;

        [SerializeField, Required] 
        private RestDialogue[] restDialogues;

        [SerializeField, Required]
        private RestEventBackground[] backgrounds;

        [System.Diagnostics.Contracts.Pure]
        public static Option<RestEventBackground> GetBackgroundPrefab(BothWays location)
        {
            RestEventsDatabase database = Instance.RestEventsDatabase;
            for (int i = 0; i < database.backgrounds.Length; i++)
            {
                RestEventBackground background = database.backgrounds[i];
                if (background.Location == location)
                {
                    if (LOG)
                        Debug.Log($"Found background for {location}, named: {background.name}", context: background);
                        
                    return Option<RestEventBackground>.Some(background);
                }
            }

            if (LOG)
                Debug.LogWarning($"No background found for {location}", context: database);

            return Option<RestEventBackground>.None;
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<RestDialogue> GetAvailableDialogue()
        {
            RestEventsDatabase database = Instance.RestEventsDatabase;
            ReusableDialogueList.Clear();
            int highestPriority = int.MinValue;

            for (int i = 0; i < database.restDialogues.Length; i++)
            {
                RestDialogue restDialogue = database.restDialogues[i];
                if (!restDialogue.IsAvailable())
                    continue;

                if (restDialogue.Priority == highestPriority)
                {
                    ReusableDialogueList.Add(restDialogue);
                }
                else if (restDialogue.Priority > highestPriority)
                {
                    ReusableDialogueList.Clear();
                    ReusableDialogueList.Add(restDialogue);
                    highestPriority = restDialogue.Priority;
                }
            }
            
            if (ReusableDialogueList.Count == 0)
                return Option<RestDialogue>.None;

            return Option<RestDialogue>.Some(ReusableDialogueList[Randomizer.Next(0, ReusableDialogueList.Count)]);
        }

#if UNITY_EDITOR
        public void AssignData([NotNull] IList<RestDialogue> dialogues, [NotNull] IList<RestEventBackground> bgs)
        {
            restDialogues = dialogues.ToArray();
            backgrounds = bgs.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}