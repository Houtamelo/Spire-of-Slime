using System.Collections.Generic;
using System.Linq;
using Core.Save_Management.SaveObjects;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn;
using Yarn.Unity;

namespace Core.Main_Database.Visual_Novel
{
    public class VariableDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private SerializedVariable[] allVariables;

        public bool VariableAssetExists(CleanString variableName)
        {
            foreach (SerializedVariable variable in allVariables)
            {
                if (variable.Key == variableName)
                    return true;
            }

            return false;
        }

        [SerializeField, Required]
        private YarnProject yarnProject;
        
        private Dictionary<CleanString, CleanString> _defaultStrings;
        public static IReadOnlyDictionary<CleanString, CleanString> DefaultStrings => Instance.VariableDatabase._defaultStrings;

        private Dictionary<CleanString, int> _defaultInts;
        public static IReadOnlyDictionary<CleanString, int> DefaultInts => Instance.VariableDatabase._defaultInts;
        
        private Dictionary<CleanString, bool> _defaultBools;
        public static IReadOnlyDictionary<CleanString, bool> DefaultBools => Instance.VariableDatabase._defaultBools;

        public void Initialize()
        {
            _defaultStrings = new Dictionary<CleanString, CleanString>();
            _defaultInts = new Dictionary<CleanString, int>();
            _defaultBools = new Dictionary<CleanString, bool>();

            foreach ((string key, Operand value) in yarnProject.Program.InitialValues)
            {
                switch (value.ValueCase)
                {
                    case Operand.ValueOneofCase.StringValue: _defaultStrings[key] = value.StringValue;      break;
                    case Operand.ValueOneofCase.BoolValue:   _defaultBools[key]   = value.BoolValue;        break;
                    case Operand.ValueOneofCase.FloatValue:  _defaultInts[key]    = (int) value.FloatValue; break;
                }
            }
        }
        
        public bool TryGetDefault(CleanString variableName, out CleanString result) => _defaultStrings.TryGetValue(variableName, out result);
        public bool TryGetDefault(CleanString variableName, out int result)         => _defaultInts.TryGetValue(variableName,    out result);
        public bool TryGetDefault(CleanString variableName, out bool result)        => _defaultBools.TryGetValue(variableName,   out result);

#if UNITY_EDITOR        
        public void AssignData([NotNull] IEnumerable<SerializedVariable> variables)
        {
            allVariables = variables.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}