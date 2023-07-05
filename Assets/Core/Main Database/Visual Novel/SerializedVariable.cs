using Core.Main_Database.Visual_Novel.Enums;
using Core.Save_Management.SaveObjects;
using UnityEngine;

namespace Core.Main_Database.Visual_Novel
{
    [CreateAssetMenu(menuName = "Yarn Spinner/Variable")]
    public class SerializedVariable : ScriptableObject
    {
        [SerializeField] 
        private VariableType type;
        
        public CleanString Key => name;
        public VariableType Type => type;
    }
}