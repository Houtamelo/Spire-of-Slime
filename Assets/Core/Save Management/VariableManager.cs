using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Save_Management
{
    public class VariableManager : VariableStorageBehaviour
    {
        public override void Clear()
        {
            {Debug.LogError("Unsupported"); }
        }

        public override bool Contains(string variableName)
        {
            Debug.LogError("Unsupported");
            return true;
        }

        public override void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
        {
            Debug.LogError("Unsupported");
        }

        public override (Dictionary<string,float>,Dictionary<string,string>,Dictionary<string,bool>) GetAllVariables()
        {
            Debug.LogError("Unsupported");
            return (new Dictionary<string, float>(), new Dictionary<string, string>(), new Dictionary<string, bool>());
        }

        public override bool TryGetValue<T>(string variableName, out T result)
        {
            if (Save.AssertInstance(out Save save) == false)
            {
                result = default;
                return false;
            }

            T variable = save.GetVariable<T>(variableName);
            result = variable;
            return true;
        }
        
        public override void SetValue(string variableName, bool boolValue) => Save.Current?.SetVariable(variableName,     boolValue);
        public override void SetValue(string variableName, string stringValue) => Save.Current?.SetVariable(variableName, stringValue);
        public override void SetValue(string variableName, float floatValue) => Save.Current?.SetVariable(variableName,   floatValue);
    }
}