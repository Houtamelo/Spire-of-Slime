using Core.Misc;
using UnityEditor;
using UnityEngine;

namespace Core.Utils.Editor
{
    [CustomPropertyDrawer(typeof(SerializableTuple<,>))]
    public class SerializableTuplePropertyDrawer : PropertyDrawer
    {
        private const float Spacing = 10f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw both properties on the same line as the label, the properties are called "item1" and "item2"
            
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            float width = (position.width - Spacing) / 2f;
            
            Rect rect1 = new Rect(position.x, position.y, width, position.height);
            Rect rect2 = new Rect(position.x + width + Spacing, position.y, width, position.height);
            Rect spacingRect = new Rect(position.x + width, position.y, Spacing, position.height);
            
            EditorGUI.LabelField(spacingRect, " |");
            EditorGUI.PropertyField(rect1, property.FindPropertyRelative("item1"), GUIContent.none);
            EditorGUI.PropertyField(rect2, property.FindPropertyRelative("item2"), GUIContent.none);
            
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, GUIContent.none);
            
            SerializedProperty item1 = property.FindPropertyRelative("item1");
            SerializedProperty item2 = property.FindPropertyRelative("item2");

            return (item1 == null, item2 == null) switch
            {
                (true, true) => EditorGUI.GetPropertyHeight(SerializedPropertyType.Generic, GUIContent.none),
                (true, false) => EditorGUI.GetPropertyHeight(item2, GUIContent.none),
                (false, true) => EditorGUI.GetPropertyHeight(item1, GUIContent.none),
                (false, false) => Mathf.Max(EditorGUI.GetPropertyHeight(item1, GUIContent.none), EditorGUI.GetPropertyHeight(item2, GUIContent.none))
            };
        }
    }
}