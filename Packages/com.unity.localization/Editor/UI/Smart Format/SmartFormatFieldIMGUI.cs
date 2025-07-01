using System.Collections.Generic;
#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Localization.Bridge;
#endif
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.Localization.SmartFormat.Core.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class SmartFormatFieldInfoWindow : PopupWindowContent
    {
        Placeholder m_Contents;

        public SmartFormatFieldInfoWindow(Placeholder ph)
        {
            m_Contents = ph;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 250);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUI.HelpBox(rect, string.Empty, MessageType.None);

            rect.xMin += 3; // margin;
            rect.yMin += 3;

            EditorGUILayout.HelpBox("Placeholder", MessageType.Info);

            EditorGUILayout.LabelField("Nested Depth", m_Contents.NestedDepth.ToString());

            EditorGUILayout.LabelField("Alignment", m_Contents.Alignment.ToString());

            if (!string.IsNullOrEmpty(m_Contents.FormatterName))
            {
                EditorGUILayout.LabelField("Formatter Name", m_Contents.FormatterName);
                EditorGUILayout.LabelField("Formatter Options", m_Contents.FormatterOptions);
            }

            if (m_Contents.Selectors.Count > 0)
            {
                EditorGUILayout.LabelField("Selectors");
                foreach (var selector in m_Contents.Selectors)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("• " + selector.RawText);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    class SmartFormatFieldIMGUI
    {
        /// <summary>
        /// Is the field plain text or using Smart Format?
        /// </summary>
        public bool IsSmart
        {
            get => m_IsSmart;
            set
            {
                m_Mode = Mode.Edit;
                SessionState.SetInt(SessionKey, (int)m_Mode);
                m_IsSmart = value;
            }
        }

        /// <summary>
        /// The table that this field is part of.
        /// </summary>
        public StringTable Table { get; set; }

        /// <summary>
        /// Key id for the table entry.
        /// </summary>
        public long KeyId { get; set; }

        /// <summary>
        /// TODO: DOC
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// The value we store inside the table. This could be a SmartFormat value or just plain text.
        /// </summary>
        public string RawText
        {
            get => m_RawText;
            set
            {
                if (m_RawText == value)
                    return;

                m_RawText = value;
                ResetCache();
            }
        }

        /// <summary>
        /// Single line version of Raw text for showing in a label
        /// </summary>
        public string Label
        {
            get
            {
                if (m_Label == null)
                {
                    if (string.IsNullOrEmpty(RawText))
                    {
                        m_Label = "<empty>";
                    }
                    else
                    {
                        var idx = RawText.IndexOf('\n');
                        m_Label = idx != -1 ? RawText.Substring(0, idx) : RawText;
                    }
                }
                return m_Label;
            }
        }

        public LocalizedString LocalizedString { get; set; }

        /// <summary>
        /// Show the metadata edit button
        /// </summary>
        public bool ShowMetadataButton { get; set; } = true;

        /// <summary>
        /// Show an additional tab for previewing the value using scene assets.
        /// </summary>
        public bool ShowPreviewTab { get; set; } = false;

        public float MinHeight { get; set; } = 100;

        /// <summary>
        /// Debug text is the RawText with hyper links inserted for Smart Format fields, we provide info when the links are clicked on.
        /// </summary>
        public string DebugText
        {
            get
            {
                if (m_DebugText == null)
                {
                    // Print the error in the message and avoid throwing actions. (LOC-119)
                    var oldParseAction = m_SmartFormatter.Settings.ParseErrorAction;
                    var oldFormatArgumentAction = m_SmartFormatter.Settings.FormatErrorAction;
                    m_SmartFormatter.Settings.ParseErrorAction = ErrorAction.OutputErrorInResult;
                    m_SmartFormatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;

                    m_Format = m_SmartFormatter.Parser.ParseFormat(RawText, m_SmartFormatter.GetNotEmptyFormatterExtensionNames());
                    m_FormatItemLookup.Clear();
                    int id = 0;
                    m_DebugText = FormatToDebugString(m_Format, ref id);

                    m_SmartFormatter.Settings.ParseErrorAction = oldParseAction;
                    m_SmartFormatter.Settings.FormatErrorAction = oldFormatArgumentAction;
                }
                return m_DebugText;
            }
        }

        public string PreviewText
        {
            get
            {
                if (m_PreviewText == null)
                {
                    // Print the error in the message and avoid throwing actions. (LOC-119)
                    var oldParseAction = m_SmartFormatter.Settings.ParseErrorAction;
                    var oldFormatArgumentAction = m_SmartFormatter.Settings.FormatErrorAction;
                    m_SmartFormatter.Settings.ParseErrorAction = ErrorAction.OutputErrorInResult;
                    m_SmartFormatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;

                    var locale = LocalizationEditorSettings.GetLocale(Table.LocaleIdentifier.Code);

                    foreach (var v in m_VariableChangedEvents)
                    {
                        v.ValueChanged -= VariableValueChanged;
                    }
                    m_VariableChangedEvents.Clear();

                    var formatCache = FormatCachePool.Get(m_SmartFormatter.Parser.ParseFormat(RawText, m_SmartFormatter.GetNotEmptyFormatterExtensionNames()));
                    formatCache.LocalVariables = LocalizedString;
                    formatCache.Table = Table;

                    m_PreviewText = m_SmartFormatter?.FormatWithCache(ref formatCache, RawText, locale?.Formatter, LocalizedString?.Arguments);
                    m_VariableChangedEvents.AddRange(formatCache.VariableTriggers);

                    foreach (var v in m_VariableChangedEvents)
                    {
                        v.ValueChanged += VariableValueChanged;
                    }

                    FormatCachePool.Release(formatCache);

                    m_SmartFormatter.Settings.ParseErrorAction = oldParseAction;
                    m_SmartFormatter.Settings.FormatErrorAction = oldFormatArgumentAction;

                    CalcHeight();
                }
                return m_PreviewText;
            }
        }

        void VariableValueChanged(IVariable obj) => m_PreviewText = null;

        string SessionKey => $"{Table.TableCollectionName}-{Table.LocaleIdentifier.Code}-{KeyId}";

        public bool Selected { get; set; }

        static readonly string[] k_Modes = {"Edit", "Debug"};
        static readonly string[] k_ModesWithPreview = {"Edit", "Debug", "Preview"};
        static readonly GUIContent k_MetadataIcon = new GUIContent(EditorIcons.Metadata, "Edit Table Entry Metadata");

        const float k_ToolbarHeight = 20;

        enum Mode
        {
            Edit,
            Debug,
            Preview
        }

        Mode m_Mode = Mode.Edit;
        string m_DebugText = null;
        string m_PreviewText = null;
        string m_Label = null;
        string m_RawText;
        bool m_IsSmart;

        GUIStyle m_TextAreaStyle;
        GUIStyle m_DebugTextAreaStyle;

        SmartFormatter m_SmartFormatter;
        Format m_Format;
        List<IVariableValueChanged> m_VariableChangedEvents = new List<IVariableValueChanged>();

        Dictionary<string, FormatItem> m_FormatItemLookup = new Dictionary<string, FormatItem>();

        public SmartFormatFieldIMGUI()
        {
            m_SmartFormatter = LocalizationEditorSettings.ActiveLocalizationSettings?.GetStringDatabase()?.SmartFormatter;
#if UNITY_2021_2_OR_NEWER
            EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;
#else
            EditorGUIBridge.hyperLinkClicked += EditorGUI_HyperLinkClicked;
#endif

            m_TextAreaStyle = new GUIStyle("TextField") { wordWrap = true };
            m_DebugTextAreaStyle = new GUIStyle("TextField") { wordWrap = false, richText = true };

            LocalizationEditorSettings.EditorEvents.TableEntryModified += OnValueChange;
        }

        ~SmartFormatFieldIMGUI()
        {
            LocalizationEditorSettings.EditorEvents.TableEntryModified -= OnValueChange;
#if UNITY_2021_2_OR_NEWER
            EditorGUI.hyperLinkClicked -= EditorGUI_HyperLinkClicked;
#else
            EditorGUIBridge.hyperLinkClicked -= EditorGUI_HyperLinkClicked;
#endif
        }

        void OnValueChange(SharedTableData.SharedTableEntry tableEntry)
        {
            var entry = Table.SharedData.GetEntry(KeyId);
            if (entry != null && entry == tableEntry)
            {
                RefreshData();
            }
        }

        /// <summary>
        /// Read the latest data for this key entry from the table. Called when the data we have may no longer be correct, such as after an Undo event.
        /// </summary>
        public void RefreshData()
        {
            ResetCache();
            var entry = Table.GetEntry(KeyId);
            if (entry != null)
            {
                m_IsSmart = entry.IsSmart;
                m_RawText = entry.Value;
            }
            else
            {
                m_IsSmart = false;
                RawText = string.Empty;
            }

            // Extract the previous state.
            m_Mode = (Mode)SessionState.GetInt(SessionKey, (int)Mode.Edit);

            CalcHeight();
        }

        void CalcHeight()
        {
            var text = m_Mode == Mode.Preview ? PreviewText : RawText;
            var contentsHeight = m_TextAreaStyle.CalcSize(new GUIContent(text)).y + EditorGUIUtility.standardVerticalSpacing;
            contentsHeight += k_ToolbarHeight + EditorGUIUtility.standardVerticalSpacing;
            if (IsSmart)
                contentsHeight += EditorStyles.toolbarButton.lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Height = Mathf.Max(MinHeight, contentsHeight);
        }

        public void ResetCache()
        {
            m_DebugText = null;
            m_PreviewText = null;
            m_Format = null;
            m_Label = null;
        }

        /// <summary>
        /// Called when a hyper link is clicked in Debug mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        #if UNITY_2021_2_OR_NEWER
        void EditorGUI_HyperLinkClicked(EditorWindow wnd, HyperLinkClickedEventArgs args)
        #else
        void EditorGUI_HyperLinkClicked(object sender, System.EventArgs e)
        #endif
        {
            #if UNITY_2021_2_OR_NEWER
            var o = args.hyperLinkData;
            #else
            var o = e.GetType().GetProperty("hyperlinkInfos").GetValue(e, null);
            #endif
            if (o is Dictionary<string, string> dict)
            {
                if (dict.TryGetValue("href", out var formatId) && m_FormatItemLookup.TryGetValue(formatId, out var formatItem))
                {
                    if (formatItem is Placeholder ph)
                    {
                        var mousePos =  Event.current.mousePosition;
                        mousePos.y -= EditorWindow.mouseOverWindow.position.height * 0.5f;
                        mousePos.y += 80;

                        // relative to the window
                        var rect = new Rect(mousePos.x, mousePos.y, 200, 200);
                        PopupWindow.Show(rect, new SmartFormatFieldInfoWindow(ph));
                    }
                    else
                    {
                        Debug.Log("Cant show " + formatItem.GetType());
                    }
                }
            }
        }

        /// <summary>
        /// Draws the field including the toolbar.
        /// </summary>
        /// <param name="rect"></param>
        public bool Draw(Rect rect)
        {
            const float metaDataButtonWidth = 25;
            bool change = false;

            if (ShowMetadataButton)
            {
                var btnRect = new Rect(rect.x, rect.y, metaDataButtonWidth, k_ToolbarHeight);
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(btnRect, Selected, k_MetadataIcon, GUI.skin.button);
                if (EditorGUI.EndChangeCheck())
                {
                    Selected = !Selected;
                }
            }

            EditorGUI.BeginChangeCheck();

            var smartFormatFieldWidth = rect.width - (ShowMetadataButton ? metaDataButtonWidth + 2 : 0);
            var smartFormatRect = new Rect(rect.x + (ShowMetadataButton ? metaDataButtonWidth + 2 : 0), rect.y, smartFormatFieldWidth, k_ToolbarHeight);
            var newIsSmart = GUI.Toggle(smartFormatRect, IsSmart, "Smart");
            if (EditorGUI.EndChangeCheck())
            {
                SetIsSmart(newIsSmart);
                change = true;
            }

            rect.yMin += k_ToolbarHeight + EditorGUIUtility.standardVerticalSpacing;

            if (IsSmart)
            {
                var header = new Rect(rect.x, rect.y, rect.width, EditorStyles.toolbarButton.lineHeight);
                var choices =  ShowPreviewTab ? k_ModesWithPreview : k_Modes;
                EditorGUI.BeginChangeCheck();
                m_Mode = (Mode)GUI.SelectionGrid(header, (int)m_Mode, choices, choices.Length, EditorStyles.miniButtonMid);
                if (EditorGUI.EndChangeCheck())
                {
                    // Store the state in case the field is reset when a change occurs.
                    // We dont want to keep going back to the Edit tab when someone is changing local variable properties etc.
                    SessionState.SetInt(SessionKey, (int)m_Mode);
                }
                rect.yMin += header.height + EditorGUIUtility.standardVerticalSpacing;
            }

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            switch (m_Mode)
            {
                case Mode.Edit:
                    EditorGUI.BeginChangeCheck();
                    var newText = EditorGUI.TextArea(rect, RawText, m_TextAreaStyle);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetValue(newText);
                        change = true;
                    }
                    break;

                case Mode.Debug:
                    EditorGUI.SelectableLabel(rect, DebugText, m_DebugTextAreaStyle);
                    break;

                case Mode.Preview:
                    // TODO: List of references and the results.
                    EditorGUI.LabelField(rect, PreviewText, m_TextAreaStyle);
                    break;
            }

            EditorGUI.indentLevel = indent;
            return change;
        }

        StringTableEntry GetOrCreateEntry()
        {
            return Table.GetEntry(KeyId) ?? Table.AddEntry(KeyId, string.Empty);
        }

        internal void SetIsSmart(bool value)
        {
            Undo.RecordObject(Table, "Set smart format");

            // This is required as Undo does not make assets dirty
            EditorUtility.SetDirty(Table);

            var entry = GetOrCreateEntry();
            entry.IsSmart = value;
            IsSmart = value;
            CalcHeight();

            LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(Table.SharedData.GetEntry(entry.KeyId));
        }

        internal void SetValue(string value)
        {
            Undo.RecordObject(Table, "Set localized value");

            // This is required as Undo does not make assets dirty
            EditorUtility.SetDirty(Table);

            var entry = GetOrCreateEntry();
            entry.Value = value;
            RawText = value;
            CalcHeight();

            LocalizationEditorSettings.EditorEvents.RaiseTableEntryModified(Table.SharedData.GetEntry(entry.KeyId));
        }

        /// <summary>
        /// A debug string is a rich text string that uses hyper links to allow for clicking and inspecting smart format fields.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        string FormatToDebugString(Format format, ref int nextId)
        {
            using (StringBuilderPool.Get(out var result))
            {
                foreach (var item in format.Items)
                {
                    if (item is Placeholder ph)
                    {
                        var id = $"{Table.GetInstanceID()}-{KeyId}-{nextId++}";
                        m_FormatItemLookup.Add(id, ph);
                        result.Append($"<a href=\"{id}\">"); // TODO: Support Different selectors from Settings
                        result.Append("<b>");
                        result.Append('{');
                        result.Append("</b>");
                        foreach (var s in ph.Selectors)
                        {
                            result.Append("<b><color=#AE6503>");
                            result.Append(s.baseString, s.operatorStart, s.endIndex - s.operatorStart);
                            result.Append("</color></b>");
                        }
                        if (ph.Alignment != 0)
                        {
                            result.Append("<b>");
                            result.Append(',');
                            result.Append(ph.Alignment);
                            result.Append("</b>");
                        }

                        if (ph.FormatterName != "")
                        {
                            result.Append(':');
                            result.Append("<color=#937D39>");
                            result.Append(ph.FormatterName);
                            if (ph.FormatterOptions != "")
                            {
                                result.Append('(');
                                result.Append(ph.FormatterOptions);
                                result.Append(')');
                            }
                            result.Append("</color>");
                        }

                        if (ph.Format != null)
                        {
                            result.Append(':');
                            result.Append(FormatToDebugString(ph.Format, ref nextId));
                        }

                        result.Append("<b>");
                        result.Append('}');
                        result.Append("</b>");
                        result.Append("</a>");
                    }
                    else
                    {
                        result.Append(item.RawText);
                    }
                }

                return result.ToString();
            }
        }

        public override string ToString() => $"SmartFormatField({RawText})";
    }
}
