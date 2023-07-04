using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ImageAlphaThresholdChanger : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private float alphaThreshold = 1f;
        
        private void Awake()
        {
            Sprite sprite = image.sprite;
            if (sprite == null)
                return;

            try
            {
                sprite.texture.GetPixel(x: 0, y: 0);
            }
            catch (Exception e)
            {
                Debug.Log(message: $"Read/Write not enabled on {name}, sprite: {sprite.name}", context: this);
                Debug.LogWarning(e);
                return;
            }

            image.alphaHitTestMinimumThreshold = alphaThreshold;
        }
        
        [ContextMenu("Set Alpha Threshold")]
        private void SetThreshold()
        {
            image.alphaHitTestMinimumThreshold = alphaThreshold;
        }
        
        private void Reset()
        {
            image = GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("ImageAlphaRaycastSetter: Image not found", this);
                return;
            }
            
            if (image.sprite == null)
            {
                Debug.LogError("ImageAlphaRaycastSetter: Image sprite not found", this);
            }
            else  if (!image.sprite.texture.isReadable)
                Debug.LogError("ImageAlphaRaycastSetter: Image sprite is not readable", this);
        }

        private void OnValidate()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
                if (image == null)
                {
                    Debug.Log(message: $"Image is null on alphaRaycaster: {name}", context: this);
                    return;
                }
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(target: this);
#endif
            }
            
            if (image.sprite == null)
            {
                Debug.LogError("ImageAlphaRaycastSetter: Image sprite not found", this);
            }
            //else  if (!image.sprite.texture.isReadable)
                //Debug.LogError("ImageAlphaRaycastSetter: Image sprite is not readable", this);
        }
    }
}