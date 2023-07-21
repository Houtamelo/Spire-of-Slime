using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Timeline
{
    public class TimelineManager : MonoBehaviour
    {
        private static readonly HashSet<StatusInstance> ReusableStatusSet = new(capacity: 8);
        private static readonly SelfSortingList<TimelineData> ReusableDataSortedList = new(capacity: 32, new DataTimeComparer());
        private static readonly HashSet<TimelineIcon> ReusableIconSet = new(capacity: 32);

        [SerializeField, Required, AssetsOnly]
        private TimelineIcon iconPrefab;

        [SerializeField, Required, SceneObjectsOnly]
        private Transform iconsParent;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterManager characters;

        [SerializeField]
        private int iconSpacing = 20;
        public int IconSpacing => iconSpacing;

        private readonly List<TimelineIcon> _spawnedIcons = new(capacity: 32);
        private readonly Dictionary<TimelineData, TimelineIcon> _activeIcons = new(32);
        private readonly SelfSortingList<CombatEvent> _events = new(capacity: 32, new EventTimeComparer());
        
        private TimelineIcon _targetingTemporaryIcon;

        [NotNull]
        private TimelineIcon CreateIcon()
        {
            TimelineIcon icon = iconPrefab.InstantiateWithFixedLocalScaleAndAnchoredPosition(iconsParent);
            icon.Initialize(timelineIconsManager: this);
            icon.gameObject.SetActive(false);
            _spawnedIcons.Add(icon);
            return icon;
        }

        private void Start()
        {
            for (int i = 0; i < 16; i++)
                CreateIcon();
            
            _targetingTemporaryIcon = iconPrefab.InstantiateWithFixedLocalScaleAndAnchoredPosition(iconsParent);
            _targetingTemporaryIcon.Initialize(timelineIconsManager: this);
            _targetingTemporaryIcon.gameObject.SetActive(false);
        }

        public void ShowTemporaryTargetingIcon([NotNull] CharacterStateMachine caster, TSpan chargeTime, [NotNull] ISkill skill)
        {
            ReusableDataSortedList.Clear();

            foreach (TimelineData data in _activeIcons.Keys)
                ReusableDataSortedList.Add(data);
            
            TimelineData temporaryChargeData = new(caster, chargeTime, CombatEvent.GetActionDescription(), HashCode.Combine(caster.GetHashCode(), skill.GetHashCode()), CombatEvent.Type.Action);
            ReusableDataSortedList.Add(temporaryChargeData);
            
            int temporaryIndex = ReusableDataSortedList.IndexOf(temporaryChargeData);

            if (temporaryIndex == -1)
            {
                Debug.LogWarning("Temporary charge data that was just generated not found in ReusableDataSortedList.");
                return;
            }
            
            _targetingTemporaryIcon.SetData(temporaryChargeData);
            _activeIcons[temporaryChargeData] = _targetingTemporaryIcon;

            for (int i = 0; i < ReusableDataSortedList.Count; i++)
            {
                TimelineData data = ReusableDataSortedList[i];
                TimelineIcon icon = _activeIcons[data];
                icon.SetTimelinePosition(i);
            }
            
            _activeIcons.RemoveValue(_targetingTemporaryIcon);
        }

        public void HideTemporaryTargetingIcon()
        {
            _targetingTemporaryIcon.Deactivate();
            
            ReusableDataSortedList.Clear();

            foreach (TimelineData data in _activeIcons.Keys)
                ReusableDataSortedList.Add(data);
            
            for (int i = 0; i < ReusableDataSortedList.Count; i++)
            {
                TimelineData data = ReusableDataSortedList[i];
                TimelineIcon icon = _activeIcons[data];
                icon.SetTimelinePosition(i);
            }
        }
        
        /// <returns> The next event. </returns>
        public CombatEvent UpdateEvents()
        {
            _events.Clear();
            foreach (CharacterStateMachine character in characters.GetAllFixed())
                character.FillTimelineEvents(in _events);
            
            SelfSortingList<TimelineData> dataPivot = ReusableDataSortedList;
            ConvertEventsToData(_events, dataPivot);
            for (int i = _spawnedIcons.Count; i < dataPivot.Count; i++)
                CreateIcon();

            HashSet<TimelineIcon> availableIcons = ReusableIconSet;
            availableIcons.Clear();
            availableIcons.Add(_spawnedIcons);
            for (int i = 0; i < dataPivot.Count; i++)
            {
                TimelineData data = dataPivot[i];
                if (_activeIcons.TryGetValue(data, out TimelineIcon icon) == false)
                    icon = availableIcons.First();

                icon.SetData(data);
                icon.SetTimelinePosition(i);
                availableIcons.Remove(icon);
                _activeIcons[data] = icon;
            }
            
            foreach (TimelineIcon icon in availableIcons)
            {
                icon.Deactivate();
                _activeIcons.RemoveValue(icon);
            }

            if (_events.Count == 0)
            {
                Debug.LogError("No events in timeline. Stepping dummy 1 second.");
                
                return CombatEvent.FromTurn(characters.GetLeftEditable().Count > 0 ? characters.GetLeftEditable()[0] 
                                            : characters.GetRightEditable().Count > 0 ? characters.GetRightEditable()[0] : throw new Exception("No characters in combat, it should end."), 
                                            TSpan.OneSecond);
            }
            
            return _events[0];
        }

        private static void ConvertEventsToData([NotNull] in SelfSortingList<CombatEvent> source, [NotNull] in SelfSortingList<TimelineData> target)
        {
            target.Clear();
            for (int index = 0; index < source.Count; index++)
            {
                CombatEvent current = source[index];

                switch (current.EventType)
                {
                    case CombatEvent.Type.Turn:
                    case CombatEvent.Type.StunEnd:
                    case CombatEvent.Type.DownedEnd:
                        target.Add(new TimelineData(current.Owner, current.Time, current.GetDescription(), HashCode.Combine(current.Source.GetHashCode(), current.EventType), current.EventType));
                        continue;
                    case CombatEvent.Type.Action:
                        target.Add(new TimelineData(current.Owner, current.Time, current.GetDescription(), HashCode.Combine(current.Source.GetHashCode(), current.EventType, current.Action), CombatEvent.Type.Action));
                        continue;
                    case CombatEvent.Type.PoisonTick:
                    {
                        int aggregatedPoison = current.PoisonAmount;
                        TSpan maxTime = current.Time;
                        int aggregatedHash = current.Source.GetHashCode();
                        while (index < source.Count - 1)
                        {
                            CombatEvent next = source[index + 1];
                            if (next.Owner != current.Owner || next.EventType != CombatEvent.Type.PoisonTick)
                                break;

                            maxTime = TSpan.ChoseMax(maxTime, next.Time);
                            aggregatedPoison += next.PoisonAmount;
                            aggregatedHash ^= next.Source.GetHashCode();
                            index++;
                        }
                        
                        aggregatedHash = HashCode.Combine(aggregatedHash, CombatEvent.Type.PoisonTick);

                        if (aggregatedPoison != 0)
                            target.Add(new TimelineData(current.Owner, maxTime, CombatEvent.GetPoisonTickDescription(aggregatedPoison), aggregatedHash, CombatEvent.Type.PoisonTick));

                        break;
                    }
                    case CombatEvent.Type.LustTick:
                    {
                        int aggregatedLust = current.LustDelta;
                        TSpan maxTime = current.Time;
                        int aggregatedHash = current.Source.GetHashCode();
                        while (index < source.Count - 1)
                        {
                            CombatEvent next = source[index + 1];
                            if (next.Owner != current.Owner || next.EventType != CombatEvent.Type.LustTick)
                                break;

                            maxTime = TSpan.ChoseMax(maxTime, next.Time);
                            aggregatedLust += next.LustDelta;
                            aggregatedHash ^= next.Source.GetHashCode();
                            index++;
                        }
                        
                        aggregatedHash = HashCode.Combine(aggregatedHash, CombatEvent.Type.LustTick);
                        if (aggregatedLust != 0)
                            target.Add(new TimelineData(current.Owner, maxTime, CombatEvent.GetLustTickDescription(aggregatedLust), aggregatedHash, CombatEvent.Type.LustTick));

                        break;
                    }
                    case CombatEvent.Type.HealTick:
                    {
                        int aggregatedHeal = current.HealAmount;
                        TSpan maxTime = current.Time;
                        int aggregatedHash = current.Source.GetHashCode();
                        while (index < source.Count - 1)
                        {
                            CombatEvent next = source[index + 1];
                            if (next.Owner != current.Owner || next.EventType != CombatEvent.Type.HealTick)
                                break;

                            maxTime = TSpan.ChoseMax(maxTime, next.Time);
                            aggregatedHeal += next.HealAmount;
                            aggregatedHash ^= next.Source.GetHashCode();
                            index++;
                        }
                        
                        aggregatedHash = HashCode.Combine(aggregatedHash, CombatEvent.Type.HealTick);
                        if (aggregatedHeal != 0)
                            target.Add(new TimelineData(current.Owner, maxTime, CombatEvent.GetHealTickDescription(aggregatedHeal), aggregatedHash, CombatEvent.Type.HealTick));

                        break;
                    }
                    case CombatEvent.Type.StatusEnd:
                    {
                        ReusableStatusSet.Clear();
                        ReusableStatusSet.Add(current.Status);
                        TSpan maxTime = current.Time;
                        int aggregatedHash = current.Source.GetHashCode();
                        while (index < source.Count - 1)
                        {
                            CombatEvent next = source[index + 1];
                            if (next.Owner != current.Owner || next.EventType != CombatEvent.Type.StatusEnd)
                                break;

                            maxTime = TSpan.ChoseMax(maxTime, next.Time);
                            aggregatedHash ^= next.Source.GetHashCode();
                            ReusableStatusSet.Add(next.Status);
                            index++;
                        }

                        if (ReusableStatusSet.Count == 1)
                        {
                            target.Add(new TimelineData(current.Owner, current.Time, current.GetDescription(), HashCode.Combine(current.Source.GetHashCode(), current.EventType), CombatEvent.Type.StatusEnd));
                        }
                        else
                        {
                            aggregatedHash = HashCode.Combine(aggregatedHash, CombatEvent.Type.StatusEnd);
                            string description = CombatEvent.GetMultipleStatusEndDescription(ReusableStatusSet);
                            target.Add(new TimelineData(current.Owner, maxTime, description, aggregatedHash, CombatEvent.Type.StatusEnd));
                        }
                        
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException(nameof(current.EventType), current.EventType, null);
                }
            }
        }

        private sealed class EventTimeComparer : IComparer<CombatEvent>
        {
            public int Compare(CombatEvent x, CombatEvent y) => x.Time.CompareTo(y.Time);
        }
        
        private sealed class DataTimeComparer : IComparer<TimelineData>
        {
            public int Compare(TimelineData x, TimelineData y) => x.MaxTime.CompareTo(y.MaxTime);
        }
    }
}