using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;

namespace Awaken.TG.Editor.Localizations {
    public abstract class ElementExtractor {
        static readonly ElementExtractor[] Extractors = {
            new TextExtractor(),
            new ChoiceExtractor(),
            new StoryStartChoiceExtractor(),
            new NodeJumpExtractor(),
            new GraphJumpExtractor(),
            new RandomPickExtractor(),
            new ChangeItemsQuantityExtractor(),
            new StatDependentExtractor(),
            new OpenGemsUIExtractor(),
            new LocationUnobservedExtractor(),
            new PerformInteractionExtractor()
        };
        static readonly ElementExtractor DefaultExtractor = new DefaultExtractor();
        public static ElementExtractor GetFor(Type type) => Extractors.FirstOrDefault(e => e.NodeType.IsAssignableFrom(type)) ?? DefaultExtractor;

        protected abstract Type NodeType { get; }
        public virtual IEnumerable<ScriptEntry> GetTexts(NodeElement element) => Enumerable.Empty<ScriptEntry>();
        public virtual IEnumerable<StoryNode> GetContinuations(NodeElement element, ScriptType type) => Enumerable.Empty<StoryNode>();
    }

    abstract class ElementExtractor<T> : ElementExtractor where T : NodeElement {
        protected override Type NodeType => typeof(T);

        public sealed override IEnumerable<ScriptEntry> GetTexts(NodeElement element) => GetTexts((T) element);
        protected virtual IEnumerable<ScriptEntry> GetTexts(T element) => Enumerable.Empty<ScriptEntry>();

        public sealed override IEnumerable<StoryNode> GetContinuations(NodeElement element, ScriptType type) => GetContinuations((T) element, type);
        protected virtual IEnumerable<StoryNode> GetContinuations(T element, ScriptType type) => Enumerable.Empty<StoryNode>();
    }
    
    class DefaultExtractor : ElementExtractor<NodeElement> { }

    class TextExtractor : ElementExtractor<SEditorText> {
        protected override IEnumerable<ScriptEntry> GetTexts(SEditorText editorText) {
            string actorName = ActorsRegister.Get.Editor_GetActorName(editorText.actorRef.guid);
            yield return new ScriptEntry(editorText.text.ID, editorText.text.ToString(), "Text", actorName);
        }
    }
    
    class ChoiceExtractor : ElementExtractor<SEditorChoice> {
        protected override IEnumerable<ScriptEntry> GetTexts(SEditorChoice choice) {
            if (choice is SEditorChoicesExit {hiddenFromPlayer: true}) {
                yield break;
            }
            LocString term = choice.choice.text;
            yield return new ScriptEntry(term.ID, term.ToString(), "Choice", "Hero");
        }

        protected override IEnumerable<StoryNode> GetContinuations(SEditorChoice choice, ScriptType type) {
            yield return choice.TargetNode() as StoryNode;
        }
    }
    
    class StoryStartChoiceExtractor : ElementExtractor<SEditorStoryStartChoice> {
        protected override IEnumerable<ScriptEntry> GetTexts(SEditorStoryStartChoice choice) {
            LocString term = choice.choice.text;
            yield return new ScriptEntry(term.ID, term.ToString(), "Choice", "Hero");
        }

        protected override IEnumerable<StoryNode> GetContinuations(SEditorStoryStartChoice choice, ScriptType type) {
            yield return choice.TargetNode() as StoryNode;
        }
    }

    class NodeJumpExtractor : ElementExtractor<SEditorNodeJump> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorNodeJump jump, ScriptType type) {
            yield return jump.TargetNode() as StoryNode;
        }
    }

    class GraphJumpExtractor : ElementExtractor<SEditorGraphJump> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorGraphJump jump, ScriptType type) {
            StoryNode node = null;
            try {
                node = jump.bookmark.EDITOR_Chapter as StoryNode;
                if (type == ScriptType.VoiceActors && node?.graph != jump.Parent.graph) {
                    node = null;
                }
            } catch {
                // ignore
            }
            yield return node;
        }
    }
    
    class StatDependentExtractor : ElementExtractor<SEditorStatDependantChoice> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorStatDependantChoice editorStatDependent, ScriptType type) {
            yield return editorStatDependent.SuccessChapter;
        }
    }
    
    class RandomPickExtractor : ElementExtractor<SEditorRandomPick> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorRandomPick element, ScriptType type) {
            yield return element.TargetNode() as StoryNode;
        }
    }

    class ChangeItemsQuantityExtractor : ElementExtractor<SEditorChangeItemsQuantity> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorChangeItemsQuantity element, ScriptType type) {
            yield return element.TargetNode() as StoryNode;
        }
    }
    
    class OpenGemsUIExtractor : ElementExtractor<SEditorOpenGemsUI> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorOpenGemsUI element, ScriptType type) {
            yield return element.TargetNode() as StoryNode;
        }
    }
    
    class LocationUnobservedExtractor : ElementExtractor<SEditorLocationRunUnobserved> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorLocationRunUnobserved element, ScriptType type) {
            yield return element.targetStory.EDITOR_Chapter as StoryNode;
        }
    }
    
    class PerformInteractionExtractor : ElementExtractor<SEditorPerformInteraction> {
        protected override IEnumerable<StoryNode> GetContinuations(SEditorPerformInteraction element, ScriptType type) {
            if (element.callback?.IsValid ?? false) {
                yield return element.callback.EDITOR_Chapter as StoryNode;
            }
        }
    }
}