using System.Collections;
using System.Collections.Generic;
using Core.Game_Manager.Scripts;
using Core.Main_Database.Audio;
using Core.ResourceManagement;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Audio.Scripts
{
    public sealed class SfxManager : Singleton<SfxManager>
    {
        [OdinSerialize, Required, SceneObjectsOnly]
        private AudioSource _audioSource;
        
        private readonly List<ResourceHandle<AudioClip>> _loadedAudioClips = new();
        private void Start()
        {
            GameManager.OnRootEnabled += OnRootEnabled;
        }

        protected override void OnDestroy()
        {
            GameManager.OnRootEnabled -= OnRootEnabled;
            base.OnDestroy();
        }

        public IEnumerator PlayAndWait(string fileName, float volume = 1)
        {
            Result<AudioClip> operationResult = LoadClip(fileName);
            if (operationResult.IsErr)
            {
                Debug.LogWarning(operationResult.Reason);
                return null;
            }

            AudioClip clip = operationResult.Value;
            PlaySingle(clip, volume);
            return new YieldableCommandWrapper(new WaitForSeconds(clip.length).AsEnumerator(), allowImmediateFinish: true, onImmediateFinish: null);
        }

        public void PlayMulti(string fileName, float volume)
        {
            Result<AudioClip> operationResult = LoadClip(fileName);
            if (operationResult.IsOk)
                _audioSource.PlayOneShot(operationResult.Value, volume);
            else
                Debug.LogWarning(operationResult.Reason);
        }

        public void PlayMulti(AudioClip clip, float volume)
        {
            _audioSource.PlayOneShot(clip, volume);
        }

        public void PlaySingle(string fileName, float volume)
        {
            Result<AudioClip> operationResult = LoadClip(fileName);
            if (operationResult.IsOk)
                PlaySingle(operationResult.Value, volume);
            else
                Debug.LogWarning(operationResult.Reason);
        }

        public void PlaySingle(AudioClip clip, float volume)
        {
            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.volume = volume;
            _audioSource.Play();
        }

        private Result<AudioClip> LoadClip(string fileName)
        {
            Result<ResourceHandle<AudioClip>> handle = AudioPathsDatabase.LoadClip(fileName);
            if (handle.IsOk)
            {
                _loadedAudioClips.Add(handle.Value);
                return handle.Value.Resource;
            }
            
            return Result<AudioClip>.Error(handle.Reason);
        }

        private void ReleaseHandles()
        {
            foreach (ResourceHandle<AudioClip> handle in _loadedAudioClips)
                handle.Dispose();

            _loadedAudioClips.Clear();
        }

        private void OnRootEnabled(SceneRef _) => ReleaseHandles();
    }
}