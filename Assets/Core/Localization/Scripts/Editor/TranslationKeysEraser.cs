using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Localization.Scripts.Editor
{
    public class TranslationKeysEraser : ScriptableObject
    {
        [SerializeField]
        private CleanString keyToClean;
        
        [SerializeField]
        private TranslationType translationType;

        [Button]
        private void Clean()
        {
            if (keyToClean.IsSome())
                TranslationUtils.CleanKey(keyToClean, translationType);
        }
    }
}