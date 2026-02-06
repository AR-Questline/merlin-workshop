using System;
using System.Linq;
using Awaken.Utility.Maths.Data;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Animations {
    [Serializable]
    public struct RagdollBoneConfig {
        const float MaxLinearVelocity = 200f; // default is 1e+16
        const float MaxAngularVelocity = 5f; // default is 7
        const float MaxDepenetrationVelocity = 2f; // default is 10

        // --- rigidbody
        [SerializeField] NullRef<RigidbodyData> _rigidbodyDataRef;
        // --- Character Joint
        [SerializeField] NullRef<CharacterJointData> _characterJointDataRef;
        // --- Configurable Joint
        [SerializeField] NullRef<ConfigurableJointData> _configurableJointDataRef;

        // --- Collider
        [SerializeField] half3 _colliderCenter;
        [SerializeField] half3 _colliderData;
        [SerializeField] ColliderType _colliderType;

        public bool HasRigidbody => _rigidbodyDataRef._value != null;

        public readonly void CopyTo(Transform target, float massRatio, Action<Rigidbody> additionalRigidbodySetup) {
            if (_rigidbodyDataRef.TryGetValue(out var rigidbodyData)) {
                var rigidbody = target.gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = false;
                rigidbody.constraints = (RigidbodyConstraints)(byte)rigidbodyData.constraints;
                rigidbody.linearDamping = rigidbodyData.drag;
                rigidbody.interpolation = (RigidbodyInterpolation)(byte)rigidbodyData.interpolation;
                rigidbody.mass = rigidbodyData.mass * massRatio;
                rigidbody.angularDamping = rigidbodyData.angularDrag;
                rigidbody.detectCollisions = rigidbodyData.detectCollisions;
                rigidbody.collisionDetectionMode = (CollisionDetectionMode)(byte)rigidbodyData.collisionDetectionMode;
                rigidbody.maxLinearVelocity = MaxLinearVelocity;
                rigidbody.maxAngularVelocity = MaxAngularVelocity;
                rigidbody.maxDepenetrationVelocity = MaxDepenetrationVelocity;
                additionalRigidbodySetup?.Invoke(rigidbody);
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null) {
                collider.isTrigger = false;
            } else {
                switch (_colliderType) {
                    case ColliderType.Capsule:
                        CapsuleCollider capsuleCollider = target.gameObject.AddComponent<CapsuleCollider>();
                        capsuleCollider.center = (float3)_colliderCenter;
                        capsuleCollider.radius = _colliderData.x;
                        capsuleCollider.height = _colliderData.y;
                        capsuleCollider.direction = (int)_colliderData.z;
                        capsuleCollider.isTrigger = false;
                        break;
                    case ColliderType.Box:
                        BoxCollider boxCollider = target.gameObject.AddComponent<BoxCollider>();
                        boxCollider.center = (float3)_colliderCenter;
                        boxCollider.size = (float3)_colliderData;
                        boxCollider.isTrigger = false;
                        break;
                    case ColliderType.Sphere:
                        SphereCollider sphereCollider = target.gameObject.AddComponent<SphereCollider>();
                        sphereCollider.center = (float3)_colliderCenter;
                        sphereCollider.radius = _colliderData.x;
                        sphereCollider.isTrigger = false;
                        break;
                    case ColliderType.None:
                    default:
                        break;
                }
            }

            CopyJointData(target);
        }

        public readonly void CopyJointData(Transform target) {
            if (_characterJointDataRef.TryGetValue(out var characterJointData)) {
                CharacterJoint characterJoint = target.gameObject.AddComponent<CharacterJoint>();
                var parentBoneName = characterJointData.parentBoneName;
                characterJoint.connectedBody = target.GetComponentsInParent<Rigidbody>().FirstOrDefault(r => r.name == parentBoneName);
                characterJoint.anchor = (float3)characterJointData.anchor;
                characterJoint.axis = (float3)characterJointData.axis;
                characterJoint.autoConfigureConnectedAnchor = characterJointData.autoConfigureConnectedAnchor;
                characterJoint.connectedAnchor = (float3)characterJointData.connectedAnchor;
                characterJoint.swingAxis = (float3)characterJointData.swingAxis;
                characterJoint.twistLimitSpring = characterJointData.twistLimitSpring;
                characterJoint.highTwistLimit = characterJointData.highTwistLimit;
                characterJoint.lowTwistLimit = characterJointData.lowTwistLimit;
                characterJoint.swingLimitSpring = characterJointData.swingLimitSpring;
                characterJoint.swing1Limit = characterJointData.swing1Limit;
                characterJoint.swing2Limit = characterJointData.swing2Limit;
                characterJoint.enableProjection = characterJointData.enableProjection;
                characterJoint.projectionDistance = characterJointData.projectionDistance;
                characterJoint.projectionAngle = characterJointData.projectionAngle;
                characterJoint.breakForce = characterJointData.breakForce;
                characterJoint.breakTorque = characterJointData.breakTorque;
                characterJoint.enableCollision = characterJointData.enableCollision;
                characterJoint.enablePreprocessing = characterJointData.enablePreprocessing;
                characterJoint.massScale = characterJointData.massScale;
                characterJoint.connectedMassScale = characterJointData.connectedMassScale;
            }

            if (_configurableJointDataRef.TryGetValue(out var configurableJointData)) {
                ConfigurableJoint configurableJoint = target.gameObject.AddComponent<ConfigurableJoint>();
                var connectedBodyName = configurableJointData.connectedBodyName;
                configurableJoint.connectedBody = target.GetComponentsInParent<Rigidbody>().FirstOrDefault(r => r.name == connectedBodyName);
                var articulationBodyName = configurableJointData.articulationBodyName;
                configurableJoint.connectedArticulationBody = target.GetComponentsInParent<ArticulationBody>().FirstOrDefault(r => r.name == articulationBodyName);
                configurableJoint.anchor = (float3)configurableJointData.anchor;
                configurableJoint.axis = (float3)configurableJointData.axis;
                configurableJoint.autoConfigureConnectedAnchor = configurableJointData.autoConfigureConnectedAnchor;
                configurableJoint.connectedAnchor = (float3)configurableJointData.connectedAnchor;
                configurableJoint.secondaryAxis = (float3)configurableJointData.secondaryAxis;
                configurableJoint.xMotion = (ConfigurableJointMotion)(byte)configurableJointData.xMotion;
                configurableJoint.yMotion = (ConfigurableJointMotion)(byte)configurableJointData.yMotion;
                configurableJoint.zMotion = (ConfigurableJointMotion)(byte)configurableJointData.zMotion;
                configurableJoint.angularXMotion = (ConfigurableJointMotion)(byte)configurableJointData.angularXMotion;
                configurableJoint.angularYMotion = (ConfigurableJointMotion)(byte)configurableJointData.angularYMotion;
                configurableJoint.angularZMotion = (ConfigurableJointMotion)(byte)configurableJointData.angularZMotion;
                configurableJoint.linearLimitSpring = configurableJointData.linearLimitSpring;
                configurableJoint.linearLimit = configurableJointData.linearLimit;
                configurableJoint.angularXLimitSpring = configurableJointData.angularXLimitSpring;
                configurableJoint.lowAngularXLimit = configurableJointData.lowAngularXLimit;
                configurableJoint.highAngularXLimit = configurableJointData.highAngularXLimit;
                configurableJoint.angularYZLimitSpring = configurableJointData.angularYZLimitSpring;
                configurableJoint.angularYLimit = configurableJointData.angularYLimit;
                configurableJoint.angularZLimit = configurableJointData.angularZLimit;
                configurableJoint.xDrive = configurableJointData.xDrive;
                configurableJoint.yDrive = configurableJointData.yDrive;
                configurableJoint.zDrive = configurableJointData.zDrive;
                configurableJoint.targetRotation = configurableJointData.targetRotation;
                configurableJoint.targetAngularVelocity = (float3)configurableJointData.targetAngularVelocity;
                configurableJoint.rotationDriveMode = configurableJointData.rotationDriveMode;
                configurableJoint.angularXDrive = configurableJointData.angularXDrive;
                configurableJoint.angularYZDrive = configurableJointData.angularYZDrive;
                configurableJoint.slerpDrive = configurableJointData.slerpDrive;
                configurableJoint.projectionMode = configurableJointData.projectionMode;
                configurableJoint.projectionDistance = configurableJointData.projectionDistance;
                configurableJoint.projectionAngle = configurableJointData.projectionAngle;
                configurableJoint.configuredInWorldSpace = configurableJointData.configuredInWorldSpace;
                configurableJoint.swapBodies = configurableJointData.swapBodies;
                configurableJoint.breakForce = configurableJointData.breakForce;
                configurableJoint.breakTorque = configurableJointData.breakTorque;
                configurableJoint.enableCollision = configurableJointData.enableCollision;
                configurableJoint.enablePreprocessing = configurableJointData.enablePreProcessing;
                configurableJoint.massScale = configurableJointData.massScale;
                configurableJoint.connectedMassScale = configurableJointData.connectedMassScale;
            }
        }

        [Serializable]
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

        [Serializable]
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

        [Serializable]
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

        [Serializable]
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

        [Serializable]
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

        [Serializable]
        public struct JointDrive {
            public float positionSpring;
            public float positionDamper;
            public float maximumForce;
            public bool useAcceleration;

            public static implicit operator JointDrive(UnityEngine.JointDrive drive) {
                return new JointDrive {
                    positionSpring = drive.positionSpring,
                    positionDamper = drive.positionDamper,
                    maximumForce = drive.maximumForce,
                    useAcceleration = drive.useAcceleration
                };
            }

            public static implicit operator UnityEngine.JointDrive(JointDrive drive) {
                var unityDrive = new UnityEngine.JointDrive();
                unityDrive.positionSpring = drive.positionSpring;
                unityDrive.positionDamper = drive.positionDamper;
                unityDrive.maximumForce = drive.maximumForce;
                unityDrive.useAcceleration = drive.useAcceleration;
                return unityDrive;
            }
        }

        enum ColliderType : byte {
            None = 0,
            Capsule = 1,
            Box = 2,
            Sphere = 3,
        }

        [Serializable]
        struct NullRef<T> where T : class {
            [SerializeReference] public T _value;

            public readonly bool TryGetValue(out T value) {
                if (_value == null) {
                    value = null;
                    return false;
                }
                value = _value;
                return true;
            }

            public static implicit operator NullRef<T>(T value) {
                return new NullRef<T> { _value = value };
            }
        }

#if UNITY_EDITOR
        public struct EditorAccess {
            public static void Save(ref RagdollBoneConfig config, Transform bone) {
                var rigidbody = bone.GetComponent<Rigidbody>();
                var ragdollCollider = bone.GetComponent<Collider>();
                var characterJoint = bone.GetComponent<CharacterJoint>();
                var configurableJoint = bone.GetComponent<ConfigurableJoint>();

                Save(ref config, rigidbody, ragdollCollider, characterJoint, configurableJoint);
            }

            public static void Clear(Transform bone) {
                var rigidbody = bone.GetComponent<Rigidbody>();
                var ragdollCollider = bone.GetComponent<Collider>();
                var characterJoint = bone.GetComponent<CharacterJoint>();
                var configurableJoint = bone.GetComponent<ConfigurableJoint>();

                Object.DestroyImmediate(configurableJoint);
                Object.DestroyImmediate(characterJoint);
                Object.DestroyImmediate(rigidbody);
                Object.DestroyImmediate(ragdollCollider);
            }

            static void Save(ref RagdollBoneConfig config, Rigidbody rb, Collider collider, CharacterJoint joint, ConfigurableJoint configurableJoint) {
                // --- rigidbody data
                if (rb != null) {
                    config._rigidbodyDataRef = new RigidbodyData {
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
                config._colliderType = ColliderType.None;
                if (collider is CapsuleCollider capsuleCollider) {
                    config._colliderType = ColliderType.Capsule;
                    config._colliderCenter = new half3(capsuleCollider.center);
                    config._colliderData = new half3((half)capsuleCollider.radius, (half)capsuleCollider.height, (half)capsuleCollider.direction);
                } else if (collider is BoxCollider boxCollider) {
                    config._colliderType = ColliderType.Box;
                    config._colliderCenter = new half3(boxCollider.center);
                    config._colliderData = new half3(boxCollider.size);
                } else if (collider is SphereCollider sphereCollider) {
                    config._colliderType = ColliderType.Sphere;
                    config._colliderCenter = new half3(sphereCollider.center);
                    config._colliderData = new half3((half)sphereCollider.radius, (half)0, (half)0);
                }

                // --- joint data
                if (joint != null) {
                    config._characterJointDataRef = new CharacterJointData() {
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
                        breakForce = joint.breakForce,
                        breakTorque = joint.breakTorque,
                        enableCollision = joint.enableCollision,
                        enablePreprocessing = joint.enablePreprocessing,
                        massScale = new half(joint.massScale),
                        connectedMassScale = new half(joint.connectedMassScale),
                    };
                }

                // --- configurable joint data
                if (configurableJoint != null) {
                    config._configurableJointDataRef = new ConfigurableJointData() {
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
        }
#endif
    }
}
