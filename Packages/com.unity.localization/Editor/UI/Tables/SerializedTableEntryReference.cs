using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class SerializedTableEntryReference
    {
        public SerializedProperty key;
        public SerializedProperty keyId;
        TableEntryReference m_Reference;

        public SerializedProperty Property { get; }

        public bool HasMultipleDifferentValues => key.hasMultipleDifferentValues || keyId.hasMultipleDifferentValues;

        public TableEntryReference Reference
        {
            get => m_Reference;
            set
            {
                m_Reference = value;
                key.stringValue = m_Reference.Key;
                keyId.longValue = m_Reference.KeyId;
            }
        }

        public void SetReference(SharedTableData.SharedTableEntry entry)
        {
            if (entry == null)
                Reference = null;
            else if (LocalizationEditorSettings.EntryReferenceMethod == EntryReferenceMethod.Id)
                Reference = entry.Id;
            else
                Reference = entry.Key;
        }

        public SerializedTableEntryReference(SerializedProperty property)
        {
            Property = property;
            key = property.FindPropertyRelative("m_Key");
            keyId = property.FindPropertyRelative("m_KeyId");

            if (HasMultipleDifferentValues)
                return;

            var id = keyId.longValue;
            if (id != SharedTableData.EmptyId)
            {
                Reference = id;
            }
            else
            {
                var keyName = key.stringValue;
                if (!string.IsNullOrEmpty(keyName))
                    Reference = keyName;
            }
        }
    }
}
