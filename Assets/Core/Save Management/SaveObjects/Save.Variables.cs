using System;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Core.Save_Management.SaveObjects
{
    public partial class Save
    {
        public void SetVariable(CleanString variableName, bool boolValue)
        {
            if (TryGetBool(variableName, out bool oldValue) && oldValue == boolValue)
                return;
            
            SetDirty();
            if (VariablesName.BoolSetters.TryGetValue(variableName, out VariablesName.BoolSetter setter))
                setter.Invoke(save: this, boolValue, variableName);
            else
                Booleans[variableName] = boolValue;
            
            BoolChanged?.Invoke(variableName, oldValue, boolValue);
        }

        public void SetVariable(CleanString variableName, CleanString stringValue)
        {
            if (TryGetString(variableName, out CleanString oldValue) == false)
                oldValue = string.Empty;

            if (oldValue == stringValue)
                return;

            SetDirty();
            if (VariablesName.StringSetters.TryGetValue(variableName, out VariablesName.StringSetter setter))
                setter.Invoke(save: this, stringValue, variableName);
            else
                Strings[variableName] = stringValue;

            StringChanged?.Invoke(variableName, oldValue, stringValue);
        }

        public void SetVariable(CleanString variableName, int intValue)
        {
            if (TryGetInt(variableName, out int oldValue) && oldValue == intValue)
                return;
            
            SetDirty();
            if (VariablesName.IntSetters.TryGetValue(variableName, out VariablesName.IntSetter setter))
                setter.Invoke(save: this, intValue, variableName);
            else
                Ints[variableName] = intValue;

            IntChanged?.Invoke(variableName, oldValue, intValue);
        }
        
        [CanBeNull]
        public T GetVariable<T>(CleanString variableName)
        {
            if (TryGetString(variableName, out CleanString stringValue))
            {
                if (stringValue is T castedClean)
                    return castedClean;

                if (stringValue.ToString() is T castedString)
                    return castedString;

                Debug.LogWarning($"Variable:{variableName.ToString()} exists as string but is requested as {typeof(T).Name}");
                return default;
            }
            
            if (VariableDatabase.TryGetDefault(variableName, out stringValue))
            {
                SetVariable(variableName, stringValue);
                if (TryGetString(variableName, out stringValue))
                {
                    if (stringValue is T castedClean)
                        return castedClean;
                    
                    if (stringValue.ToString() is T castedString)
                        return castedString;
                    
                    Debug.LogWarning($"Variable:{variableName.ToString()} exists as string but is requested as {typeof(T).Name}");
                    return default;
                }

                {
                    Debug.LogWarning($"Variable:{variableName.ToString()}, has been set with default value:{stringValue} but wasn't found with {nameof(TryGetString)}");
                    if (stringValue is T castedClean)
                        return castedClean;
                    
                    if (stringValue.ToString() is T castedString)
                        return castedString;

                    Debug.LogWarning($"Variable:{variableName.ToString()} has default value as string but is requested as {typeof(T).Name}");
                    return default;
                }
            }
            
            if (TryGetBool(variableName, out bool boolValue))
            {
                if (boolValue is T castedBool)
                    return castedBool;

                Debug.LogWarning($"Variable:{variableName.ToString()} exists as bool but is requested as {typeof(T).Name}");
                return default;
            }
            
            if (VariableDatabase.TryGetDefault(variableName, out boolValue))
            {
                SetVariable(variableName, boolValue);
                if (TryGetBool(variableName, out boolValue))
                {
                    if (boolValue is T castedBool)
                        return castedBool;
                    
                    Debug.LogWarning($"Variable:{variableName.ToString()} exists as bool but is requested as {typeof(T).Name}");
                    return default;
                }

                {
                    Debug.LogWarning($"Variable:{variableName.ToString()}, has been set with default value:{boolValue} but wasn't found with {nameof(TryGetBool)}");
                    if (boolValue is T castedBool)
                        return castedBool;

                    Debug.LogWarning($"Variable:{variableName.ToString()} has default value as bool but is requested as {typeof(T).Name}");
                    return default;
                }
            }
            
            if (TryGetInt(variableName, out int intValue))
            {
                if (intValue is T castedInt)
                    return castedInt;

                Debug.LogWarning($"Variable:{variableName.ToString()} exists as int but is requested as {typeof(T).Name}");
                return default;
            }
            
            if (VariableDatabase.TryGetDefault(variableName, out intValue))
            {
                SetVariable(variableName, intValue);
                if (TryGetInt(variableName, out intValue))
                {
                    if (intValue is T castedInt)
                        return castedInt;
                    
                    Debug.LogWarning($"Variable:{variableName.ToString()} exists as int but is requested as {typeof(T).Name}");
                    return default;
                }

                {
                    Debug.LogWarning($"Variable:{variableName.ToString()}, has been set with default value:{intValue} but wasn't found with {nameof(TryGetInt)}");
                    if (intValue is T castedFloat)
                        return castedFloat;

                    Debug.LogWarning($"Variable:{variableName.ToString()} has default value as int but is requested as {typeof(T).Name}");
                    return default;
                }
            }
            
            if (0 is not T && false is not T && string.Empty is not T && new CleanString(string.Empty) is not T)
                Debug.LogWarning($"T must be string, bool, int, requested: {typeof(T).Name}, variable name: {variableName.ToString()}.");

            return default;
        }

        private bool TryGetString(CleanString variableName, out CleanString stringValue)
        {
            if (VariablesName.StringGetters.TryGetValue(variableName, out VariablesName.StringGetter stringGetter))
            {
                stringValue = stringGetter.Invoke(this, variableName);
                return true;
            }

            return Strings.TryGetValue(variableName, out stringValue);
        }
        
        public bool TryGetInt(CleanString variableName, out int value)
        {
            if (VariablesName.IntGetters.TryGetValue(variableName, out VariablesName.IntGetter intGetter))
            {
                value = intGetter.Invoke(save: this, variableName);
                return true;
            }
            return Ints.TryGetValue(variableName, out value);
        }

        public bool TryGetBool(CleanString variableName, out bool value)
        {
            if (VariablesName.BoolGetters.TryGetValue(variableName, out VariablesName.BoolGetter boolGetter))
            {
                value = boolGetter.Invoke(save: this, variableName);
                return true;
            }
            
            return Booleans.TryGetValue(variableName, out value);
        }
    }
}