using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class StringTableTreeViewItem : GenericAssetTableTreeViewItem<StringTable>
    {
        class Data
        {
            public StringTable table;
            public string localizedText;
            public bool isSmart;
            public float height;

            public void UpdateHeight() => height = s_TextAreaStyle.CalcSize(new GUIContent(localizedText)).y;
        }

        StringTableCollection m_StringTableCollection;
        Data[] m_TableProperties;
        List<LocalizationTable> m_SortedTables;
        int m_StartIndex;

        const string k_WordWrapKey = "Localization-Table-WordWrap";
        static readonly GUIStyle s_TextAreaStyle = new GUIStyle("TextField") { wordWrap = WordWrap };

        public static bool WordWrap
        {
            get => EditorPrefs.GetBool(k_WordWrapKey, false);
            set
            {
                EditorPrefs.SetBool(k_WordWrapKey, value);
                s_TextAreaStyle.wordWrap = value;
            }
        }

        public override string displayName
        {
            get
            {
                if (base.displayName == null)
                    UpdateSearchString();
                return base.displayName;
            }
            set => base.displayName = value;
        }

        public override void Initialize(LocalizationTableCollection collection, int startIdx, List<LocalizationTable> sortedTables)
        {
            m_SortedTables = sortedTables;
            m_StartIndex = startIdx;
            m_StringTableCollection = (StringTableCollection)collection;
        }

        void DelayedInit()
        {
            if (m_TableProperties == null)
            {
                m_TableProperties = new Data[m_StartIndex + m_SortedTables.Count];

                for (int i = m_StartIndex; i < m_TableProperties.Length; ++i)
                {
                    var data = new Data
                    {
                        table = m_SortedTables[i - m_StartIndex] as StringTable,
                    };
                    m_TableProperties[i] = data;
                }

                RefreshFields();
            }
        }

        public float GetHeight(int colIdx)
        {
            DelayedInit();

            // Some cols may be missing table cols.
            if (colIdx < m_TableProperties.Length && m_TableProperties[colIdx] != null)
                return m_TableProperties[colIdx].height;
            return 0;
        }

        public void RefreshFields()
        {
            for (int i = 0; i < m_TableProperties.Length; ++i)
            {
                if (m_TableProperties[i] != null)
                {
                    var data = m_TableProperties[i];
                    var entry = m_TableProperties[i].table.GetEntry(KeyId);
                    if (entry != null)
                    {
                        data.isSmart = entry.IsSmart;
                        data.localizedText = entry.LocalizedValue;
                        data.height = s_TextAreaStyle.CalcSize(new GUIContent(data.localizedText)).y;
                    }
                    else
                    {
                        data.isSmart = false;
                        data.localizedText = string.Empty;
                        data.height = s_TextAreaStyle.fixedHeight;
                    }

                    data.UpdateHeight();
                }
            }

            UpdateSearchString();
        }

        public bool Draw(int colIdx, Rect rect, StringTableListView listView)
        {
            DelayedInit();

            var data = m_TableProperties[colIdx];

            EditorGUI.BeginChangeCheck();
            var newString = EditorGUI.TextArea(rect, data.localizedText, s_TextAreaStyle);
            if (EditorGUI.EndChangeCheck())
            {
                data.localizedText = newString;

                Undo.RecordObject(data.table, "Set localized value");

                // This is required as Undo does not make assets dirty
                EditorUtility.SetDirty(data.table);

                var entry = data.table.GetEntry(KeyId) ?? data.table.AddEntry(KeyId, string.Empty);
                entry.Value = newString;
                data.UpdateHeight();

                LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(SharedEntry);
                UpdateSearchString();
                return true;
            }

            return false;
        }

        public override void OnDeleteKey()
        {
            DelayedInit();
            foreach (var tableField in m_TableProperties)
            {
                if (tableField != null)
                    tableField.table?.RemoveEntry(KeyId);
            }
        }

        public bool AreAllSmart()
        {
            DelayedInit();
            foreach (var tableField in m_TableProperties)
            {
                if (tableField != null && !tableField.isSmart)
                    return false;
            }
            return true;
        }

        public bool IsSmart(int colIdx)
        {
            DelayedInit();
            return m_TableProperties[colIdx]?.isSmart == true;
        }

        public void SetSmart(int colIdx, bool smart)
        {
            DelayedInit();
            var data = m_TableProperties[colIdx];
            if (data != null)
            {
                Undo.RecordObject(data.table, "Set Smart String");
                EditorUtility.SetDirty(data.table);

                var entry = data.table.GetEntry(KeyId);

                // We require an entry to be present in the table before we can set it to be smart.
                if (entry == null && smart)
                    entry = data.table.AddEntry(KeyId, string.Empty);

                if (entry != null)
                    entry.IsSmart = smart;
            }

            RefreshFields();
        }

        public void SetSmartAll(bool smart)
        {
            DelayedInit();

            using (new UndoScope("Set Smart String", true))
            {
                foreach (var st in m_StringTableCollection.StringTables)
                {
                    Undo.RecordObject(st, "Set Smart String");
                    EditorUtility.SetDirty(st);

                    var entry = st.GetEntry(KeyId);

                    // We require an entry to be present in the table before we can set it to be smart.
                    if (entry == null && smart)
                        entry = st.AddEntry(KeyId, string.Empty);

                    if (entry != null)
                        entry.IsSmart = smart;
                }
            }

            RefreshFields();
        }

        void UpdateSearchString()
        {
            using (StringBuilderPool.Get(out var sb))
            {
                sb.Clear();

                sb.Append(SharedEntry.Id.ToString());
                sb.Append(SharedEntry.Key);

                foreach (StringTable st in m_SortedTables)
                {
                    var entry = st.GetEntry(KeyId);
                    if (entry != null)
                        sb.Append(entry.LocalizedValue);
                }

                displayName = sb.ToString();
            }
        }
    }
}
