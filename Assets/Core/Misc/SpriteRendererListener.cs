using UnityEngine;

namespace Core.Misc
{
    [ExecuteInEditMode]
    public class SpriteRendererListener : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer reference, self;
        
        private bool _registered;
        private bool _setFirst;

        private void Awake()
        {
            if (reference == null || self == null)
                return;
            
            Register();
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (_setFirst == false)
            {
                OnReferenceSpriteChanged(reference);
                _setFirst = true;
            }

            if (_registered)
                return;
            
            if (reference != null && self != null)
                Register();
        }
#endif

        private void OnDestroy()
        {
            if (reference == null || !_registered)
                return;
            
            reference.UnregisterSpriteChangeCallback(OnReferenceSpriteChanged);
        }

        private void Register()
        {
            try
            {
                reference.RegisterSpriteChangeCallback(OnReferenceSpriteChanged);
                _registered = true;
            }
            catch
            {
                _registered = false;
            }
        }

        private void OnReferenceSpriteChanged(SpriteRenderer spriteRenderer)
        {
#if UNITY_EDITOR
            if (spriteRenderer == null)
                return;
#endif
            
            self.sprite = spriteRenderer.sprite;
            self.flipX = spriteRenderer.flipX;
            self.flipY = spriteRenderer.flipY;
        }

        private void OnValidate()
        {
            if (reference == null || self == null || _registered)
                return;

            Register();
        }
        
        [ContextMenu("Force Update")]
        private void ForceUpdate()
        {
            OnReferenceSpriteChanged(reference);
        }
    }
}