using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.States.Rotation;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class VCAnimationRiggingHandler {
        class DialogueAnimationRigging : AnimationRigging {
            const float MinAngleToStartFullRotation = 5f;
            
            bool _isInDialogue;
            bool _isLeavingDialogue;
            LookAtChangedData? _onStoryStartLookAtPosition = null;
            StoryInteractionToggleData _dialogueSettings;
            
            bool _fullRotation;
            IEventListener _lookAtMovedListener;

            public bool Active => _isInDialogue;
            GroundedPosition DefaultLookAt => GroundedPosition.ByPosition(NpcElement.Coords + NpcElement.Forward());

            protected override void OnInit() {
                Location.ListenTo(StoryInteraction.Events.StoryInteractionToggled, OnDialogueToggled, Handler);
                Location.ListenTo(StoryInteraction.Events.LocationLookAtChanged, OnLookAtChanged, Handler);
                NpcElement.ListenTo(NpcRotate.Events.RotationStopped, OnRotationStopped, Handler);

                SetLookAt(DefaultLookAt);
                
                _data.rootRigDesiredWeight = 0;
                _data.headRigDesiredWeight = 0;
                _data.bodyRigDesiredWeight = 0;
                _data.combatRigDesiredWeight = 0;
                _data.attackRigDesiredWeight = 0;
                
                _data.headTurnSpeed = DefaultHeadRigUpdateSpeed * 1.5f;
                _data.bodyTurnSpeed = DefaultBodyRigUpdateSpeed;
                _data.rootTurnSpeed = DefaultRootRigUpdateSpeed;
                _data.combatTurnSpeed = DefaultCombatRigUpdateSpeed;
                _data.attackTurnSpeed = DefaultCombatRigUpdateSpeed;
            }

            void OnDialogueToggled(StoryInteractionToggleData data) {
                if (data.IsEntering) {
                    if (!_isInDialogue || _isLeavingDialogue) {
                        Handler.CancelGlancing();
                        Start(data);
                    }
                } else if (_isInDialogue) {
                    Stop(data);
                }
            }

            void OnRotationStopped(bool aborted) {
                if (!_isInDialogue) {
                    return;
                }
                
                if (aborted) {
                    _isInDialogue = false;
                    _isLeavingDialogue = false;
                    SetLookAt(DefaultLookAt);
                    return;
                }

                if (_fullRotation) {
                    FinalizeFullRotationTowardsLookAt();
                }
            }

            void Start(StoryInteractionToggleData data) {
                _isInDialogue = true;
                _data.spineRotationType = data.RotationType;
                SetLookAt(_onStoryStartLookAtPosition?.groundedPosition ?? DefaultLookAt);
                

                _fullRotation = data.RotationType is SpineRotationType.FullRotation;
                SetupDefaultInteractionRigWeights(_data.spineRotationType);

                if (_onStoryStartLookAtPosition?.lookAtOnlyWithHead ?? false) {
                    _data.headRigDesiredWeight = 1;
                } else if (_fullRotation) {
                    FullyRotateTowardsLookAt(true);
                }

                _onStoryStartLookAtPosition = null;
            }

            void Stop(StoryInteractionToggleData data) {
                _isLeavingDialogue = true;
                _data.headRigDesiredWeight = 0;
                _data.bodyRigDesiredWeight = 0;
                _data.rootRigDesiredWeight = 0;
                if (data.InstantExit) {
                    Handler._headRigWeight = 0;
                    Handler._bodyRigWeight = 0;
                    Handler._rootRigWeight = 0;
                } else {
                    _data.rootTurnSpeed = DefaultRootRigUpdateSpeed;
                }
                
                _isInDialogue = false;
                _isLeavingDialogue = false;
                SetLookAt(DefaultLookAt);


                if (_fullRotation) {
                    NpcElement.Movement.StopInterrupting();
                }
                
                World.EventSystem.TryDisposeListener(ref _lookAtMovedListener);
            }

            void OnLookAtChanged(LookAtChangedData lookAtData) {
                var groundedPosition = lookAtData.groundedPosition;
                if (!_isInDialogue) {
                    _onStoryStartLookAtPosition = lookAtData;
                    Log.Minor?.Info($"Trying to change look at position while not in dialogue!\n" +
                                       $"If not overriden during dialogue, it will be used at involve!\n" +
                                       $"{NpcElement.Name} look at {groundedPosition?.DebugName() ?? "null"}");
                    return;
                }
                
                SetLookAt(groundedPosition);
                if (lookAtData.lookAtOnlyWithHead) {
                    _data.headRigDesiredWeight = 1;
                } else if (_fullRotation) {
                    FullyRotateTowardsLookAt(true);
                } else {
                    SetupDefaultInteractionRigWeights(_data.spineRotationType);
                }
            }

            void SetLookAt(GroundedPosition lookAt) {
                _data.lookAt = lookAt;

                World.EventSystem.TryDisposeListener(ref _lookAtMovedListener);
                
                if (lookAt?.Grounded is { } lookAtGrounded) {
                    _lookAtMovedListener = lookAtGrounded.ListenTo(GroundedEvents.AfterMovedToPosition, OnLookAtMoved, Handler);
                }
            }
            
            void OnLookAtMoved(Vector3 lookAtPosition) {
                if (!_isInDialogue || !_fullRotation) {
                    return;
                }
                
                var targetLookDirection = (_data.lookAt.Coords - NpcElement.Coords).ToHorizontal2();
                var currentLookDirection = NpcElement.Forward().ToHorizontal2();

                float angle = Vector2.Angle(targetLookDirection, currentLookDirection);
                
                if (angle >= MinAngleToStartFullRotation) {
                    FullyRotateTowardsLookAt(false);
                }
            }

            void FullyRotateTowardsLookAt(bool interruptRotationState) {
                var lookDirection = _data.lookAt.Coords - NpcElement.Coords;
                var talkingState = new SnapToPositionAndRotate(NpcElement.Coords, lookDirection, null);
                NpcElement.Movement.InterruptState(talkingState);

                if (!interruptRotationState && NpcRotate.IsInRotation(NpcElement)) {
                    return;
                }

                bool forceRotation = !interruptRotationState;
                bool isRotating = NpcRotate.TryEnterRotationState(NpcElement, lookDirection, forceRotation);
                if (isRotating) {
                    _data.headRigDesiredWeight = 0;
                    _data.bodyRigDesiredWeight = 0;
                } else {
                    FinalizeFullRotationTowardsLookAt();
                }
            }

            void SetupDefaultInteractionRigWeights(SpineRotationType rotationType) {
                _data.headRigDesiredWeight = rotationType switch {
                    SpineRotationType.UpperBody => 1,
                    SpineRotationType.HeadOnly => 1,
                    _ => 0
                };
                _data.bodyRigDesiredWeight = rotationType switch {
                    SpineRotationType.UpperBody => 1,
                    _ => 0
                };
            }
            void FinalizeFullRotationTowardsLookAt() {
                _data.headRigDesiredWeight = 0;
                _data.bodyRigDesiredWeight = 0;
            }
            
            public override void Dispose() {
                World.EventSystem.TryDisposeListener(ref _lookAtMovedListener);
                _onStoryStartLookAtPosition = null;
            }
        }
    }
}