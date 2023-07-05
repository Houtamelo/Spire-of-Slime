using System;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Utils.Patterns;
using Object = UnityEngine.Object;

namespace Core.ResourceManagement
{
    public class ResourceHandle<T> : IDisposable where T : Object
    {
        private T _resource;
        public Result<T> Resource
        {
            get
            {
                if (_isLoaded)
                    return _resource;

                return _isDisposed ? Result<T>.Error($"ResourceHandle is disposed. Path: {Path}") :
                    Result<T>.Error($"Loading operation not yet finished. Path: {Path}");
            }
        }

        private bool _isLoaded;
        private ResourceRequest _request;
        public readonly string Path;
        public bool HasResult => _isLoaded && !_isDisposed;

        private ResourceHandle(T resource, string path)
        {
            _resource = resource;
            _isLoaded = true;
            Path = path;
        }
        
        private ResourceHandle(ResourceRequest request, string path)
        {
            _request = request;
            Path = path;
            request.completed += OnRequestCompleted;
        }

        private void OnRequestCompleted(AsyncOperation request)
        {
            _resource = (T) _request.asset;
            if (_resource == null)
            {
                Debug.LogWarning($"Resource not found. Path: {Path}");
                Dispose();
            }
            else
                _isLoaded = true;
        }
        
        [MustUseReturnValue]
        public static ResourceHandle<T> Load(string path)
        {
            T resource = Resources.Load<T>(path);
            if (resource == null)
            {
                Debug.LogWarning($"Resource not found: {path}");
                return null;
            }

            return new ResourceHandle<T>(resource, path);
        }
        
        [MustUseReturnValue]
        public static ResourceHandle<T> LoadAsync(string path)
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            return new ResourceHandle<T>(request, path);
        }

        #region Disposing

        private bool _isDisposed;
        
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_isLoaded)
                Resources.UnloadAsset(_resource);

            _isLoaded = false;
            _resource = null;
            _request = null;
        }
        
        #endregion
    }
}