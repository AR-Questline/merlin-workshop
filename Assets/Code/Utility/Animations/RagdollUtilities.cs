using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Animations {
    public class RagdollUtilities : MonoBehaviour {
        static IEnumerable<Transform> RagdollBones(Transform parent) => parent.GetComponentsInChildren<Transform>()
            .Where(t => t.gameObject.layer == RenderLayers.Ragdolls);

        public static void RemoveRagdoll(Transform transform) {
            foreach (var bone in RagdollBones(transform)) {
                if (Application.isPlaying) {
                    Object.Destroy(bone.GetComponent<ConfigurableJoint>());
                    Object.Destroy(bone.GetComponent<CharacterJoint>());
                    Object.Destroy(bone.GetComponent<Rigidbody>());
                    Object.Destroy(bone.GetComponent<Collider>());
                } else {
                    Object.DestroyImmediate(bone.GetComponent<ConfigurableJoint>());
                    Object.DestroyImmediate(bone.GetComponent<CharacterJoint>());
                    Object.DestroyImmediate(bone.GetComponent<Rigidbody>());
                    Object.DestroyImmediate(bone.GetComponent<Collider>());
                }
            }
        }

        [Title("Drag Setup"), Button]
        public void SetupDrag(float linearDrag = 0.5f, float angularDrag = 1.25f) {
            foreach (var ragdollBone in RagdollBones(transform)) {
                var rb = ragdollBone.GetComponent<Rigidbody>();
                if (rb == null) {
                    continue;
                }
                
                rb.linearDamping = linearDrag;
                rb.angularDamping = angularDrag;
            }
        }

        [Title("Removing"), Button("RemoveRagdoll")]
        public void RemoveRagdollFromChild() {
            RemoveRagdoll(transform);
        }

        [Title("Copying"), Button("Copy Ragdoll From To")]
        public void CopyRagdoll(Transform from, Transform to, bool removeRagdollFromOriginal = true) {
            Rigidbody rigidbody;
            Collider ragdollCollider;
            CharacterJoint characterJoint;
            ConfigurableJoint configurableJoint;
            Dictionary<string, RagdollData> _ragdollData = new();
            // --- Gather data
            foreach (var bone in RagdollBones(from)) {
                var ragdollData = new RagdollData();
                rigidbody = bone.GetComponent<Rigidbody>();
                ragdollCollider = bone.GetComponent<Collider>();
                characterJoint = bone.GetComponent<CharacterJoint>();
                configurableJoint = bone.GetComponent<ConfigurableJoint>();
                // --- Cache Data
                ragdollData.Save(rigidbody, ragdollCollider, characterJoint, configurableJoint);
                _ragdollData[bone.name] = ragdollData;
                // --- Remove components
                if (removeRagdollFromOriginal) {
                    Object.Destroy(configurableJoint);
                    Object.Destroy(characterJoint);
                    Object.Destroy(rigidbody);
                    Object.Destroy(ragdollCollider);
                }
            }

            // --- Copy data to new parent
            foreach (Transform bone in RagdollBones(to)) {
                if (_ragdollData.TryGetValue(bone.name, out RagdollData config)) {
                    config.CopyTo(bone);
                }
            }
        }

        enum ColliderType : byte {
            Capsule = 0,
            Box = 1,
            Sphere = 2,
            None = 3
        }

        public struct RagdollData {
            const float MaxLinearVelocity = 200f; // default is 1e+16
            const float MaxAngularVelocity = 5f; // default is 7
            const float MaxDepenetrationVelocity = 2f; // default is 10
            
            // --- rigidbody
            RigidbodyData _rigidbodyData;
            // --- Character Joint
            CharacterJointData _characterJointData;
            // --- Configurable Joint
            ConfigurableJointData _configurableJointData;

            // --- Collider
            half3 _colliderCenter;
            half3 _colliderData;
            ColliderType _colliderType;

            public void Save(Rigidbody rb, Collider collider, CharacterJoint joint, ConfigurableJoint configurableJoint) {
                
                // --- rigidbody data
                if (rb != null) {
                    _rigidbodyData = new RigidbodyData {
                        drag = rb.linearDamping,
                        mass = rb.mass,
                        angularDrag = rb.angularDamping,
                        constraints = (RigidbodyData.RigidbodyConstraints)(byte)(int)rb.constraints,
                        interpolation = (RigidbodyData.RigidbodyInterpolation)(byte)(int)rb.interpolation,
                        collisionDetectionMode = (RigidbodyData.CollisionDetectionMode)(byte)(int)rb.collisionDetectionMode,
                        detectCollisions = rb.detectCollisions
                    };
                }

                // --- Collider data
                _colliderType = ColliderType.None;
                if (collider is CapsuleCollider capsuleCollider) {
                    _colliderType = ColliderType.Capsule;
                    _colliderCenter = new half3(capsuleCollider.center);
                    _colliderData = new half3((half)capsuleCollider.radius, (half)capsuleCollider.height, (half)capsuleCollider.direction);
                } else if (collider is BoxCollider boxCollider) {
                    _colliderType = ColliderType.Box;
                    _colliderCenter = new half3(boxCollider.center);
                    _colliderData = new half3(boxCollider.size);
                } else if (collider is SphereCollider sphereCollider) {
                    _colliderType = ColliderType.Sphere;
                    _colliderCenter = new half3(sphereCollider.center);
                    _colliderData = new half3((half)sphereCollider.radius, (half)0, (half)0);
                }

                // --- joint data
                if (joint != null) {
                    _characterJointData = new CharacterJointData() {
                        parentBoneName = joint.connectedBody?.name ?? string.Empty,
                        anchor = new half3(joint.anchor),
                        axis = new half3(joint.axis),
                        autoConfigureConnectedAnchor = joint.autoConfigureConnectedAnchor,
                        connectedAnchor = new half3(joint.connectedAnchor),
                        swingAxis = new half3(joint.swingAxis),
                        twistLimitSpring = joint.twistLimitSpring,
                        highTwistLimit = joint.highTwistLimit,
                        lowTwistLimit = joint.lowTwistLimit,
                        swingLimitSpring = joint.swingLimitSpring,
                        swing1Limit = joint.swing1Limit,
                        swing2Limit = joint.swing2Limit,
                        enableProjection = joint.enableProjection,
                        projectionDistance = new half(joint.projectionDistance),
                        projectionAngle = new half(joint.projectionAngle),
                        breakForce =  joint.breakForce,
                        breakTorque =  joint.breakTorque,
                        enableCollision = joint.enableCollision,
                        enablePreprocessing = joint.enablePreprocessing,
                        massScale = new half(joint.massScale),
                        connectedMassScale = new half(joint.connectedMassScale),
                    };
                }

                // --- configurable joint data
                if (configurableJoint != null) {
                    _configurableJointData = new ConfigurableJointData() {
                        connectedBodyName = configurableJoint.connectedBody?.name ?? string.Empty,
                        articulationBodyName = configurableJoint.connectedArticulationBody?.name ?? string.Empty,
                        anchor = new half3(configurableJoint.anchor),
                        axis = new half3(configurableJoint.axis),
                        autoConfigureConnectedAnchor = configurableJoint.autoConfigureConnectedAnchor,
                        connectedAnchor = new half3(configurableJoint.connectedAnchor),
                        secondaryAxis = new half3(configurableJoint.secondaryAxis),
                        xMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.xMotion,
                        yMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.yMotion,
                        zMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.zMotion,
                        angularXMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.angularXMotion,
                        angularYMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.angularYMotion,
                        angularZMotion = (ConfigurableJointData.ConfigurableJointMotion)(byte)(int)configurableJoint.angularZMotion,
                        linearLimitSpring = configurableJoint.linearLimitSpring,
                        linearLimit = configurableJoint.linearLimit,
                        angularXLimitSpring = configurableJoint.angularXLimitSpring,
                        lowAngularXLimit = configurableJoint.lowAngularXLimit,
                        highAngularXLimit = configurableJoint.highAngularXLimit,
                        angularYZLimitSpring = configurableJoint.angularYZLimitSpring,
                        angularYLimit = configurableJoint.angularYLimit,
                        angularZLimit = configurableJoint.angularZLimit,
                        xDrive = configurableJoint.xDrive,
                        yDrive = configurableJoint.yDrive,
                        zDrive = configurableJoint.zDrive,
                        targetRotation = configurableJoint.targetRotation,
                        targetAngularVelocity = new half3(configurableJoint.targetAngularVelocity),
                        rotationDriveMode = configurableJoint.rotationDriveMode,
                        angularXDrive = configurableJoint.angularXDrive,
                        angularYZDrive = configurableJoint.angularYZDrive,
                        slerpDrive = configurableJoint.slerpDrive,
                        projectionMode = configurableJoint.projectionMode,
                        projectionDistance = configurableJoint.projectionDistance,
                        projectionAngle = configurableJoint.projectionAngle,
                        configuredInWorldSpace = configurableJoint.configuredInWorldSpace,
                        swapBodies = configurableJoint.swapBodies,
                        breakForce = configurableJoint.breakForce,
                        breakTorque = configurableJoint.breakTorque,
                        enableCollision = configurableJoint.enableCollision,
                        enablePreProcessing = configurableJoint.enablePreprocessing,
                        massScale = configurableJoint.massScale,
                        connectedMassScale = configurableJoint.connectedMassScale,
                    };
                }
            }

            public void CopyTo(Transform t, Action<Rigidbody> additionalRigidbodySetup = null) {
                if (_rigidbodyData != null) {
                    var rigidbody = t.gameObject.AddComponent<Rigidbody>();
                    rigidbody.isKinematic = false;
                    rigidbody.constraints = (RigidbodyConstraints)(byte)_rigidbodyData.constraints;
                    rigidbody.linearDamping = _rigidbodyData.drag;
                    rigidbody.interpolation = (RigidbodyInterpolation)(byte)_rigidbodyData.interpolation;
                    rigidbody.mass = _rigidbodyData.mass;
                    rigidbody.angularDamping = _rigidbodyData.angularDrag;
                    rigidbody.detectCollisions = _rigidbodyData.detectCollisions;
                    rigidbody.collisionDetectionMode = (CollisionDetectionMode)(byte)_rigidbodyData.collisionDetectionMode;
                    rigidbody.maxLinearVelocity = MaxLinearVelocity;
                    rigidbody.maxAngularVelocity = MaxAngularVelocity;
                    rigidbody.maxDepenetrationVelocity = MaxDepenetrationVelocity;
                    additionalRigidbodySetup?.Invoke(rigidbody);
                }

                Collider collider = t.GetComponent<Collider>();
                if (collider != null) {
                    collider.isTrigger = false;
                } else {
                    switch (_colliderType) {
                        case ColliderType.Capsule:
                            CapsuleCollider capsuleCollider = t.gameObject.AddComponent<CapsuleCollider>();
                            capsuleCollider.center = (float3)_colliderCenter;
                            capsuleCollider.radius = _colliderData.x;
                            capsuleCollider.height = _colliderData.y;
                            capsuleCollider.direction = (int)_colliderData.z;
                            capsuleCollider.isTrigger = false;
                            break;
                        case ColliderType.Box:
                            BoxCollider boxCollider = t.gameObject.AddComponent<BoxCollider>();
                            boxCollider.center = (float3)_colliderCenter;
                            boxCollider.size = (float3)_colliderData;
                            boxCollider.isTrigger = false;
                            break;
                        case ColliderType.Sphere:
                            SphereCollider sphereCollider = t.gameObject.AddComponent<SphereCollider>();
                            sphereCollider.center = (float3)_colliderCenter;
                            sphereCollider.radius = _colliderData.x;
                            sphereCollider.isTrigger = false;
                            break;
                        case ColliderType.None:
                        default:
                            break;
                    }
                }

                CopyJointData(t);
            }

            public void CopyJointData(Transform t) {
                if (_characterJointData != null) {
                    CharacterJoint characterJoint = t.gameObject.AddComponent<CharacterJoint>();
                    var parentBoneName = _characterJointData.parentBoneName;
                    characterJoint.connectedBody = t.GetComponentsInParent<Rigidbody>().FirstOrDefault(r => r.name == parentBoneName);
                    characterJoint.anchor = (float3)_characterJointData.anchor;
                    characterJoint.axis = (float3)_characterJointData.axis;
                    characterJoint.autoConfigureConnectedAnchor = _characterJointData.autoConfigureConnectedAnchor;
                    characterJoint.connectedAnchor = (float3)_characterJointData.connectedAnchor;
                    characterJoint.swingAxis = (float3)_characterJointData.swingAxis;
                    characterJoint.twistLimitSpring = _characterJointData.twistLimitSpring;
                    characterJoint.highTwistLimit = _characterJointData.highTwistLimit;
                    characterJoint.lowTwistLimit = _characterJointData.lowTwistLimit;
                    characterJoint.swingLimitSpring = _characterJointData.swingLimitSpring;
                    characterJoint.swing1Limit = _characterJointData.swing1Limit;
                    characterJoint.swing2Limit = _characterJointData.swing2Limit;
                    characterJoint.enableProjection = _characterJointData.enableProjection;
                    characterJoint.projectionDistance = _characterJointData.projectionDistance;
                    characterJoint.projectionAngle = _characterJointData.projectionAngle;
                    characterJoint.breakForce = _characterJointData.breakForce;
                    characterJoint.breakTorque = _characterJointData.breakTorque;
                    characterJoint.enableCollision = _characterJointData.enableCollision;
                    characterJoint.enablePreprocessing = _characterJointData.enablePreprocessing;
                    characterJoint.massScale = _characterJointData.massScale;
                    characterJoint.connectedMassScale = _characterJointData.connectedMassScale;
                }

                if (_configurableJointData != null) {
                    ConfigurableJoint configurableJoint = t.gameObject.AddComponent<ConfigurableJoint>();
                    var connectedBodyName = _configurableJointData.connectedBodyName;
                    configurableJoint.connectedBody = t.GetComponentsInParent<Rigidbody>().FirstOrDefault(r => r.name == connectedBodyName);
                    var articulationBodyName = _configurableJointData.articulationBodyName;
                    configurableJoint.connectedArticulationBody = t.GetComponentsInParent<ArticulationBody>().FirstOrDefault(r => r.name == articulationBodyName);
                    configurableJoint.anchor = (float3)_configurableJointData.anchor;
                    configurableJoint.axis = (float3)_configurableJointData.axis;
                    configurableJoint.autoConfigureConnectedAnchor = _configurableJointData.autoConfigureConnectedAnchor;
                    configurableJoint.connectedAnchor = (float3)_configurableJointData.connectedAnchor;
                    configurableJoint.secondaryAxis = (float3)_configurableJointData.secondaryAxis;
                    configurableJoint.xMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.xMotion;
                    configurableJoint.yMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.yMotion;
                    configurableJoint.zMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.zMotion;
                    configurableJoint.angularXMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.angularXMotion;
                    configurableJoint.angularYMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.angularYMotion;
                    configurableJoint.angularZMotion = (UnityEngine.ConfigurableJointMotion)(byte)_configurableJointData.angularZMotion;
                    configurableJoint.linearLimitSpring = _configurableJointData.linearLimitSpring;
                    configurableJoint.linearLimit = _configurableJointData.linearLimit;
                    configurableJoint.angularXLimitSpring = _configurableJointData.angularXLimitSpring;
                    configurableJoint.lowAngularXLimit = _configurableJointData.lowAngularXLimit;
                    configurableJoint.highAngularXLimit = _configurableJointData.highAngularXLimit;
                    configurableJoint.angularYZLimitSpring = _configurableJointData.angularYZLimitSpring;
                    configurableJoint.angularYLimit = _configurableJointData.angularYLimit;
                    configurableJoint.angularZLimit = _configurableJointData.angularZLimit;
                    configurableJoint.xDrive = _configurableJointData.xDrive;
                    configurableJoint.yDrive = _configurableJointData.yDrive;
                    configurableJoint.zDrive = _configurableJointData.zDrive;
                    configurableJoint.targetRotation = _configurableJointData.targetRotation;
                    configurableJoint.targetAngularVelocity = (float3)_configurableJointData.targetAngularVelocity;
                    configurableJoint.rotationDriveMode = _configurableJointData.rotationDriveMode;
                    configurableJoint.angularXDrive = _configurableJointData.angularXDrive;
                    configurableJoint.angularYZDrive = _configurableJointData.angularYZDrive;
                    configurableJoint.slerpDrive = _configurableJointData.slerpDrive;
                    configurableJoint.projectionMode = _configurableJointData.projectionMode;
                    configurableJoint.projectionDistance = _configurableJointData.projectionDistance;
                    configurableJoint.projectionAngle = _configurableJointData.projectionAngle;
                    configurableJoint.configuredInWorldSpace = _configurableJointData.configuredInWorldSpace;
                    configurableJoint.swapBodies = _configurableJointData.swapBodies;
                    configurableJoint.breakForce = _configurableJointData.breakForce;
                    configurableJoint.breakTorque = _configurableJointData.breakTorque;
                    configurableJoint.enableCollision = _configurableJointData.enableCollision;
                    configurableJoint.enablePreprocessing = _configurableJointData.enablePreProcessing;
                    configurableJoint.massScale = _configurableJointData.massScale;
                    configurableJoint.connectedMassScale = _configurableJointData.connectedMassScale;
                }
            }

            public class RigidbodyData {
                public float drag;
                public float mass;
                public float angularDrag;
                public RigidbodyConstraints constraints;
                public RigidbodyInterpolation interpolation;
                public CollisionDetectionMode collisionDetectionMode;
                public bool detectCollisions;

                // Copy from Unity source but byte instead of int
                public enum RigidbodyConstraints : byte {
                    [UnityEngine.Scripting.Preserve] None = 0,
                    [UnityEngine.Scripting.Preserve] FreezePositionX = 2,
                    [UnityEngine.Scripting.Preserve] FreezePositionY = 4,
                    [UnityEngine.Scripting.Preserve] FreezePositionZ = 8,
                    [UnityEngine.Scripting.Preserve] FreezePosition = 14,
                    [UnityEngine.Scripting.Preserve] FreezeRotationX = 16,
                    [UnityEngine.Scripting.Preserve] FreezeRotationY = 32,
                    [UnityEngine.Scripting.Preserve] FreezeRotationZ = 64,
                    [UnityEngine.Scripting.Preserve] FreezeRotation = 112,
                    [UnityEngine.Scripting.Preserve] FreezeAll = 126,
                }

                public enum RigidbodyInterpolation : byte {
                    [UnityEngine.Scripting.Preserve] None,
                    [UnityEngine.Scripting.Preserve] Interpolate,
                    [UnityEngine.Scripting.Preserve] Extrapolate,
                }

                public enum CollisionDetectionMode : byte {
                    [UnityEngine.Scripting.Preserve] Discrete,
                    [UnityEngine.Scripting.Preserve] Continuous,
                    [UnityEngine.Scripting.Preserve] ContinuousDynamic,
                    [UnityEngine.Scripting.Preserve] ContinuousSpeculative,
                }
            }

            public class CharacterJointData {
                public string parentBoneName;
                public float breakTorque;
                public float breakForce;
                public SoftJointLimit highTwistLimit;
                public SoftJointLimit lowTwistLimit;
                public SoftJointLimit swing1Limit;
                public SoftJointLimit swing2Limit;
                public SoftJointLimitSpring twistLimitSpring;
                public SoftJointLimitSpring swingLimitSpring;
                public half3 anchor;
                public half3 axis;
                public half3 connectedAnchor;
                public half3 swingAxis;
                public half projectionDistance;
                public half projectionAngle;
                public half massScale;
                public half connectedMassScale;
                public bool autoConfigureConnectedAnchor;
                public bool enableProjection;
                public bool enableCollision;
                public bool enablePreprocessing;
            }

            public class ConfigurableJointData {
                public string connectedBodyName;
                public string articulationBodyName;
                public JointDrive xDrive;
                public JointDrive yDrive;
                public JointDrive zDrive;
                public JointDrive angularXDrive;
                public JointDrive angularYZDrive;
                public JointDrive slerpDrive;
                public RotationDriveMode rotationDriveMode;
                public JointProjectionMode projectionMode;
                public float projectionDistance;
                public float projectionAngle;
                public float breakForce;
                public float breakTorque;
                public float massScale;
                public float connectedMassScale;

                public SoftJointLimit linearLimit;
                public SoftJointLimit lowAngularXLimit;
                public SoftJointLimit highAngularXLimit;
                public SoftJointLimit angularYLimit;
                public SoftJointLimit angularZLimit;
                public SoftJointLimitSpring linearLimitSpring;
                public SoftJointLimitSpring angularXLimitSpring;
                public SoftJointLimitSpring angularYZLimitSpring;
                public quaternionHalf targetRotation;
                public half3 anchor;
                public half3 axis;
                public half3 connectedAnchor;
                public half3 secondaryAxis;
                public half3 targetAngularVelocity;

                public ConfigurableJointMotion xMotion;
                public ConfigurableJointMotion yMotion;
                public ConfigurableJointMotion zMotion;
                public ConfigurableJointMotion angularXMotion;
                public ConfigurableJointMotion angularYMotion;
                public ConfigurableJointMotion angularZMotion;
                public bool autoConfigureConnectedAnchor;
                public bool configuredInWorldSpace;
                public bool swapBodies;
                public bool enableCollision;
                public bool enablePreProcessing;

                public enum ConfigurableJointMotion : byte {
                    [UnityEngine.Scripting.Preserve] Locked,
                    [UnityEngine.Scripting.Preserve] Limited,
                    [UnityEngine.Scripting.Preserve] Free,
                }
            }

            public struct SoftJointLimit {
                public half limit;
                public half bounciness;
                public half contactDistance;

                public static implicit operator SoftJointLimit(UnityEngine.SoftJointLimit limit) {
                    return new SoftJointLimit {
                        limit = (half)limit.limit,
                        bounciness = (half)limit.bounciness,
                        contactDistance = (half)limit.contactDistance
                    };
                }

                public static implicit operator UnityEngine.SoftJointLimit(SoftJointLimit limit) {
                    return new UnityEngine.SoftJointLimit {
                        limit = (float)limit.limit,
                        bounciness = (float)limit.bounciness,
                        contactDistance = (float)limit.contactDistance
                    };
                }
            }

            public struct SoftJointLimitSpring {
                public half spring;
                public half damper;

                public static implicit operator SoftJointLimitSpring(UnityEngine.SoftJointLimitSpring limit) {
                    return new SoftJointLimitSpring {
                        spring = (half)limit.spring,
                        damper = (half)limit.damper
                    };
                }

                public static implicit operator UnityEngine.SoftJointLimitSpring(SoftJointLimitSpring limit) {
                    return new UnityEngine.SoftJointLimitSpring {
                        spring = (float)limit.spring,
                        damper = (float)limit.damper
                    };
                }
            }
        }
    }
}
