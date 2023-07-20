using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Editor
{
    [CustomPropertyDrawer(typeof(PositionSetup))]
    public class PositionSetupPropertyDrawer : PropertyDrawer
    {
        private const float MaxWidthPerBox = 20f;
        public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label)
        {
            // draw 4 boolean boxes in a single line that's the same as the label
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            float width = position.width / 4f;
            float boxSpacing = Mathf.Min(width, MaxWidthPerBox);
            float height = position.height;
            float x = position.x;
            float y = position.y;
            
            Rect rect = new Rect(x, y, width, height);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("one"), GUIContent.none);
            rect.x += boxSpacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("two"), GUIContent.none);
            rect.x += boxSpacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("three"), GUIContent.none);
            rect.x += boxSpacing;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("four"), GUIContent.none);
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight; 
        }
    }
}