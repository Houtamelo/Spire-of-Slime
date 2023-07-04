using System;
using Main_Database.Visual_Novel.Enums;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Save_Management.Save;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Main_Database.Visual_Novel
{
    [Serializable]
    public struct VariableRequirement
    {
        [SerializeField, Required]
        private SerializedVariable reference;

        [SerializeField]
        private ComparisonType comparisonType;

        [SerializeField]
        private BoolEnum boolValue;

        [SerializeField]
        private float floatValue;

        [SerializeField]
        private string stringValue;
        
        public float FloatValue => floatValue;
        public string StringValue => stringValue;
        public bool BoolValue => boolValue == BoolEnum.True;
        
        public bool Validate(Save save)
        {
            switch (reference.Type)
            {
                case VariableType.Bool:
                {
                    bool value = save.GetVariable<bool>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal => value == BoolValue,
                        ComparisonType.NotEqual => value != BoolValue,
                        _ => throw new ArgumentOutOfRangeException($"comparisonType: {comparisonType} is not valid for bool")
                    };
                }
                case VariableType.Float:
                {
                    float value = save.GetVariable<float>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal => value == FloatValue,
                        ComparisonType.NotEqual => value != FloatValue,
                        ComparisonType.Greater => value > FloatValue,
                        ComparisonType.GreaterOrEqual => value >= FloatValue,
                        ComparisonType.Less => value < FloatValue,
                        ComparisonType.LessOrEqual => value <= FloatValue,
                        _ => throw new ArgumentOutOfRangeException($"comparisonType: {comparisonType} is not valid for float")
                    };
                }
                case VariableType.String:
                {
                    string value = save.GetVariable<string>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal => value == StringValue,
                        ComparisonType.NotEqual => value != StringValue,
                        _ => throw new ArgumentOutOfRangeException($"comparisonType: {comparisonType} is not valid for string")
                    };
                }
                default:
                    throw new ArgumentOutOfRangeException($"reference.Type: {reference.Type} is not valid");
            }
        }
    }
}