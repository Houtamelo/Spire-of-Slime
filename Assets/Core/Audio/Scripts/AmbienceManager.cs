using System;
using System.Collections.Generic;
using Core.Main_Database.Audio;
using Core.ResourceManagement;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Audio.Scripts
{
    public sealed class AmbienceManager : Singleton<AmbienceManager>
    {
        [OdinSerialize, Required, AssetsOnly] 
        private AudioSource _ambiencePrefab;
        
        [NonSerialized] 
        private readonly List<AudioSource> _ambienceSources = new();
        private readonly List<ResourceHandle<AudioClip>> _loadedAudioClips = new();
        private TweenCallback _clearInactiveSources;

        protected override void Awake()
        {
            base.Awake();
            _clearInactiveSources = ClearInactiveSources;
        }

        private void Start()
        {
            for (int i = 0; i < 4; i++)
                CreateSource();
        }

        private AudioSource CreateSource()
        {
            AudioSource source = Instantiate(_ambiencePrefab, transform);
            _ambienceSources.Add(source);
            return source;
        }

        private AudioSource GetIdleSource()
        {
            foreach (AudioSource source in _ambienceSources)
                if (source.isPlaying == false)
                    return source;

            return CreateSource();
        }

        public void Set(string fileName, float volume, bool loop)
        {
            End();

            AudioSource mainSource = GetIdleSource();
            PlayClipOnSource(mainSource, fileName, volume, loop);
        }

        public void Add(string filename, float volume, bool loop)
        {
            AudioSource source = GetIdleSource();
            PlayClipOnSource(source, filename, volume, loop);
        }

        public void End()
        {
            foreach (AudioSource source in _ambienceSources)
                if (source.isPlaying && source.volume > 0)
                    source.DOFade(endValue: 0, MusicManager.FadeDuration).SetSpeedBased().OnComplete(_clearInactiveSources);

            ReleaseHandles();
        }

        private void PlayClipOnSource(AudioSource source, string fileName, float volume, bool loop)
        {
            Result<AudioClip> operationResult = LoadClip(fileName);
            if (operationResult.IsOk)
                PlayClipOnSource(source, operationResult.Value, volume, loop);
            else
                Debug.LogWarning(operationResult.Reason);
        }

        private void PlayClipOnSource(AudioSource source, AudioClip clip, float volume, bool loop)
        {
            source.clip = clip;
            source.volume = 0.01f;
            source.DOFade(endValue: volume, MusicManager.FadeDuration).SetSpeedBased();
            source.loop = loop;
            source.Play();
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

        private void ClearInactiveSources()
        {
            foreach (AudioSource audioSource in _ambienceSources)
                if (audioSource.volume <= 0)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
        }
    }
}