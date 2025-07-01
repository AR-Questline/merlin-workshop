using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Utility.RichLabels;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using Awaken.Utility.UI;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(TaskNode))]
    public class TaskNodeEditor : StoryNodeEditor {
        enum NoteType : byte {
            // ReSharper disable InconsistentNaming
            NOTE = 0,
            TODO = 1,
            ISSUE = 2,
            CRITICAL = 3
            // ReSharper restore InconsistentNaming
        }
        static readonly Color[] TypeColors = {
            ARColor.EditorGrey,
            ARColor.EditorBlue,
            ARColor.EditorSecondaryRed,
            ARColor.EditorRed
        };
        
        TaskNode Target => (TaskNode) target;
        RichLabelConfig _config;
        IRichLabelUser _richLabelUser;

        public override void OnCreate() {
            base.OnCreate();
            _config = RichLabelEditorUtilities.GetOrCreateRichLabelConfig(Target.RichLabelConfigType);
        }

        protected override void BeforeElements() {
            bool isFolded = Node.Folded;
            if (GUILayout.Button(isFolded ? UnfoldString : FoldString)) {
                Node.Folded = !isFolded;
            }
        }

        protected override void DrawElements() {
            GUIUtils.PushContextWidth(GetWidth());
            //ObjectTree.Draw();
            GUIUtils.PopContextWidth();
            // change check does not work due to delayed editing in popups
            UpdateName();
        }

        void UpdateName() {
            const string TaskComment = "Task Comment";
            if (Target.RichLabelSet == null) return;
            
            var existingPersistantData = _config.TryGetSavedLabels(Target.RichLabelSet);
            
            if (!existingPersistantData.IsNullOrEmpty() && existingPersistantData[0] != null) {
                Enum.TryParse(existingPersistantData[0].Name, out NoteType type);
                string targetName = existingPersistantData.Length >= 2 
                                        ? $"{existingPersistantData[0].Name.ColoredText(TypeColors[(int) type])}: {existingPersistantData[1]?.Name}" 
                                        : $"{existingPersistantData[0].Name.ColoredText(TypeColors[(int) type])}: {TaskComment}";
                if (targetName != target.name) {
                    target.name = targetName;
                    EditorUtility.SetDirty(target);
                }
            } else {
                if (target.name != TaskComment) {
                    target.name = TaskComment;
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}