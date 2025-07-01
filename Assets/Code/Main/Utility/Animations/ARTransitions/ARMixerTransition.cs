using System;
using Animancer;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    [Serializable]
    public class ARMixerTransition : MixerTransition2D, ICopyable<ARMixerTransition> {
        public const float DefaultVelocityUpdateSpeed = 2f;
        public const float DefaultTurningUpdateSpeed = 5f;
        
        public enum MovementType {
            Strafing,
            Turning
        }

        [SerializeField] ARTurningAnimationOverrides turningOverrides;
        [SerializeField] MovementType movementType;
        [SerializeField] float velocityUpdateSpeed = DefaultVelocityUpdateSpeed;
        [SerializeField] float turningUpdateSpeed = DefaultTurningUpdateSpeed;
        
        public ARTurningAnimationOverrides TurningOverrides => turningOverrides;
        
        
        public override MixerState<Vector2> CreateState() {
            var properties = new Properties {
                movementType = movementType,
                turningOverrides = turningOverrides,
                velocityUpdateSpeed = velocityUpdateSpeed,
                turningUpdateSpeed = turningUpdateSpeed,
            };
            
            State = Type switch {
                MixerType.Cartesian => new CartesianARMixerState(properties),
                MixerType.Directional => new DirectionalARMixerState(properties),
                _ => throw new ArgumentOutOfRangeException(nameof(Type))
            };
            InitializeState();
            return State;
        }
        
        public virtual void CopyFrom(ARMixerTransition copyFrom)
        {
            CopyFrom((MixerTransition2D)copyFrom);
            movementType = copyFrom?.movementType ?? default;
        }

        [Serializable]
        public struct Properties {
            public MovementType movementType;
            public ARTurningAnimationOverrides turningOverrides;
            public float velocityUpdateSpeed;
            public float turningUpdateSpeed;
        }
        
#if UNITY_EDITOR
        [UnityEditor.CustomPropertyDrawer(typeof(ARMixerTransition), true)]
        public new class Drawer : MixerTransitionDrawer {
            public Drawer() : base(StandardThresholdWidth * 2 + 20) { }
            
            public override void OnGUI(Rect area, UnityEditor.SerializedProperty property, GUIContent label) {
                base.OnGUI(area, property, label);
                
                if (GUILayout.Button("Draw Simplified")) {
                    UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", true);
                }
                
                if (GUILayout.Button("Draw Full")) {
                    UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", false);
                }

                if (GUILayout.Button("Resample Root Rotation Deltas")) {
                    ResampleOverridesRootRotationDeltas(property);
                }
            }
            
            static void ResampleOverridesRootRotationDeltas(UnityEditor.SerializedProperty property) {
                if (property.serializedObject.targetObject is not MixerTransition2DAsset asset) {
                    return;
                }
                
                if (asset.Transition is not ARMixerTransition transition) {
                    return;
                }
                
                foreach (var turningOverride in transition.turningOverrides.entries) {
                    turningOverride.clip.SampleRootRotationDelta();
                }
                
                UnityEditor.EditorUtility.SetDirty(asset);
            }
        }
#endif
    }
}