using System;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Save_Management
{
    public partial class Save
    {
        public void SetVariable(CleanString variableName, bool boolValue)
        {
            if (TryGetBool(variableName, out bool oldValue))
            {
                if (oldValue == boolValue)
                    return;
            }
            else
            {
                oldValue = false;
            }
            
            SetDirty();
            if (VariablesName.BoolSetters.TryGetValue(variableName, out VariablesName.BoolSetter setter))
                setter.Invoke(save: this, boolValue, variableName);
            else
                Booleans[variableName] = boolValue;
            
            BoolChanged?.Invoke(variableName, oldValue, boolValue);
        }

        public void SetVariable(CleanString variableName, CleanString stringValue)
        {
            if (!TryGetString(variableName, out CleanString oldValue))
                oldValue = string.Empty;

            if (oldValue == stringValue)
                return;

            SetDirty();
            if (VariablesName.StringSetters.TryGetValue(variableName, out VariablesName.StringSetter setter))
                setter.Invoke(this, stringValue, variableName);
            else
                Strings[variableName] = stringValue;

            StringChanged?.Invoke(variableName, oldValue, stringValue);
        }

        public void SetVariable(CleanString variableName, float floatValue)
        {
            if (TryGetFloat(variableName, out float oldValue))
            {
                if (Math.Abs(oldValue - floatValue) < 0.00001f)
                    return;
            }
            else
            {
                oldValue = 0f;
            }

            SetDirty();
            if (VariablesName.FloatSetters.TryGetValue(variableName, out VariablesName.FloatSetter setter))
                setter.Invoke(save: this, floatValue, variableName);
            else
                Floats[variableName] = floatValue;

            FloatChanged?.Invoke(variableName, oldValue, floatValue);
        }
        
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
            
            if (TryGetFloat(variableName, out float floatValue))
            {
                if (floatValue is T castedFloat)
                    return castedFloat;

                Debug.LogWarning($"Variable:{variableName.ToString()} exists as float but is requested as {typeof(T).Name}");
                return default;
            }
            
            if (VariableDatabase.TryGetDefault(variableName, out floatValue))
            {
                SetVariable(variableName, floatValue);
                if (TryGetFloat(variableName, out floatValue))
                {
                    if (floatValue is T castedFloat)
                        return castedFloat;
                    
                    Debug.LogWarning($"Variable:{variableName.ToString()} exists as float but is requested as {typeof(T).Name}");
                    return default;
                }

                {
                    Debug.LogWarning($"Variable:{variableName.ToString()}, has been set with default value:{floatValue} but wasn't found with {nameof(TryGetFloat)}");
                    if (floatValue is T castedFloat)
                        return castedFloat;

                    Debug.LogWarning($"Variable:{variableName.ToString()} has default value as float but is requested as {typeof(T).Name}");
                    return default;
                }
            }
            
            if (0f is not T && false is not T && string.Empty is not T && new CleanString(string.Empty) is not T)
            {
                Debug.LogWarning($"T must be string, bool or float, requested: {typeof(T).Name}, variable name: {variableName.ToString()}.");
            }
            
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
        
        public bool TryGetFloat(CleanString variableName, out float value)
        {
            if (VariablesName.FloatGetters.TryGetValue(variableName, out VariablesName.FloatGetter floatGetter))
            {
                value = floatGetter.Invoke(save: this, variableName);
                return true;
            }
            return Floats.TryGetValue(variableName, out value);
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