using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Text: Play Gesture")]
    public class SEditorPlayGesture : EditorStep {
        [HideLabel]
        public ActorRef actorRef;
        public string gestureKey;
        public bool waitForEnd = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayGesture {
                actorRef = actorRef,
                gestureKey = gestureKey,
                waitForEnd = waitForEnd
            };
        }
    }

    public partial class SPlayGesture : StoryStep {
        public ActorRef actorRef;
        public string gestureKey;
        public bool waitForEnd;
        
        public override StepResult Execute(Story story) {
            if (waitForEnd) {
                StepResult result = new();
                Execute(story, result);
                return result;
            } else {
                Execute(story, null);
                return StepResult.Immediate;
            }
        }

        void Execute(Story api, StepResult result) {
            // Who is gesticulating
            Actor sourceActor = actorRef.Get();
            IWithActor withActor = StoryUtils.FindIWithActor(api, sourceActor);
            NpcElement speaker = withActor as NpcElement;
            Transform speakerTransform = withActor?.ActorTransform;
            // Gather Components
            ARNpcAnimancer arNpcAnimancer = null;
            if (speakerTransform != null) {
                arNpcAnimancer = speakerTransform.GetComponentInChildren<ARNpcAnimancer>();
            }
            // Gesticulate
            string gesture = gestureKey;
            StoryAnimationData storyAnimationData = TryGetGesture(speaker, gesture, arNpcAnimancer);
            StoryUtils.CompleteWhenGestureEnds(api, result, storyAnimationData).Forget();
        }

        public static StoryAnimationData TryGetGesture(NpcElement speaker, string gestureKey, ARNpcAnimancer arNpcAnimancer) {
            if (speaker == null || speaker.IsInCombat() || (speaker.NpcAI?.InAlert ?? false)) {
                return null;
            }
            bool hasGestureAndAnimator = arNpcAnimancer != null && !string.IsNullOrEmpty(gestureKey);
            if (!hasGestureAndAnimator) {
                if (speaker.Interactor.CurrentInteraction is not TalkInteraction) {
                    return null;
                }
                return new StoryAnimationData { npcElement = speaker };
            }
            
            if (arNpcAnimancer == null || string.IsNullOrEmpty(gestureKey)) {
                return null;
            }

            Gender gender = speaker.GetGender();
            AnimationClip clip = speaker.InteractionGestures?.TryToGetGestureOverrideClip(gestureKey) ?? 
                                 speaker.GesturesWrapper?.TryToGetGestureOverrideClip(gestureKey) ??
                                 GesturesSerializedWrapper.TryToGetDefaultGesture(gestureKey, gender);
            
            if (clip != null) {
                var data = new StoryAnimationData {
                    npcElement = speaker,
                    arNpcAnimancer =  arNpcAnimancer,
                    animationClip = clip
                };
                
                return data;
            }

            return null;
        }
    }
}