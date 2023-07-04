using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts.Events;
using Core.Local_Map.Scripts.Events.Combat;
using Core.Local_Map.Scripts.Events.Dialogue;
using Core.Local_Map.Scripts.Events.ReachLocation;
using Core.Local_Map.Scripts.Events.Rest;
using Core.World_Map.Scripts;
using KGySoft.CoreLibraries;
using ListPool;
using Save_Management;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

// ReSharper disable Unity.RedundantHideInInspectorAttribute

namespace Main_Database.Local_Map
{
    public sealed class MapEventDatabase : ScriptableObject
    {
        public const float DangerMultiplier = 1.5f;
        
        private static DatabaseManager Instance => DatabaseManager.Instance;

        public static readonly ILocalMapEvent DefaultCombatEvent = new DefaultCombatEvent();
        public static readonly ILocalMapEvent DefaultRestEvent = new DefaultRestEvent();
        private static readonly IReadOnlyDictionary<LocationEnum, DefaultReachLocationEvent> DefaultReachLocationEvents = Enum<LocationEnum>.GetValues()
            .ToDictionary(keySelector: location => location, elementSelector: location => new DefaultReachLocationEvent(location));

        public static ILocalMapEvent GetDefaultReachLocationEvent(LocationEnum location) => DefaultReachLocationEvents[location];

        [SerializeField, Required]
        private ScriptableLocalMapEvent[] allEvents;

        private readonly Dictionary<CleanString, ILocalMapEvent> _mappedEvents = new();

        [OdinSerialize, HideInInspector]
        private YarnSceneEvent[][] _yarnSceneEventsOrderedDescendingByPriority;

        public static Option<ILocalMapEvent> GetEvent(CleanString key)
        {
            if (Instance.MapEventDatabase._mappedEvents.TryGetValue(key, out ILocalMapEvent mapEvent))
                return Option<ILocalMapEvent>.Some(mapEvent);
            
            return Option.None;
        }

        public static Option<YarnSceneEvent> GetHighestPriorityAvailableEvent(in OneWay path)
        {
            for (int index = 0; index < Instance.MapEventDatabase._yarnSceneEventsOrderedDescendingByPriority.Length; index++)
            {
                YarnSceneEvent[] yarnSceneEvents = Instance.MapEventDatabase._yarnSceneEventsOrderedDescendingByPriority[index];
                for (int i = 0; i < yarnSceneEvents.Length; i++)
                {
                    YarnSceneEvent yarnSceneEvent = yarnSceneEvents[i];
                    Option<int> availableOption = yarnSceneEvent.IsAvailable(path);
                    if (availableOption.IsSome)
                        return Option<YarnSceneEvent>.Some(yarnSceneEvent);
                }
            }
            
            return Option<YarnSceneEvent>.None;
        }
        
        public static Option<YarnSceneEvent[]> GetAllAvailableEventsOrderedByPriority(in OneWay path)
        {
            using ListPool<YarnSceneEvent> availableEvents = new();
            {
                for (int index = 0; index < Instance.MapEventDatabase._yarnSceneEventsOrderedDescendingByPriority.Length; index++)
                {
                    YarnSceneEvent[] yarnSceneEvents = Instance.MapEventDatabase._yarnSceneEventsOrderedDescendingByPriority[index];
                    for (int i = 0; i < yarnSceneEvents.Length; i++)
                    {
                        YarnSceneEvent yarnSceneEvent = yarnSceneEvents[i];
                        Option<int> availableOption = yarnSceneEvent.IsAvailable(path);
                        if (availableOption.IsSome)
                            availableEvents.Add(yarnSceneEvent);
                    }
                }

                if (availableEvents.Count > 0)
                {
                    YarnSceneEvent[] array = availableEvents.ToArray();
                    return Option<YarnSceneEvent[]>.Some(array);
                }
            }
            
            return Option<YarnSceneEvent[]>.None;
        }

        public void Initialize()
        {
            foreach (ScriptableLocalMapEvent localMapEvent in allEvents)
                _mappedEvents.Add(localMapEvent.Key, localMapEvent);
            
            _mappedEvents.Add(DefaultCombatEvent.Key, DefaultCombatEvent);
            _mappedEvents.Add(DefaultRestEvent.Key, DefaultRestEvent);
            
            foreach (DefaultReachLocationEvent defaultReachLocationEvent in DefaultReachLocationEvents.Values)
                _mappedEvents.Add(defaultReachLocationEvent.Key, defaultReachLocationEvent);
        }
        
#if UNITY_EDITOR
        public void AssignData(ICollection<ScriptableLocalMapEvent> mapEvents)
        {
            allEvents = mapEvents.ToArray();
            
            List<YarnSceneEvent> yarnSceneEvents = mapEvents.Where(e => e is YarnSceneEvent).Cast<YarnSceneEvent>().ToList();
            // group yarn scene events by their priority which is an integer and then sort them by their priority, the collection is an array of arrays
            
            _yarnSceneEventsOrderedDescendingByPriority = yarnSceneEvents
                .GroupBy(e => e.GetPriority)
                .Select(s => s.ToArray())
                .OrderByDescending(e => e[0].GetPriority).ToArray();

            UnityEditor.EditorUtility.SetDirty(target: this);
        }
#endif
    }
}