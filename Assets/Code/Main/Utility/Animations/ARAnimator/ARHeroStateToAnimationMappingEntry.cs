using System;
using System.Collections.Generic;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using AnimationEvent = UnityEngine.AnimationEvent;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [Serializable]
    public class ARHeroStateToAnimationMappingEntry {
        // === Fields
        public HeroStateType heroStateType;
        [HideIf(nameof(IsMixerType))]
        public ClipTransition[] clipTransitions;
        [ShowIf(nameof(IsMixerType))]
        public MixerTransition2DAsset.UnShared mixerTransition;
        public bool customSpeedMultiplyCurve;
        [ShowIf(nameof(customSpeedMultiplyCurve))]
        public AnimationCurve speedMultiplyCurve = AnimationCurve.Linear(0, 1, 1, 1);

        // === Properties
        public IEnumerable<ITransition> AnimancerNodes => IsMixerType ? mixerTransition.Yield() : clipTransitions;
        bool IsMixerType => AnimancerUtils.IsMixerType(heroStateType);
        
        // === Constructors
        public ARHeroStateToAnimationMappingEntry() {
            heroStateType = HeroStateType.None;
        }
        
        public ARHeroStateToAnimationMappingEntry(HeroStateType stateType) {
            heroStateType = stateType;
        }
        
#if UNITY_EDITOR
        [HideInInspector] public ARHeroStateToAnimationMapping EDITOR_Owner;
        
        [Button, FoldoutGroup("Utils")]
        void CopyEventsFromAnimationClip() => EDITOR_CopyEventsFromAnimationClip();

        public void EDITOR_CopyEventsFromAnimationClip() {
            foreach (var clipTransition in clipTransitions) {
                AnimationClip clip = clipTransition.Clip;
                if (clip == null) {
                    continue;
                }

                AnimationEvent[] events = UnityEditor.AnimationUtility.GetAnimationEvents(clip);

                if (events.Length <= 0 || EDITOR_Owner == null) {
                    continue;
                }

                clipTransition.SerializedEvents.Names = Array.Empty<string>();
                clipTransition.SerializedEvents.NormalizedTimes = Array.Empty<float>();
                clipTransition.SerializedEvents.Callbacks = Array.Empty<IARSerializableCallbackEvent>();

                if (!clip.isLooping) {
                    ArrayUtils.Add(ref clipTransition.SerializedEvents.Names, "EndEvent");
                    ArrayUtils.Add(ref clipTransition.SerializedEvents.NormalizedTimes, 1);
                    ArrayUtils.Add(ref clipTransition.SerializedEvents.Callbacks, new ARSerializableCallbackEvent());
                }

                foreach (var animationEvent in events) {
                    float normalizedTime = animationEvent.time / clip.length;
                    int timeIndex = clipTransition.SerializedEvents.NormalizedTimes.IndexOf(normalizedTime);
                    if (timeIndex != -1) {
                        continue;
                    }

                    int index = math.max(0, clipTransition.SerializedEvents.Names.Length - 1);
                    ArrayUtils.Insert(ref clipTransition.SerializedEvents.Names, index,
                        animationEvent.objectReferenceParameter?.name ?? string.Empty);
                    ArrayUtils.Insert(ref clipTransition.SerializedEvents.NormalizedTimes, index, normalizedTime);

                    ARSerializableCallbackEvent callbackEvent = null;
                    if (animationEvent.objectReferenceParameter is ARAnimationEvent eventData) {
                        callbackEvent = new ARSerializableCallbackEvent(eventData.CreateData());
                    } else if (animationEvent.objectReferenceParameter is ARFinisherAnimationEvent finisherData) {
                        callbackEvent = new ARSerializableCallbackEvent(finisherData.arFinisherEffectsData);
                    } else if (animationEvent.objectReferenceParameter != null) {
                        Log.Critical?.Error($"Animation Event objectReference of unsupported type! In clip: {clip}", clip);
                    }
                    
                    ArrayUtils.Insert(ref clipTransition.SerializedEvents.Callbacks, index, callbackEvent);
                }
            }
        }

        [Button, FoldoutGroup("Utils")]
        void RemoveAnimationEvents() => EDITOR_RemoveAnimationEvents();
        
        public void EDITOR_RemoveAnimationEvents() {
            foreach (var clipTransition in clipTransitions) {
                clipTransition.SerializedEvents.Names = Array.Empty<string>();
                clipTransition.SerializedEvents.NormalizedTimes = Array.Empty<float>();
                clipTransition.SerializedEvents.Callbacks = Array.Empty<IARSerializableCallbackEvent>();
            }
        }
#endif
    }
}