using System;
using Core.Main_Database.Visual_Novel.Enums;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Core.Main_Database.Visual_Novel.Editor
{
    [CustomPropertyDrawer(typeof(VariableRequirement))]
    public class SerializedVariablePropertyDrawer : PropertyDrawer
    {
        private const float ComparisonTypeWidth = 130f;
        private const float ValueWidth = 100f;
        private const float Spacing = 5f;
        
        public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            
            SerializedProperty serializedVariableProperty = property.FindPropertyRelative("reference");
            SerializedVariable serializedVariable = (SerializedVariable)serializedVariableProperty.objectReferenceValue;
            if (serializedVariable == null)
            {
                EditorGUI.PropertyField(position, serializedVariableProperty);
                EditorGUI.EndProperty();
                return;
            }
            
            VariableType type = serializedVariable.Type;
            SerializedProperty comparisonTypeProperty = property.FindPropertyRelative("comparisonType");
            ComparisonType comparisonType = (ComparisonType) comparisonTypeProperty.enumValueIndex;
            
            if (type != VariableType.Int && comparisonType != ComparisonType.Equal && comparisonType != ComparisonType.NotEqual)
                comparisonTypeProperty.enumValueIndex = (int) ComparisonType.Equal;

            float referenceWidth = position.width - ComparisonTypeWidth - ValueWidth - (Spacing * 2);
            Rect referenceRect = new Rect(position.x, position.y, referenceWidth, position.height);
            Rect comparisonTypeRect = new Rect(referenceRect.xMax + Spacing, position.y, ComparisonTypeWidth, position.height);
            Rect valueRect = new Rect(comparisonTypeRect.xMax + Spacing, position.y, ValueWidth, position.height);
            
            EditorGUI.PropertyField(referenceRect, serializedVariableProperty, GUIContent.none);
            EditorGUI.PropertyField(comparisonTypeRect, comparisonTypeProperty, GUIContent.none);
            
            switch (type)
            {
                case VariableType.Int:
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("intValue"), GUIContent.none);
                    break;
                case VariableType.Bool:
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("boolValue"), GUIContent.none);
                    break;
                case VariableType.String:
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("stringValue"), GUIContent.none);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, message: null);
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }
}