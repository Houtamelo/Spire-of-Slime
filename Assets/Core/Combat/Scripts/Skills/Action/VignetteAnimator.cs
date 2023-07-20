using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Core.Combat.Scripts.Skills.Action
{
    [ExecuteInEditMode]
    public class VignetteAnimator : MonoBehaviour
    {
        [SerializeField] private VolumeProfile profile;
        
        [SerializeField] private Vignette vignette;

        [SerializeField] private float intensity;

        private void Update()
        {
            vignette.intensity.value = intensity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (vignette == null)
            {
                profile.TryGet(component: out vignette);
                if (vignette == null)
                    Debug.Log("Vignette not found");
                else
                    UnityEditor.EditorUtility.SetDirty(target: this);
            }
        }
        #endif
    }
}