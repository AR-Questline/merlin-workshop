using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Localization.UI
{
    class PropertyDrawerExtendedData
    {
        // Called during Undo and when a change is made to the SerializedObject.
        public virtual void Reset() {}
    }

    /// <summary>
    /// Supports drawing properties for lists.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    abstract class PropertyDrawerExtended<TData> : PropertyDrawer where TData : PropertyDrawerExtendedData
    {
        protected Dictionary<(int, string), TData> PropertyData = new Dictionary<(int, string), TData>();
        int m_DirtyCount;

        public virtual TData GetDataForProperty(SerializedProperty property)
        {
            // We reset when a change is made so we can refresh items. If we do not then we may have
            // caching issues when an array item is moved as we have no way to know when this happens.
            var dirtyCount = EditorUtility.GetDirtyCount(property.serializedObject.targetObject);
            if (m_DirtyCount != dirtyCount)
            {
                foreach (var propertyDrawerExtendedData in PropertyData.Values)
                {
                    propertyDrawerExtendedData.Reset();
                }
                m_DirtyCount = dirtyCount;
            }

            var key = (property.serializedObject.GetHashCode(), property.propertyPath);
            if (!PropertyData.TryGetValue(key, out var propertyData))
            {
                propertyData = CreatePropertyData(property);
                PropertyData[key] = propertyData;
            }

            return propertyData;
        }

        const float k_PrefixPaddingRight = 2;
        public static float PrefixLabelWidth => EditorGUIUtility.labelWidth + k_PrefixPaddingRight;

        public abstract TData CreatePropertyData(SerializedProperty property);
        public abstract void OnGUI(TData data, Rect position, SerializedProperty property, GUIContent label);
        public abstract float GetPropertyHeight(TData data, SerializedProperty property, GUIContent label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = GetDataForProperty(property);
            OnGUI(data, position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var data = GetDataForProperty(property);
            return GetPropertyHeight(data, property, label);
        }

        protected void ClearPropertyDataCache() => PropertyData.Clear();
    }
}
