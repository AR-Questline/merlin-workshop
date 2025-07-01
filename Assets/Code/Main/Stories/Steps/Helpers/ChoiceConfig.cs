using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Choices;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Stories.Utils;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Stories.Steps.Helpers {
    public class ChoiceConfig {
        ChoiceConfig(RuntimeChoice choiceData, string icon = "", Action callback = null) {
            _choiceData = choiceData;
            _icon = icon;
            _callback = callback;
        }

        ChoiceConfig(Prompt prompt) {
            _prompt = prompt;
        }
        
        public bool IsHighlighted { get; set; }
        
        public static ChoiceConfig WithData(RuntimeChoice singleChoice) => new(singleChoice);
        [UnityEngine.Scripting.Preserve] 
        public static ChoiceConfig WithDataAndIcon(RuntimeChoice choiceData, string icon) => new(choiceData, icon);
        public static ChoiceConfig WithCallback(RuntimeChoice choiceData, Action callback) => new(choiceData, callback: callback);
        public static ChoiceConfig WithEverything(RuntimeChoice choiceData, string icon, Action callback) => new(choiceData, icon, callback);
        public static ChoiceConfig WithPrompt(Prompt prompt) => new(prompt);

        RuntimeChoice _choiceData;
        ShareableSpriteReference _iconReference;
        string _icon;
        Action _callback;
        Prompt _prompt;
        bool _isHighlighted;

        public Prompt Prompt() {
            return _prompt;
        }
        
        public Action Callback() {
            return _callback;
        }
 
        public StoryChapter TargetChapter() {
            return _choiceData.targetChapter;
        }
        
        public bool IsMainChoice() {
            return _choiceData.isMainChoice;
        }
        
        public string DisplayText() {
            return _choiceData.text;
        }

        public string Tooltip() {
            return _choiceData.Tooltip;
        }

        public ShareableSpriteReference SpriteIcon() {
            return _iconReference;
        }

        public void AssignIconReference(ShareableSpriteReference iconReference) => _iconReference = iconReference;

        public string IconName(Story story) {
            return string.IsNullOrWhiteSpace(_icon) ? ChooseStat(story) : _icon.IconNameFromTag();
        }

        public string KnownEffects(Story story) {
            if (_choiceData.targetChapter == null) {
                return string.Empty;
            }
            var effects = new StructList<string>(0);
            foreach (var step in new StoryBranchIterator(_choiceData.targetChapter)) {
                step.AppendKnownEffects(story, ref effects);
            }
            if (effects.Count == 0) {
                return string.Empty;
            }
            return StoryTextStyle.KnownEffects.FormatList(effects, ChoiceEnabled(story));
        }

        public bool ChoiceEnabled(Story story) {
            return AreChoiceRequirementsFulfilled(story);
        }

        // === Helpers

        bool AreChoiceRequirementsFulfilled(Story story) {
            if (_choiceData.targetChapter == null) {
                return true;
            }
            foreach (var step in new StoryBranchIterator(_choiceData.targetChapter)) {
                var requirement = step.GetRequirement();
                if (requirement(story) == false) {
                    return false;
                }
            }
            return true;
        }

        public StructList<IHoverInfo> CollectHoverInfos(Story story) {
            var infos = new StructList<IHoverInfo>(0);
            if (_choiceData.targetChapter != null) {
                foreach (var step in new StoryBranchIterator(_choiceData.targetChapter)) {
                    step.AppendHoverInfo(story, ref infos);
                }
            }
            return infos;
        }

        string ChooseStat(Story story) {
            if (_choiceData.targetChapter == null) {
                return string.Empty;
            }
            
            var stats = new StructList<string>(0);
            foreach (var step in new StoryBranchIterator(_choiceData.targetChapter)) {
                var stat = step.GetKind(story);
                if (stat != null) {
                    stats.AddUnique(stat);
                }
            }
            return stats.Count switch {
                0 => "Story",
                1 => stats[0].Replace("{", "").Replace("}", ""),
                _ => "Multi"
            };
        }
    }
}
