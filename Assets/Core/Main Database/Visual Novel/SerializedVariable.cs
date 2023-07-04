using Main_Database.Visual_Novel.Enums;
using Save_Management;
using UnityEngine;

namespace Main_Database.Visual_Novel
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