using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEditor;
using UnityEngine;

namespace Core.World_Map.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(BothWays))]
    public class BothWaysPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label)
        {
            // Bothways is a struct that has two fields of the enum type "LocationEnum", one field is called "One" and the other "Two", both fields should be drawn in the same line as the label and should have different values
            
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            float width = position.width / 2;
            Rect rect1 = new Rect(position.x, position.y, width, position.height);
            Rect rect2 = new Rect(position.x + width, position.y, width, position.height);
            SerializedProperty oneProperty = property.FindPropertyRelative("One");
            SerializedProperty twoProperty = property.FindPropertyRelative("Two");

            if (oneProperty.enumValueIndex == twoProperty.enumValueIndex)
            {
                LocationEnum[] possibleEnums = Enum<LocationEnum>.GetValues();

                for (int i = 0; i < possibleEnums.Length; i++)
                {
                    LocationEnum enumValue = possibleEnums[i];
                    if (enumValue != (LocationEnum)twoProperty.enumValueIndex)
                    {
                        twoProperty.enumValueIndex = (int)enumValue;
                        break;
                    }
                }
            }
            
            EditorGUI.PropertyField(rect1, oneProperty, GUIContent.none);
            EditorGUI.PropertyField(rect2, twoProperty, GUIContent.none);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}