// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR
using Animancer.Editor;
using UnityEditor;
using UnityEngine;
using System;
#endif

namespace Animancer.Units
{
    /// <summary>[Editor-Conditional]
    /// Causes a float field to display a suffix to indicate what kind of units the value represents as well as
    /// displaying it as several different fields which convert the value between different units.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/other/units">Units Attribute</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Units/UnitsAttribute
    /// 
    [System.Diagnostics.Conditional(Strings.UnityEditor)]
    public class UnitsAttribute : SelfDrawerAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The validation rule applied to the value.</summary>
        public Validate.Value Rule { get; set; }

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="UnitsAttribute"/>.</summary>
        protected UnitsAttribute() {
        }

        /// <summary>Creates a new <see cref="UnitsAttribute"/>.</summary>
        public UnitsAttribute(string suffix)
        {
        }

        /// <summary>Creates a new <see cref="UnitsAttribute"/>.</summary>
        public UnitsAttribute(float[] multipliers, string[] suffixes, int unitIndex = 0)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The unit conversion ratios.</summary>
        /// <remarks><c>valueInUnitX = valueInBaseUnits * Multipliers[x];</c></remarks>
        public float[] Multipliers { get; private set; }

        /// <summary>[Editor-Only] The converters used to generate display strings for each of the fields.</summary>
        public CompactUnitConversionCache[] DisplayConverters { get; private set; }

        /// <summary>[Editor-Only] The index of the <see cref="DisplayConverters"/> for the attributed serialized value.</summary>
        public int UnitIndex { get; private set; }

        /// <summary>[Editor-Only] Should the field have a toggle to set its value to <see cref="float.NaN"/>?</summary>
        public bool IsOptional { get; set; }

        /// <summary>[Editor-Only] The value to display if the actual value is <see cref="float.NaN"/>.</summary>
        public float DefaultValue { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Sets the unit details.</summary>
        protected void SetUnits(float[] multipliers, CompactUnitConversionCache[] displayConverters, int unitIndex = 0)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns <see cref="AnimancerGUI.StandardSpacing"/>.</summary>
        protected static float StandardSpacing => AnimancerGUI.StandardSpacing;

        /// <summary>[Editor-Only] Returns <see cref="AnimancerGUI.LineHeight"/>.</summary>
        protected static float LineHeight => AnimancerGUI.LineHeight;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /// <summary>[Editor-Only] Determines how many lines tall the `property` should be.</summary>
        protected virtual int GetLineCount(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.wideMode ? 1 : 2;

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Begins a GUI property block to be ended by <see cref="EndProperty"/>.</summary>
        protected static void BeginProperty(Rect area, SerializedProperty property, ref GUIContent label, out float value)
        {
            value = default(float);
        }

        /// <summary>[Editor-Only] Ends a GUI property block started by <see cref="BeginProperty"/>.</summary>
        protected static void EndProperty(Rect area, SerializedProperty property, ref float value)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws this attribute's fields for the `property`.</summary>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws this attribute's fields.</summary>
        public void DoFieldGUI(Rect area, GUIContent label, ref float value)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws a <see cref="EditorGUI.FloatField(Rect, GUIContent, float)"/> with an alternate string when it is not
        /// selected (for example, "1" might become "1s" to indicate "seconds").
        /// </summary>
        /// <remarks>
        /// This method treats most <see cref="EventType"/>s normally, but for <see cref="EventType.Repaint"/> it
        /// instead draws a text field with the converted string.
        /// </remarks>
        public static float DoSpecialFloatField(Rect area, GUIContent label, float value, CompactUnitConversionCache toString)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void DoOptionalBeforeGUI(bool isOptional, Rect area, out Rect toggleArea, out bool guiWasEnabled, out float previousLabelWidth)
        {
            toggleArea = default(Rect);
            guiWasEnabled = default(bool);
            previousLabelWidth = default(float);
        }

        /************************************************************************************************************************/

        private void DoOptionalAfterGUI(bool isOptional, Rect area, ref float value, float defaultValue, bool guiWasEnabled, float previousLabelWidth)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns the value that should be displayed for a given field.</summary>
        public static float GetDisplayValue(float value, float defaultValue)
            => float.IsNaN(value) ? defaultValue : value;

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

