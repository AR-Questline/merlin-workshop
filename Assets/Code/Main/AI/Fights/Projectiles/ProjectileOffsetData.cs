using System;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using DG.Tweening;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public struct ProjectileOffsetData {
        public Ease ease;
        public bool moveProjectileAtStart;
        public bool blockRotationWhileOffseting;
        public bool disableGravityWhileOffseting;
        bool _valueSet;
        float _distanceToReach;
        Vector3 _localEndOffset;
        Vector3 _startingOffset;
        Func<Vector3> _startingOffsetFunc;

        public float DeltaTimeMultiplier { get; private set; }
        public Vector3 InitialVelocity { get; private set; }

        public Vector3 StartingOffset {
            get {
                if (!_valueSet) {
                    if (_startingOffsetFunc != null) {
                        _startingOffset = _startingOffsetFunc.Invoke();
                    }
                    _valueSet = true;
                }
                return _startingOffset;
            }
        }

        public void Init(Vector3 initialVelocity) {
            const float MinTimeToReach = 0.15f;
            
            Vector3 worldEndOffset = Quaternion.LookRotation(initialVelocity) * _localEndOffset;
            
            float timeToReach = math.max(_distanceToReach / initialVelocity.magnitude, MinTimeToReach);
            DeltaTimeMultiplier = 1 / timeToReach;
            
            Vector3 direction = StartingOffset - worldEndOffset;
            
            float initialSpeed = GetInitialSpeed(direction.magnitude, timeToReach);
            InitialVelocity = direction.normalized * initialSpeed;
        }

        static float GetInitialSpeed(float s, float t) {
            //s = ut + 1/2at^2
            //a = -u/t
            //s = ut + 1/2(-u/t)t^2
            //s = ut - 1/2u
            //u = 2s / t
            return -2*s / t;
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static ProjectileOffsetData GetOffsetParams(Item item, Transform shotTransform) {
            if (item.IsMagic) {
                return MagicShootOffsetParams(item, shotTransform);
            } else if (item.IsRanged) {
                return BowOffsetParams(GetStartingOffsetFromHandBase(item, Vector3.zero), shotTransform.position + shotTransform.forward);
            } else if (item.IsThrowable) {
                return ThrowableOffsetParams(GetStartingOffsetFromHandBase(item, Vector3.zero), shotTransform.position + shotTransform.forward);
            }
            return default;
        }

        public static ProjectileOffsetData MagicShootOffsetParams(Item item, Transform shotTransform) {
            return new ProjectileOffsetData() {
                ease = Ease.InOutCubic,
                _distanceToReach = 10f,
                _startingOffsetFunc = () => GetStartingOffsetFromHandBase(item, shotTransform.position + shotTransform.forward),
                _localEndOffset = GetMagicEndOffset(item),
                moveProjectileAtStart = true,
                blockRotationWhileOffseting = false,
                disableGravityWhileOffseting = false,
            };
        }
        
        public static ProjectileOffsetData BowOffsetParams(Vector3 projectilePosition, Vector3 firePointPos) {
            return new ProjectileOffsetData() {
                ease = Ease.InOutSine,
                _distanceToReach = 3f,
                _startingOffset = GetStartingOffsetFromProjectile(projectilePosition, firePointPos),
                _valueSet = true,
                _localEndOffset = Vector3.zero,
                moveProjectileAtStart = false,
                blockRotationWhileOffseting = false,
                disableGravityWhileOffseting = true,
            };
        }
        
        public static ProjectileOffsetData ThrowableOffsetParams(Vector3 projectilePosition, Vector3 firePointPos) {
            return new ProjectileOffsetData() {
                ease = Ease.InSine,
                _distanceToReach = 3f,
                _startingOffset = GetStartingOffsetFromProjectile(projectilePosition, firePointPos),
                _valueSet = true,
                _localEndOffset = Vector3.zero,
                moveProjectileAtStart = false,
                blockRotationWhileOffseting = true,
                disableGravityWhileOffseting = true,
            };
        }

        public static Vector3 GetMagicEndOffset(Item item) {
            const float Offset = 0.075f;
            
            var eqSlot = item.EquippedInSlotOfType;
            if (eqSlot == EquipmentSlotType.MainHand) {
                return Vector3.right * Offset;
            } else if (eqSlot == EquipmentSlotType.OffHand) {
                return Vector3.left * Offset;
            }
            return Vector3.zero;
        }
        
        public static Vector3 GetStartingOffsetFromProjectile(in Vector3 projectilePosition, in Vector3 firePointPos) {
            return projectilePosition - firePointPos;
        }

        public static Vector3 GetStartingOffsetFromHandBase(Item item, in Vector3 shotPosition) {
            var handBase = GetItemHandBase(item);
            return (handBase?.VisualFirePoint?.position - shotPosition) ?? Vector3.zero;
        }
        
        static CharacterHandBase GetItemHandBase(Item item) {
            var eqSlot = item.EquippedInSlotOfType;
            if (eqSlot == EquipmentSlotType.MainHand) {
                return item.Character?.MainHandWeapon;
            } else if (eqSlot == EquipmentSlotType.OffHand) {
                return item.Character?.OffHandWeapon;
            }
            return null;
        }
    }
}