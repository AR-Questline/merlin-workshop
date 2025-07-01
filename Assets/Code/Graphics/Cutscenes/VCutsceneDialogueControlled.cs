using System;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Graphics.Cutscenes {
    public class VCutsceneDialogueControlled : VCutsceneFreeCam {
        [SerializeField] ActorToEmitterMap[] actorToEmitterMap = Array.Empty<ActorToEmitterMap>();
        [SerializeField] bool useAnimator;
        [SerializeField, ShowIf(nameof(useAnimator))] string animatorTrigger;
        [SerializeField, ShowIf(nameof(useAnimator))] Animator[] animators = Array.Empty<Animator>();
        [SerializeField] bool useTimeline;
        [SerializeField, ShowIf(nameof(useTimeline))] PlayableDirector director;
        [SerializeField, ShowIf(nameof(useTimeline))] SignalAsset[] signalsThatDontPauseTimeline = Array.Empty<SignalAsset>();

        STextModifier _sTextModifier;
        int _animatorTriggerId;
        int _currentMarker = -1;
        int _timelinePausingLockedTillFrame = -1;
        double _timelineFrameDuration;
        IMarker[] _markers;
        
        protected override void OnInitialize() {
            if (useAnimator) {
                _animatorTriggerId = Animator.StringToHash(animatorTrigger);
            }

            if (useTimeline) {
                if (director == null) {
                    Log.Critical?.Error($"Cutscene {transform.name}: Trying to use timeline, but no director is assigned to the cutscene.");
                    useTimeline = false;
                } else {
                    var timelineAsset = (TimelineAsset)director.playableAsset;
                    if (timelineAsset == null) {
                        Log.Critical?.Error($"Cutscene {transform.name}: Trying to use timeline, but no timeline asset is assigned to the director.");
                        useTimeline = false;
                    } else if (timelineAsset.markerTrack == null) {
                        Log.Critical?.Error($"Cutscene {transform.name}: Trying to use timeline, but the timeline asset has no marker track.");
                        useTimeline = false;
                    } else { 
                        _markers = timelineAsset.markerTrack.GetMarkers().OrderBy(m => m.time).ToArray();
                        _timelineFrameDuration = 1 / timelineAsset.editorSettings.frameRate;
                    }
                }
            }

            base.OnInitialize();
        }

        [UnityEngine.Scripting.Preserve]
        public void OnTimeLineMarkerReachedWithPause() {
            TryPauseTimeline();
        }

        [UnityEngine.Scripting.Preserve]
        public void OnTimeLineMarkerReachedWithoutPause() {
            
        }

        public void PerformAction(Action action, int index, bool toggle, Story story) {
            switch (action) {
                case Action.None:
                    break;
                case Action.ManualForward:
                    JumpForward();
                    break;
                case Action.JumpTo:
                    JumpTo(index);
                    break;
                case Action.End:
                    End(story);
                    break;
                case Action.ToggleSync:
                    if (_sTextModifier == null) {
                        _sTextModifier = new STextModifier(this);
                        story.AddSTextModifier(_sTextModifier);
                    }
                    _sTextModifier.syncText = toggle;
                    break;
                case Action.ToggleAutoForward:
                    if (_sTextModifier == null) {
                        _sTextModifier = new STextModifier(this);
                        story.AddSTextModifier(_sTextModifier);
                    }
                    _sTextModifier.autoForward = toggle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void JumpTo(int index) {
            if (useTimeline) {
                index = math.clamp(index, 0, _markers.Length - 1);
                _currentMarker = index;
                _timelinePausingLockedTillFrame = Time.frameCount + 2;
                
                if (director.time > _markers[_currentMarker].time) {
                   return;
                }
                
                director.time = _markers[_currentMarker].time + _timelineFrameDuration;
                director.Evaluate();
                if (director.state == PlayState.Paused) {
                    director.Resume();
                }
            }
        }

        public void JumpForward() {
            ForwardInternal(true);
        }
        
        public void AutoForward() {
            ForwardInternal(false);
        }

        void ForwardInternal(bool jump) {
            if (useAnimator) {
                foreach (var animator in animators) {
                    animator.SetTrigger(_animatorTriggerId);
                }
            }
            if (useTimeline && _currentMarker < _markers.Length - 1) {
                _currentMarker++;
                
                // If this marker is not forcing a jump, than it shouldn't force a jump when dialogue is not skipped.
                if (!jump && _markers[_currentMarker] is SignalEmitter signalEmitter) {
                    foreach (var signalAsset in signalsThatDontPauseTimeline) {
                        if (signalAsset == signalEmitter.asset) {
                            _timelinePausingLockedTillFrame = Time.frameCount + 2;
                            return;
                        }
                    }
                }
                
                JumpTo(_currentMarker);
            }
        }

        public void End(Story story) {
            StopTransition().Forget();
            if (_sTextModifier != null) {
                story.RemoveSTextModifier(_sTextModifier);
                _sTextModifier = null;
            }
        }

        protected override void ProcessUpdate(float deltaTime) { }
        
        void TryPauseTimeline() {
            if (!useTimeline) {
                return;
            }

            if (_timelinePausingLockedTillFrame >= Time.frameCount) {
                _timelinePausingLockedTillFrame = -1;
                return;
            }

            director.time = GetTimelineMarkerTime(_currentMarker + 1) - _timelineFrameDuration;
            director.Evaluate();
            director.Pause();
        }

        double GetTimelineMarkerTime(int index) {
            index = math.clamp(index, 0, _markers.Length - 1);
            return _markers[index].time;
        }
        
        [Serializable]
        struct ActorToEmitterMap {
            [SerializeField] public ActorRef actorRef;
            [SerializeField] public VoiceOversEventEmitter emitter;
        }
        
        [Serializable]
        public enum Action : byte {
            None,
            ManualForward,
            JumpTo,
            End,
            ToggleSync,
            ToggleAutoForward,
        }
        
        class STextModifier : ISTextModifer {
            readonly VCutsceneDialogueControlled _cutscene;

            public bool syncText;
            public bool autoForward;
            
            public STextModifier(VCutsceneDialogueControlled cutscene) {
                _cutscene = cutscene;
            }

            public void ModifyPreText(ref STextData sTextData) {
                if (autoForward) {
                    _cutscene.AutoForward();
                }
            }
            
            public void ModifyPostText(ref STextData sTextData) {
                if (syncText) {
                    foreach (var map in _cutscene.actorToEmitterMap) {
                        if (map.actorRef.guid == sTextData.sourceActor.Id) {
                            sTextData.emitter = map.emitter;
                            return;
                        }
                    }
                }
            }
        }
    }
}