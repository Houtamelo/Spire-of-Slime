using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Game_Manager.Scripts
{
    public sealed class WorldTextCueManager : Singleton<WorldTextCueManager>
    {
        [SerializeField, Required]
        private WorldTextCue cuePrefab;
        
        [SerializeField, Required]
        private Transform cueParent;
        
        private static readonly List<WorldTextCue> Cues = new();

        public Sequence Show(WorldCueOptions options)
        {
            if (options.StopOthers)
                Cues.DoForEach(cue => cue.Hide());
            
            return GetCue().Show(options);
        }

        private WorldTextCue GetCue()
        {
            for (int i = 0; i < Cues.Count; i++)
            {
                WorldTextCue cue = Cues[i];
                if (!cue.IsBusy)
                    return cue;
            }

            return CreateCue();
        }

        private WorldTextCue CreateCue()
        {
            WorldTextCue cue = cuePrefab.InstantiateWithFixedLocalScale(cueParent);
            Cues.Add(cue);
            return cue;
        }
    }
}