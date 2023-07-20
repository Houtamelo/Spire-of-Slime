using System.Collections.Generic;
using System.Linq;
using Core.Audio.Scripts.MusicControllers;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Main_Database.Audio
{
    public class MusicDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;

        [SerializeField]
        private MusicController[] controllers = new MusicController[0];
        
        private readonly Dictionary<CleanString, MusicController> _mappedControllers = new(32);
        private readonly Dictionary<BothWays, MusicController> _localMapControllers = new(32);

        public static Option<MusicController> GetController(CleanString key) 
            => Instance.MusicDatabase._mappedControllers.TryGetValue(key, out MusicController controller) ? Option<MusicController>.Some(controller) : Option.None;

        public static Option<MusicController> GetStandardControllerForPath(BothWays bothWays)
            => Instance.MusicDatabase._localMapControllers.TryGetValue(bothWays, out MusicController controller) ? Option<MusicController>.Some(controller) : Option.None;

        public void Initialize()
        {
            foreach (MusicController controller in controllers)
            {
                _mappedControllers.Add(controller.Key, controller);
                
                if (controller.BelongsToLocalMap.TrySome(out BothWays location))
                    _localMapControllers.Add(location, controller);
            }
        }
        
#if UNITY_EDITOR
        public void AssignData([NotNull] IEnumerable<MusicController> foundControllers)
        {
            controllers = foundControllers.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}