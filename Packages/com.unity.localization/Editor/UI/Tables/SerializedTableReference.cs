using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class SerializedTableReference
    {
        TableReference m_Reference;

        public SerializedProperty Property { get; }

        public SerializedProperty TableNameProperty { get; }

        public bool HasMultipleDifferentValues => TableNameProperty.hasMultipleDifferentValues;

        public TableReference Reference
        {
            get => m_Reference;
            set
            {
                m_Reference = value;
                TableNameProperty.stringValue = m_Reference.GetSerializedString();
            }
        }

        public void SetReference(LocalizationTableCollection collection)
        {
            if (collection == null)
                Reference = null;
            else if (LocalizationEditorSettings.TableReferenceMethod == TableReferenceMethod.Guid)
                Reference = collection.TableCollectionNameReference;
            else
                Reference = collection.TableCollectionName;
        }

        public SerializedTableReference(SerializedProperty property)
        {
            Property = property;
            TableNameProperty = property.FindPropertyRelative("m_TableCollectionName");

            if (HasMultipleDifferentValues)
                return;

            Reference = TableReference.TableReferenceFromString(TableNameProperty.stringValue);
        }
    }
}
