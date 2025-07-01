using System;
using System.Collections.Generic;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility.Debugging;
using CrazyMinnow.SALSA;
using Cysharp.Threading.Tasks;
using FMOD;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using EventReference = FMODUnity.EventReference;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Text: Show")]
    public class SEditorText : EditorStep, IStoryActorRef, IStoryTextRef {
        public const int MaxCharsPerLine = 180;

        // === Serialized fields
        [HideLabel, ActorAsTalker]
        public ActorRef actorRef;
        [HideLabel, ActorAsListener]
        public ActorRef targetActorRef = DefinedActor.Hero.ActorRef;
        public bool lookAtOnlyWithHead;

        [Tooltip("This text is displayed in Story. Usable in-text tags: '{{hero:name}}' '{{hero:master}}' '{{hero:pronoun}}' '{{location:name}}' '{[variable]}'")]
        [TextArea(1, 200)]
        public LocString text;
        [HideIf(nameof(lookAtOnlyWithHead))]
        public string gestureKey;
        public EventReference audioClip;
        public bool waitForInput = true;
        public bool hasVoice = true;
        public bool overrideDuration;
        [ShowIf(nameof(overrideDuration))]
        public int cutDurationMilliseconds = 250;
        [LabelText("Comment"), TextArea(1,20)]
        public string commentInfo;
        // editor only field
        public int textLength;

        public bool HasAudioClip => hasVoice && !audioClip.Guid.IsNull && false;//RuntimeManager.StudioSystem.getEventByID(audioClip.Guid, out _) == RESULT.OK;
        public GUID AudioClipEventGUID => audioClip.Guid;
        public List<EmotionData> emotions;

        public ActorRef[] ActorRef => new[] { actorRef, targetActorRef };
        public LocString Text => text;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SText {
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

    public partial class SText : StoryStep {
        public ActorRef actorRef;
        public ActorRef targetActorRef = DefinedActor.Hero.ActorRef;
        public bool lookAtOnlyWithHead;

        public LocString text;
        public string gestureKey;
        public EventReference audioClip;
        public bool hasVoice = true;
        public bool overrideDuration;
        public int cutDurationMilliseconds = 250;
        
        public EmotionData[] emotions;

        EmotionData[] _inlineEmotions;

        public bool HasAudioClip => hasVoice && !audioClip.Guid.IsNull && false;//RuntimeManager.StudioSystem.getEventByID(audioClip.Guid, out _) == RESULT.OK;
        
        public override StepResult Execute(Story story) {
            StepResult result = new();
            AsyncExecute(story, result).Forget();
            return result;
        }

        public virtual TextConfig PrepareTextConfig(IWithActor withActor, Actor sourceActor) {
            return TextConfig.WithTextAndStyle(text, StoryTextStyle.NpcDialogue)
                .ByLocation((withActor as ILocationElementWithActor)?.ParentModel, sourceActor);
        }
        
        async UniTaskVoid AsyncExecute(Story api, StepResult result) {
            STextData data = new() {
                sourceActor = actorRef.Get(),
                targetActor = targetActorRef.Get()
            };
            
            // Who is speaking
            IWithActor withActor = StoryUtils.FindIWithActor(api, data.sourceActor);
            NpcElement speaker = withActor as NpcElement;

            // Who is listening
            IGrounded listener = StoryUtils.FindIGrounded(api, data.targetActor, allowDefault: false);
            // Rotate to target
            if (speaker != null && listener != null) {
                UniTask rotateTask = SNpcLookAt.NpcLookAt(speaker, GroundedPosition.ByGrounded(listener), api.InvolveHero, lookAtOnlyWithHead);
                if (listener is NpcElement npc) {
                    SNpcLookAt.NpcLookAt(npc, GroundedPosition.ByGrounded(speaker), api.InvolveHero, lookAtOnlyWithHead).Forget();
                }
                if (CanWait(api)) {
                    await rotateTask; // If player initiated the same story while this async runs, story will no longer exist after
                    if (api.HasBeenDiscarded) return; 
                }
            }
            
            // Modify
            if (api.STextModifiers.Count > 0) {
                for (int i = 0; i < api.STextModifiers.Count; i++) {
                    api.STextModifiers[i].ModifyPreText(ref data);
                }
            }
            
            // Define text content and show it 
            var textConfig = PrepareTextConfig(withActor, data.sourceActor);
            api.ShowText(textConfig);
            
            // Gather Components
            ARNpcAnimancer arNpcAnimancer = speaker?.Controller.ARNpcAnimancer;
            data.emitter = speaker?.VoiceOversEmitter;
            
            // Modify
            if (api.STextModifiers.Count > 0) {
                for (int i = 0; i < api.STextModifiers.Count; i++) {
                    api.STextModifiers[i].ModifyPostText(ref data);
                }
            }

            // Gesticulate
            string gesture = gestureKey;
            if (string.IsNullOrWhiteSpace(gesture)) {
                gesture = text.GetSharedMetadata<GestureMetadata>()?.GestureKey ?? string.Empty;
            }
            StoryAnimationData storyAnimationData = lookAtOnlyWithHead ? null : SPlayGesture.TryGetGesture(speaker, gesture, arNpcAnimancer);
            if (api is Story story) {
                // var (waitedFramesCount, loadedAll) = await FmodRuntimeManagerUtils.WaitForBanksFinishLoading(story.LoadingBanks, Story.MaxFramesToWaitForVOBanksLoad);
                // if (loadedAll == false) {
                //     Log.Important?.Error($"Loading of VO banks for story graph {story.Guid} took too long, more than {Story.MaxFramesToWaitForVOBanksLoad} frames.");
                // }
                // Log.Debug?.Info($"Loading of VO banks for story graph {story.Guid} took {waitedFramesCount} frames.");
            }

            var hasAudioClip = HasAudioClip;
            // Play VO
            if (hasAudioClip && data.emitter != null) {
                data.emitter.Speak(audioClip, ExtractEmotions(textConfig)).Forget();
            } else if (hasAudioClip && api.Hero?.NonSpatialVoiceOvers != null) {
                //api.Hero.NonSpatialVoiceOvers.EventEmitter.ChangeEvent(audioClip);
            }

            // Wait for end
            bool canWait = CanWait(api);
            if (!canWait) {
                result.Complete();
                return;
            }

            if (hasAudioClip) {
                int? cutDuration = overrideDuration ? cutDurationMilliseconds : null;
                if (data.emitter != null) {
                    StoryUtils.WaitDialogueLine(result, api, text, data.emitter, null, storyAnimationData, cutDuration).Forget();
                } else {
                    StoryUtils.WaitDialogueLine(result, api, text, null, api.Hero?.NonSpatialVoiceOvers, null, cutDuration).Forget();
                }
            } else {
                StoryUtils.CompleteWhenReadOrInput(api, result, text, storyAnimationData).Forget();
            }
        }

        EmotionData[] ExtractEmotions(TextConfig textConfig) {
            // predefined emotions have higher priority than inlineEmotions
            if (emotions.Length > 0) {
                return emotions;
            }

            if (string.IsNullOrEmpty(textConfig.EmoteKey)) {
                return Array.Empty<EmotionData>();
            }
            
            _inlineEmotions ??= new EmotionData[1];
            _inlineEmotions[0] = new EmotionData {
                startTime = 0, 
                roundDuration = 0, 
                emotionKey = textConfig.EmoteKey, 
                state = EmotionState.Enable,
                expressionHandler = ExpressionComponent.ExpressionHandler.RoundTrip
            };
            return _inlineEmotions;
        }

        bool CanWait(Story api) => api.MainView is VDialogue or VBark or VStoryPanel && !DebugReferences.FastStory && !DebugReferences.ImmediateStory;
    }

    public interface ISTextModifer {
        public void ModifyPreText(ref STextData data);
        public void ModifyPostText(ref STextData data);
    }

    public struct STextData {
        public Actor sourceActor;
        public Actor targetActor;
        public VoiceOversEventEmitter emitter;
    }
}