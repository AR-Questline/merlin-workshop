using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    // /// <summary>
    // /// Used in game objects, E.g. in NpcPresenceAttachments.
    // /// </summary>
    // public class SourceRichLabelBrowser : RichLabelBrowser {
    //     List<string> _guids;
    //     List<string> _filteredGuids;
    //     InspectorProperty _inspectorProperty;
    //     Action<InspectorProperty, List<string>> _onChange;
    //
    //     public static void Show(Rect source, RichLabelConfig richLabelConfig, IReadOnlyList<string> guids, InspectorProperty inspectorProperty, Action<InspectorProperty, List<string>> onChange) {
    //         SourceRichLabelBrowser eventBrowser = ScriptableObject.CreateInstance<SourceRichLabelBrowser>();
    //         eventBrowser.Init(guids, inspectorProperty, onChange);
    //         eventBrowser.ShowWindow(source, richLabelConfig);
    //     }
    //
    //     public void Init(IReadOnlyList<string> guids, InspectorProperty inspectorProperty, Action<InspectorProperty, List<string>> onChange) {
    //         _guids = guids.ToList();
    //         _onChange = onChange;
    //         _inspectorProperty = inspectorProperty;
    //     }
    //
    //     protected override void DrawRichLabel(RichLabel label, RichLabelCategory category) {
    //         Rect labelRect = GUILayoutUtility.GetRect(ColumnWidth, ColumnWidth, ColumnItemHeight, ColumnItemHeight);
    //         bool isLabelSelected = _guids.Contains(label.Guid);
    //         var guiStyle = isLabelSelected ? RichLabelStyles.IncludedLabel : RichLabelStyles.NeutralLabel;
    //         if (GUI.Button(labelRect, label.Name, guiStyle)) {
    //             if (isLabelSelected) {
    //                 _guids.Remove(label.Guid);
    //             } else if (category.SingleChoice) {
    //                 _guids.RemoveAll(g => category.Labels.Any(e => e.Guid == g));
    //                 _guids.Add(label.Guid);
    //             } else {
    //                 _guids.Add(label.Guid);
    //             }
    //             _onChange?.Invoke(_inspectorProperty, _guids);
    //         }
    //     }
    // }
}