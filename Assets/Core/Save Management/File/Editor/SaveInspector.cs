using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Save_Management.Editor
{
    public class SaveInspector : SerializedScriptableObject
    {
        [ShowInInspector, OdinSerialize]
        public Save Save;

        [Button]
        private void BindToCurrent() => Save = Save.Current;
    }
}