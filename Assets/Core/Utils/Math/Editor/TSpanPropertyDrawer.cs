using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using static Core.Utils.Math.TSpan;

namespace Core.Utils.Math.Editor
{
    [CustomPropertyDrawer(typeof(TSpan))]
    public class TSpanPropertyDrawer : PropertyDrawer
    {
        private const float Spacing = 10f;
        
        public override void OnGUI(Rect position, [NotNull] SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            float width = (position.width - Spacing) / 2f;
            
            Rect modeRect = new Rect(position.x, position.y, width, position.height);
            Rect valueRect = new Rect(position.x + width + Spacing, position.y, width, position.height);

            SerializedProperty timeModeProperty = property.FindPropertyRelative("timeMode");
            timeModeProperty.enumValueIndex = (int)(TimeMode)EditorGUI.EnumPopup(modeRect, (TimeMode)timeModeProperty.enumValueIndex);
            TimeMode timeMode = (TimeMode) timeModeProperty.enumValueIndex;
            
            SerializedProperty ticksProperty = property.FindPropertyRelative("ticks");
            long ticks = ticksProperty.longValue;
            
            ticksProperty.longValue = timeMode switch // always save in ticks, but show in the selected mode
            {
                TimeMode.Ticks        => EditorGUI.LongField(valueRect, ticks),
                TimeMode.Milliseconds => MillisecondsToTicks(EditorGUI.LongField(valueRect, TicksToMilliseconds(ticks))),
                TimeMode.Seconds      => SecondsToTicks(EditorGUI.DoubleField(valueRect, TicksToSeconds(ticks))),
                _                     => ticksProperty.longValue
            };

            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
    }
}