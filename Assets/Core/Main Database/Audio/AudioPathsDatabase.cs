using System.Collections.Generic;
using Core.ResourceManagement;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Main_Database.Audio
{
    public class AudioPathsDatabase : SerializedScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [OdinSerialize, Required] private Dictionary<string, string> _audioPaths = new();
        
        public void AssignData(Dictionary<string, string> audioPaths)
        {
            _audioPaths = audioPaths;
        }

        public Option<string> GetAudioPathInternal(string fileName) => _audioPaths.TryGetValue(fileName.ToLowerInvariant(), out string path) ? path : Option<string>.None;
        public static Option<string> GetAudioPath(string fileName) => Instance.AudioPathsDatabase.GetAudioPathInternal(fileName);


        [MustUseReturnValue]
        public static Result<ResourceHandle<AudioClip>> LoadClip(string fileName)
        {
            Option<string> audioPath = GetAudioPath(fileName);
            if (audioPath.IsNone)
                return Result<ResourceHandle<AudioClip>>.Error($"Audio file with name {fileName} not found");
            
            return ResourceHandle<AudioClip>.Load(audioPath.Value);
        }
    }
}