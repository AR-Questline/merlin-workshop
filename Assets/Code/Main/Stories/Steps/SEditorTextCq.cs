using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Conquest Dialogue")]
    public class SEditorTextCq : SEditorText {
        [ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.UI)]
        public ShareableSpriteReference iconRef;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STextCq {
                iconRef = iconRef,
                // --- SText
                actorRef = actorRef,
                targetActorRef = targetActorRef,
                lookAtOnlyWithHead = lookAtOnlyWithHead,
                text = text,
                gestureKey = gestureKey,
                audioClip = audioClip,
                hasVoice = hasVoice,
                overrideDuration = overrideDuration,
                cutDurationMilliseconds = cutDurationMilliseconds,
                emotions = emotions.ToArray(),
            };
        }
    }

    public partial class STextCq : SText {
        public ShareableSpriteReference iconRef;

        public override TextConfig PrepareTextConfig(IWithActor withActor, Actor sourceActor) {
            return TextConfig.WithEverything(text, StoryTextStyle.NpcDialogue, iconRef);
        }
    }
}