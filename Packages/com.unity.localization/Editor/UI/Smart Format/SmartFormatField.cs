#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class SmartFormatField : VisualElement
    {
        StringTable m_Table;
        long m_EntryId;
        SerializedProperty m_LocalizedString;
        Toggle m_SmartToggle;
        Button m_PreviewButton;
        Label m_PreviewText;
        List<IVariableValueChanged> m_VariableChangedEvents = new List<IVariableValueChanged>();

        public TextField ValueField { get; }

        public SmartFormatField(StringTable table, long entryId, SerializedProperty localizedStringProperty)
        {
            m_Table = table;
            m_EntryId = entryId;
            m_LocalizedString = localizedStringProperty;

            var asset = Resources.GetTemplateAsset(nameof(SmartFormatField));
            asset.CloneTree(this);
            ValueField = this.Q<TextField>("value-field");
            ValueField.RegisterValueChangedCallback(ValueChanged);

            m_SmartToggle = this.Q<Toggle>("smart-toggle");
            m_SmartToggle.RegisterValueChangedCallback(SmartChanged);

            // Add toggle to clicking the label
            var smartLabel = this.Q<Label>("smart-label");
            var clicked = new Clickable(() => m_SmartToggle.value = !m_SmartToggle.value);
            smartLabel.AddManipulator(clicked);

            m_PreviewButton = this.Q<Button>("preview-button");
            m_PreviewButton.clicked += GeneratePreview;

            m_PreviewText = this.Q<Label>("preview-text");

            RefreshFields();
        }

        void RefreshFields()
        {
            var entry = GetEntry(false);
            if (entry != null)
            {
                ValueField.SetValueWithoutNotify(entry.Value);
                m_SmartToggle.SetValueWithoutNotify(entry.IsSmart);
            }
            else
            {
                ValueField.SetValueWithoutNotify(string.Empty);
                m_SmartToggle.SetValueWithoutNotify(false);
            }

            RefreshSmartFields();
        }

        void RefreshSmartFields()
        {
            m_PreviewButton.visible = m_SmartToggle.value;
            m_PreviewText.style.display = DisplayStyle.None;
            m_PreviewText.text = string.Empty;
        }

        StringTableEntry GetEntry(bool create)
        {
            var entry = m_Table.GetEntry(m_EntryId);
            if (entry == null && create)
                entry = m_Table.AddEntry(m_EntryId, string.Empty);
            return entry;
        }

        void SmartChanged(ChangeEvent<bool> evt)
        {
            Undo.RecordObject(m_Table, "Set smart format");

            // This is required as Undo does not make assets dirty
            EditorUtility.SetDirty(m_Table);

            var entry = GetEntry(true);
            entry.IsSmart = evt.newValue;

            RefreshSmartFields();

            LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(m_Table.SharedData.GetEntry(m_EntryId));
        }

        void ValueChanged(ChangeEvent<string> evt)
        {
            Undo.RecordObject(m_Table, "Set localized value");

            // This is required as Undo does not make assets dirty
            EditorUtility.SetDirty(m_Table);

            var entry = GetEntry(true);
            entry.Value = evt.newValue;


            LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(m_Table.SharedData.GetEntry(m_EntryId));
        }

        LocalizedString GetLocalizedString()
        {
            var ls = m_LocalizedString.managedReferenceValue;
            if (ls is LocalizedString.UxmlSerializedData uxmlLocalizedString)
            {
                var localizedString = new LocalizedString();
                uxmlLocalizedString.Deserialize(localizedString);
                localizedString.Add("test", new StringVariable { Value = "Hello world" });
                return localizedString;
            }
            else if (ls is LocalizedString localizedString)
            {
                return localizedString;
            }

            return null;
        }

        void GeneratePreview()
        {
            var smartFormatter = LocalizationEditorSettings.ActiveLocalizationSettings?.GetStringDatabase()?.SmartFormatter ?? Smart.Default;

            // Print the error in the message and avoid throwing actions.
            var oldParseAction = smartFormatter.Settings.ParseErrorAction;
            var oldFormatArgumentAction = smartFormatter.Settings.FormatErrorAction;
            smartFormatter.Settings.ParseErrorAction = ErrorAction.OutputErrorInResult;
            smartFormatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;

            var locale = LocalizationEditorSettings.GetLocale(m_Table.LocaleIdentifier.Code);

            foreach (var v in m_VariableChangedEvents)
            {
                v.ValueChanged -= VariableValueChanged;
            }
            m_VariableChangedEvents.Clear();

            var formatCache = FormatCachePool.Get(smartFormatter.Parser.ParseFormat(ValueField.value, smartFormatter.GetNotEmptyFormatterExtensionNames()));
            var localizedString = GetLocalizedString();
            formatCache.LocalVariables = localizedString;
            formatCache.Table = m_Table;

            m_PreviewText.style.display = DisplayStyle.Flex;
            m_PreviewText.text = smartFormatter?.FormatWithCache(ref formatCache, ValueField.value, locale?.Formatter, localizedString?.Arguments);
            m_VariableChangedEvents.AddRange(formatCache.VariableTriggers);

            foreach (var v in m_VariableChangedEvents)
            {
                v.ValueChanged += VariableValueChanged;
            }

            FormatCachePool.Release(formatCache);

            smartFormatter.Settings.ParseErrorAction = oldParseAction;
            smartFormatter.Settings.FormatErrorAction = oldFormatArgumentAction;
        }

        void VariableValueChanged(IVariable obj) => GeneratePreview();
    }
}

#endif
