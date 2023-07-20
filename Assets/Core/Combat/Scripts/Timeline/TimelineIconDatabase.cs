using System.Collections.Generic;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts.Timeline
{
    public class TimelineIconDatabase : SerializedScriptableObject
    {
        private static Main_Database.DatabaseManager Instance => Main_Database.DatabaseManager.Instance;

        [OdinSerialize, Required]
        private Dictionary<CombatEvent.Type, Sprite> _icons;
        
        public static Option<Sprite> GetIcon(CombatEvent.Type evenType) => Instance.TimelineIconDatabase._icons.TryGetValue(evenType, out Sprite sprite) ? sprite : Option.None;
    }
}