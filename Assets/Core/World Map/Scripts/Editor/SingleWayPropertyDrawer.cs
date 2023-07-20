using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using UnityEditor;
using UnityEngine;

namespace Core.World_Map.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(OneWay))]
    public class SingleWayPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label)
        {
            // Bothways is a struct that has two fields of the enum type "LocationEnum", one field is called "Origin" and the other "Destination", both fields should be drawn in the same line as the label and should have different values
            
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            float width = position.width / 2;
            Rect rect1 = new Rect(position.x, position.y, width, position.height);
            Rect rect2 = new Rect(position.x + width, position.y, width, position.height);
            SerializedProperty origin = property.FindPropertyRelative("Origin");
            SerializedProperty destination = property.FindPropertyRelative("Destination");

            if (origin.enumValueIndex == destination.enumValueIndex) // if both are the same then find the next possible enum value and assign it to the destination
            {
                LocationEnum[] possibleEnums = Enum<LocationEnum>.GetValues();
                for (int i = 0; i < possibleEnums.Length; i++)
                {
                    LocationEnum possibleEnum = possibleEnums[i];
                    if (possibleEnum != (LocationEnum)origin.enumValueIndex)
                    {
                        destination.enumValueIndex = (int)possibleEnum;
                        break;
                    }
                }
            }
            
            EditorGUI.PropertyField(rect1, origin, GUIContent.none);
            EditorGUI.PropertyField(rect2, destination, GUIContent.none);
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}