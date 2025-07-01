using System;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    /// <summary>
    /// Used as an data provider to choice.
    /// </summary>
    [Serializable]
    public class SingleChoice {
        public ChapterEditorNode targetChapter;
        public bool isMainChoice;
        // === Editable data to model choice in editor
        [TextArea(1, 20)]
        public LocString text;
        [NonSerialized]
        public string tooltip;

        public RuntimeChoice ToRuntimeChoice(StoryGraphParser parser) {
            return new RuntimeChoice {
                targetChapter = parser.GetChapter(targetChapter),
                isMainChoice = isMainChoice,
                text = text,
                Tooltip = tooltip
            };
        }
    }

    public struct RuntimeChoice {
        public StoryChapter targetChapter;
        public bool isMainChoice;
        public LocString text;
        public string Tooltip { get; set; }
    }
}