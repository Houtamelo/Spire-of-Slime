using System;
using Core.Main_Database.Visual_Novel.Enums;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Core.Main_Database.Visual_Novel
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
        public bool BoolValue => boolValue == BoolEnum.True;

        [SerializeField]
        private int intValue;
        public float IntValue => intValue;

        [SerializeField]
        private string stringValue;
        public string StringValue => stringValue;

        public bool Validate([NotNull] Save save)
        {
            switch (reference.Type)
            {
                case VariableType.Bool:
                {
                    bool value = save.GetVariable<bool>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal    => value == BoolValue,
                        ComparisonType.NotEqual => value != BoolValue,
                        _                       => throw new ArgumentOutOfRangeException(nameof(comparisonType), message: $"comparisonType: {comparisonType} is not valid for bool")
                    };
                }
                case VariableType.Int:
                {
                    int value = save.GetVariable<int>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal          => value == IntValue,
                        ComparisonType.NotEqual       => value != IntValue,
                        ComparisonType.Greater        => value > IntValue,
                        ComparisonType.GreaterOrEqual => value >= IntValue,
                        ComparisonType.Less           => value < IntValue,
                        ComparisonType.LessOrEqual    => value <= IntValue,
                        _                             => throw new ArgumentOutOfRangeException(nameof(comparisonType), message: $"comparisonType: {comparisonType} is not valid for int")
                    };
                }
                case VariableType.String:
                {
                    string value = save.GetVariable<string>(reference.Key);
                    return comparisonType switch
                    {
                        ComparisonType.Equal    => value == StringValue,
                        ComparisonType.NotEqual => value != StringValue,
                        _                       => throw new ArgumentOutOfRangeException(nameof(comparisonType), message: $"comparisonType: {comparisonType} is not valid for string")
                    };
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(reference.Type),$"reference.Type: {reference.Type} is not valid");
            }
        }
    }
}