using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Utils.Objects
{
    [ExecuteInEditMode]
    public class CustomAudioSource : MonoBehaviour
    {
        [SerializeField, Required]
        private AudioSource source;

        [SerializeField]
        private bool randomizePitch;

        [SerializeField, ShowIf(nameof(randomizePitch))]
        private float pitchRadius = 0.1f;

        [SerializeField, ShowIf(nameof(randomizePitch))]
        private float defaultPitch;
        
        public bool IsPlaying => source.isPlaying;
        
        public float Volume
        {
            get => source.volume;
            set => source.volume = value;
        }
        
        public float Pitch
        {
            get => source.pitch;
            set
            {
                source.pitch = value;
                defaultPitch = value;
            }
        }
        
        public float PanStereo
        {
            get => source.panStereo;
            set => source.panStereo = value;
        }
        
        public float SpatialBlend
        {
            get => source.spatialBlend;
            set => source.spatialBlend = value;
        }
        
        public float ReverbZoneMix
        {
            get => source.reverbZoneMix;
            set => source.reverbZoneMix = value;
        }
        
        public float DopplerLevel
        {
            get => source.dopplerLevel;
            set => source.dopplerLevel = value;
        }
        
        public float Spread
        {
            get => source.spread;
            set => source.spread = value;
        }
        
        public float MinDistance
        {
            get => source.minDistance;
            set => source.minDistance = value;
        }
        
        public float MaxDistance
        {
            get => source.maxDistance;
            set => source.maxDistance = value;
        }
        
        public bool Loop
        {
            get => source.loop;
            set => source.loop = value;
        }
        
        public bool Mute
        {
            get => source.mute;
            set => source.mute = value;
        }
        
        public AudioClip Clip
        {
            get => source.clip;
            set => source.clip = value;
        }
        
        public AudioMixerGroup OutputAudioMixerGroup
        {
            get => source.outputAudioMixerGroup;
            set => source.outputAudioMixerGroup = value;
        }
        
        public AudioRolloffMode RollOffMode
        {
            get => source.rolloffMode;
            set => source.rolloffMode = value;
        }
        
        public bool BypassEffects
        {
            get => source.bypassEffects;
            set => source.bypassEffects = value;
        }
        
        public bool BypassListenerEffects
        {
            get => source.bypassListenerEffects;
            set => source.bypassListenerEffects = value;
        }
        
        public bool BypassReverbZones
        {
            get => source.bypassReverbZones;
            set => source.bypassReverbZones = value;
        }
        
        public bool IgnoreListenerPause
        {
            get => source.ignoreListenerPause;
            set => source.ignoreListenerPause = value;
        }
        
        public bool IgnoreListenerVolume
        {
            get => source.ignoreListenerVolume;
            set => source.ignoreListenerVolume = value;
        }
        
        public bool PlayOnAwake
        {
            get => source.playOnAwake;
            set => source.playOnAwake = value;
        }
        
        public AudioVelocityUpdateMode VelocityUpdateMode
        {
            get => source.velocityUpdateMode;
            set => source.velocityUpdateMode = value;
        }
        
        public float Time
        {
            get => source.time;
            set => source.time = value;
        }
        
        public int TimeSamples
        {
            get => source.timeSamples;
            set => source.timeSamples = value;
        }
        
        public int Priority
        {
            get => source.priority;
            set => source.priority = value;
        }
        
        public bool Spatialize
        {
            get => source.spatialize;
            set => source.spatialize = value;
        }
        
        public bool SpatializePostEffects
        {
            get => source.spatializePostEffects;
            set => source.spatializePostEffects = value;
        }
        
        public bool IsVirtual => source.isVirtual;

        [Button]
        private async void DebugPlay()
        {
            float previousPitch = source.pitch;
            if (randomizePitch)
                source.pitch = defaultPitch + Random.Range(-pitchRadius, pitchRadius);
            
            source.Play();
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(source.clip.length));
            source.pitch = previousPitch;
        }
        
        public void Play()
        {
            if (randomizePitch)
                source.pitch = defaultPitch + Random.Range(-pitchRadius, pitchRadius);
            
            source.Play();
        }

        public void PlayParallel()
        {
            if (source.isPlaying)
            {
                source.PlayOneShot(source.clip);
                return;
            }
            
            Play();
        }
        
        public void PlayOneShot(AudioClip clip)
        {
            if (randomizePitch)
                source.pitch = defaultPitch + Random.Range(-pitchRadius, pitchRadius);
            
            source.PlayOneShot(clip);
        }
        
        /// <summary> Remember to add AudioSettings.dspTime </summary>
        public void PlayScheduled(double absoluteTime)
        {
            if (randomizePitch)
                source.pitch = defaultPitch + Random.Range(-pitchRadius, pitchRadius);
            
            source.PlayScheduled(absoluteTime);
        }
        
        public void Stop()
        {
            source.Stop();
        }
        
        public void Pause()
        {
            source.Pause();
        }
        
        public void UnPause()
        {
            source.UnPause();
        }
        
        public void SetScheduledEndTime(double absoluteTime)
        {
            source.SetScheduledEndTime(absoluteTime);
        }
        
        public void SetScheduledStartTime(double absoluteTime)
        {
            source.SetScheduledStartTime(absoluteTime);
        }

        private void OnValidate()
        {
            if (source != null)
                defaultPitch = source.pitch;
        }

        private void Reset()
        {
            source = GetComponent<AudioSource>();
        }
    }
}