using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Times;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    [CustomPropertyDrawer(typeof(ARTimeSpan))]
    public class ARTimeSpanDrawer : PropertyDrawer {
        static readonly string[] DaysRange = Enumerable.Range(0, 31).Select(v => v.ToString()).ToArray();
        static readonly string[] HoursRange = Enumerable.Range(0, 24).Select(v => v.ToString()).ToArray();
        static readonly string[] MinutesRange = Enumerable.Range(0, 60).Select(v => v.ToString()).ToArray();
        static readonly GUIContent DaysContent = new("D:");
        static readonly GUIContent HoursContent = new("H:");
        static readonly GUIContent MinutesContent = new("M:");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            PropertyDrawerRects rect = position;

            bool showDays = true;
            bool showHours = true;
            bool showMinutes = true;
            if (property.ExtractAttribute<ARTimeSpanAttribute>() is {} timeSpanAttribute) {
                showDays = timeSpanAttribute.showDays;
                showHours = timeSpanAttribute.showHours;
                showMinutes = timeSpanAttribute.showMinutes;
            }
            
            var labelRect = rect.AllocateLeft(EditorStyles.boldLabel.CalcSize(label).x);
            EditorGUI.PrefixLabel(labelRect, label, EditorStyles.boldLabel);

            var ticksProperty = property.FindPropertyRelative("<Ticks>k__BackingField");
            var ticks = ticksProperty.longValue;

            ARTimeSpan span = new(ticks);
            EditorGUI.BeginChangeCheck();
            DrawTimeSpan(ref rect, ref span, showDays, showHours, showMinutes);

            if (EditorGUI.EndChangeCheck()) {
                ticksProperty.longValue = span.Ticks;
            }
        }

        static void DrawTimeSpan(ref PropertyDrawerRects rect, ref ARTimeSpan timeSpan, bool drawDays, bool drawHours, bool drawMinutes) {
            var width = ((Rect)rect).width * 0.33f;
            if (drawDays) {
                var daysRect = (PropertyDrawerRects)rect.AllocateLeft(width);
                EditorGUI.BeginChangeCheck();
                var days = DrawTimeProp(ref daysRect, timeSpan.Days, DaysContent, DaysRange);
                if (EditorGUI.EndChangeCheck()) {
                    timeSpan.Days = days;
                }
            }

            if (drawHours) {
                var hoursRect = (PropertyDrawerRects)rect.AllocateLeft(width);
                EditorGUI.BeginChangeCheck();
                var hours = DrawTimeProp(ref hoursRect, timeSpan.Hours, HoursContent, HoursRange);
                if (EditorGUI.EndChangeCheck()) {
                    timeSpan.Hours = hours;
                }
            }

            if (drawMinutes) {
                var minutesRect = (PropertyDrawerRects)rect.AllocateLeft(width);
                EditorGUI.BeginChangeCheck();
                var minutes = DrawTimeProp(ref minutesRect, timeSpan.Minutes, MinutesContent, MinutesRange);
                if (EditorGUI.EndChangeCheck()) {
                    timeSpan.Minutes = minutes;
                }
            }
        }

        static int DrawTimeProp(ref PropertyDrawerRects rect, int value, GUIContent content, string[] values) {
            var width = EditorStyles.label.CalcSize(content).x;
            var titleRect = rect.AllocateLeft(width);
            rect.LeaveSpace(2);
            var valueRect = (Rect)rect;
            EditorGUI.LabelField(titleRect, content);
            return EditorGUI.Popup(valueRect, value, values);
        }
    }
}
