using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utils.Async;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Pause_Menu.Scripts
{
    public sealed class ResolutionManager : Singleton<ResolutionManager>
    {
        private Resolution _memorizedResolution;
        public event Action<Resolution> ResolutionChanged;

        private FullScreenMode _memorizedScreenMode;
        public event Action<FullScreenMode> ScreenModeChanged;

        private readonly Queue<CoroutineWrapper> _screenChangeQueue = new();
        private CoroutineWrapper _currentRoutine;

        private void Update()
        {
            Resolution currentResolution;
#if UNITY_EDITOR
            Vector2 size = UnityEditor.Handles.GetMainGameViewSize();
            currentResolution = new Resolution() { width = Mathf.FloorToInt(size.x), height = Mathf.FloorToInt(size.y), refreshRate = Screen.currentResolution.refreshRate };
#else
            currentResolution = Screen.currentResolution;
#endif
            if (CompareResolutions(right: currentResolution, left: _memorizedResolution) == false)
            {
                _memorizedResolution = currentResolution;
                ResolutionChanged?.Invoke(currentResolution);
            }

            FullScreenMode currentScreenMode = UnityEngine.Device.Screen.fullScreenMode;
            if (currentScreenMode != _memorizedScreenMode)
            {
                _memorizedScreenMode = currentScreenMode;
                ScreenModeChanged?.Invoke(currentScreenMode);
            }
            
            if (_screenChangeQueue.Count == 0 || _currentRoutine is {IsFinished: false})
                return;
            
            _currentRoutine = _screenChangeQueue.Dequeue();
            _currentRoutine.Start();
        }

        public void ChangeResolution(Resolution resolution)
        {
            _screenChangeQueue.Enqueue(item: new CoroutineWrapper(ResolutionChangeRoutine(resolution: resolution), routineName: nameof(ResolutionChangeRoutine), autoStart: false));
        }

        private IEnumerator ResolutionChangeRoutine(Resolution resolution)
        {
            UnityEngine.Device.Screen.SetResolution(width: resolution.width, height: resolution.height, fullscreenMode: UnityEngine.Device.Screen.fullScreenMode, preferredRefreshRate: resolution.refreshRate);
            yield return null; yield return null; yield return null;
        }

        public void ChangeScreenMode(FullScreenMode mode)
        {
            _screenChangeQueue.Enqueue(item: new CoroutineWrapper(ScreenModeChangeRoutine(mode: mode), routineName: nameof(ScreenModeChangeRoutine), autoStart: false));
        }
        
        private IEnumerator ScreenModeChangeRoutine(FullScreenMode mode)
        {
            UnityEngine.Device.Screen.fullScreenMode = mode;
            yield return null; yield return null; yield return null;
        }

        public static bool CompareResolutions(Resolution right, Resolution left) => right.width == left.width && right.height == left.height && right.refreshRate == left.refreshRate;
    }
}