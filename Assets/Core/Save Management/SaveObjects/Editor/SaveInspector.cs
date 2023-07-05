using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Core.Save_Management.SaveObjects.Editor
{
    public class SaveInspector : SerializedScriptableObject
    {
        [ShowInInspector, OdinSerialize]
        public Save Save;

        [Button]
        private void BindToCurrent() => Save = Save.Current;
    }
}