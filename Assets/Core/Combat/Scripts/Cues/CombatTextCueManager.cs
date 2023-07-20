using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts.Cues
{
    public class CombatTextCueManager : Singleton<CombatTextCueManager>
    {
        [OdinSerialize]
        private readonly Transform _cueParents;

        [OdinSerialize]
        private readonly CombatTextCue _worldTextPrefab;

        private readonly List<CombatTextCue> _unboundedCues = new();
        private readonly Dictionary<DisplayModule, CombatTextCue> _characterDictionary = new();
        
        [NotNull]
        private CombatTextCue CreateCue()
        {
            CombatTextCue cue = _worldTextPrefab.InstantiateWithFixedLocalScale(_cueParents).OnCreate();
            _unboundedCues.Add(cue);
            return cue;
        }

        [NotNull]
        private CombatTextCue GetIdleCue()
        {
            foreach (CombatTextCue cue in _unboundedCues)
            {
                if (cue.IsIdle)
                    return cue;
            }

            return CreateCue();
        }

        public void IndependentCue(ref CombatCueOptions options)
        {
            if (options.CanShowOnTopOfOthers == false)
                Debug.LogWarning( "IndependentWorldCue should be used for independent cues only");
            
            GetIdleCue().Enqueue(ref options);
        }

        public void EnqueueAboveCharacter(ref CombatCueOptions options, DisplayModule character)
        {
            if (options.CanShowOnTopOfOthers)
            {
                GetIdleCue().Enqueue(ref options);
                return;
            }
            
            if (_characterDictionary.TryGetValue(character, out CombatTextCue cue))
            {
                cue.Enqueue(ref options);
            }
            else
            {
                cue = GetIdleCue();
                _characterDictionary[character] = cue;
                _unboundedCues.Remove(cue);
                cue.Enqueue(ref options);
            }
        }
    }
}