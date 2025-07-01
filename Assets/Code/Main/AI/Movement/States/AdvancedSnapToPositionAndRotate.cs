using System;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class AdvancedSnapToPositionAndRotate : SnapToPositionAndRotate {
        readonly float _snapDelay;
        readonly EasingType _verticalEasingType;
        readonly EasingType _horizontalEasingType;

        public AdvancedSnapToPositionAndRotate(Vector3 position, Vector3 forward, [CanBeNull] GameObject interactionGO, SetupData settings) : 
            this(position, forward, interactionGO, settings.snapDuration, settings.snapDelay, settings.VerticalEasingType, settings.HorizontalEasingType) { }

        public AdvancedSnapToPositionAndRotate(Vector3 position, Vector3 forward, 
            [CanBeNull] GameObject interactionGO, float snapDuration = DefaultSnapDuration, float snapDelay = 0, 
            EasingType verticalEasingType = null, EasingType horizontalEasingType = null) : base(position, forward, interactionGO, snapDuration) {
            _snapDelay = snapDelay;
            _verticalEasingType = verticalEasingType ?? EasingType.CubicInOut;
            _horizontalEasingType = horizontalEasingType ?? EasingType.CubicInOut;
        }
        
        protected override void OnEnter() {
            base.OnEnter();
            _snappingState = -_snapDelay;
        }
        
        protected override void SnapStep(float deltaTime) {
            if (_snappingState < 0) {
                _snappingState += deltaTime;
                if (_snappingState >= 0) {
                    _snappingState = 0;
                }
                return;
            }
            base.SnapStep(deltaTime);
        }

        protected override void PerformPositionEasingStep(float currentEasingDelta, float targetEasingDelta) {
            float verticalDelta = GetEasedDelta(currentEasingDelta, targetEasingDelta, _verticalEasingType);
            float horizontalDelta = GetEasedDelta(currentEasingDelta, targetEasingDelta, _horizontalEasingType);

            var transform = Controller.transform;
            var position = transform.position;
            float verticalPos = Mathf.Lerp(position.y, _desiredPosition.y, verticalDelta);
            Vector3 horizontalPos = Vector3.Lerp(position.X0Z(), _desiredPosition.X0Z(), horizontalDelta);
            horizontalPos.y = verticalPos;
            
            transform.position = horizontalPos;
        }

        static float GetEasedDelta(float currentEasingDelta, float targetEasingDelta, EasingType easingType) {
            float currentEasingValue = easingType.Calculate(currentEasingDelta);
            float targetEasingValue = easingType.Calculate(targetEasingDelta);
            return Mathf.InverseLerp(currentEasingValue, 1f, targetEasingValue);
        }
        
        protected override void OnExit() { }
        
        [Serializable]
        public struct SetupData {
            public float snapDuration;
            public float snapDelay;
            public bool overrideEasing;
            [SerializeField, ShowIf(nameof(overrideEasing)), RichEnumExtends(typeof(EasingType))] RichEnumReference horizontalEasingType;
            [SerializeField, ShowIf(nameof(overrideEasing)), RichEnumExtends(typeof(EasingType))] RichEnumReference verticalEasingType;

            public EasingType HorizontalEasingType => overrideEasing ? horizontalEasingType.EnumAs<EasingType>() : null;
            public EasingType VerticalEasingType => overrideEasing ? verticalEasingType.EnumAs<EasingType>() : null;
            
            public static SetupData Default => new SetupData() {
                snapDelay = 0.5f
            };
        }
    }
}