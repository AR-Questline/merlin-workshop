#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocalizedAsset<>.UxmlSerializedData), true)]
    class LocalizedAssetUxmlSerializedDataPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Find the asset type.
            var baseType = fieldInfo.FieldType;
            if (property.propertyType == SerializedPropertyType.ManagedReference)
                baseType = ManagedReferenceUtility.GetType(property.managedReferenceFullTypename);

            // Extract the LocalizedAsset type from the UxmlSerializedData type.
            baseType = baseType.DeclaringType;

            Type assetType = null;
            while (baseType != null)
            {
                if (baseType.IsArray)
                    baseType = baseType.GetElementType().BaseType;

                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(LocalizedAsset<>))
                {
                    assetType = baseType.GetGenericArguments()[0];
                    break;
                }
                baseType = baseType.BaseType;
            }
            Debug.Assert(assetType != null, "Could not determine the asset type for " + fieldInfo.FieldType.Name);

            return new LocalizedAssetField("Localized " + assetType.Name, property, assetType);
        }
    }
}

#endif
