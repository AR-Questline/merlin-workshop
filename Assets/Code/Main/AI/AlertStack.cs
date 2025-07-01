using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Maths;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace Awaken.TG.Main.AI {
    public partial class AlertStack : Element<NpcAI> {
        const float DefaultDecreaseRate = 1f;
        const float TopDecreaseRecoverRate = 0.1f;
        const float DecreaseRate = 2.5f;
        const int MaxMergeDistanceSqr = 3;
        const float MergePower = 1.3f;
        const float OneOverMergePower = 1.0f/MergePower;
        const float DecreaseAlertTimeDelay = 2f;
        const float DecreaseAlertDuringAlertTimeDelay = 6f;

        readonly List<AlertElement> _stack = new(10);
        float _topDecreaseRate = 1;
        float _decreaseTimeDelay;
        
        public float TopDecreaseRate {
            get => _topDecreaseRate;
            set => _topDecreaseRate = Mathf.Clamp(value, 0f, 2f);
        }

        public Vector3 CurrentTarget => (_stack.Count < 1 || !_stack[0].KnownPosition) ? ParentModel.Coords : _stack[0].AlertTarget;
        public float AlertValue => _stack.Count < 1 ? 0 : 
            (AlertTransitionsPaused ? Mathf.Min(_stack[0].AlertValue, StateAlert.Alert2Combat) : _stack[0].AlertValue);
        public float AlertVisionGain { get; set; }
        public bool AlertTransitionsPaused { get; set; }

        public VelocityScheme VelocityScheme => AlertValue > StateAlert.PatrolWalk2RunPercentage ?
            VelocityScheme.Run :
            VelocityScheme.Walk;

        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<AlertStack, AlertStack> AlertChanged = new(nameof(AlertChanged));
        }

        public void NewPoi(AlertStrength alertPoints, Vector3 position, float multiplier = 1) {
            NewPoi((int)alertPoints * multiplier, position, true, null);
        }

        public void NewPoIWithHiddenPosition(float alertPoints, Vector3 position) {
            NewPoi(alertPoints, position, false, null);
        }
        
        public void NewPoi(float alertPoints, Vector3 position) {
            NewPoi(alertPoints, position, true, null);
        }

        public void NewPoi(AlertStrength alertPoints, IGrounded grounded, float multiplier = 1) {
            NewPoi((int)alertPoints * multiplier, grounded.Coords, true, grounded);
        }
        
        public void NewPoi(float alertPoints, IGrounded grounded) {
            NewPoi(alertPoints, grounded.Coords, true, grounded);
        }

        public void Reset() {
            _stack.Clear();
            TopDecreaseRate = 1;
        }

        public void RemovePoiOf(IGrounded grounded, float range = 4) {
            var coords = grounded.Coords;
            var rangeSq = range * range;
            _stack.RemoveAll(element => (element.AlertModel.TryGet(out var elementGrounded) && elementGrounded == grounded) || element.AlertTarget.SquaredDistanceTo(coords) <= rangeSq);
        }

        public void Update(float deltaTime) {
            if (_decreaseTimeDelay > 0f) {
                _decreaseTimeDelay -= deltaTime;
                return;
            }
            
            var decreaseMultiplier = ParentModel.AlertDecreaseModifierByDistanceToLastIdlePoint();
            bool hasChange = false;
            for (var i = _stack.Count - 1; i >= 0; i--) {
                var stackElement = _stack[i];
                bool isTop = i == 0;
                float decreaseValue = DecreaseRate * deltaTime * (isTop ? TopDecreaseRate : DefaultDecreaseRate) * decreaseMultiplier;
                stackElement.AlertValue -= decreaseValue;
                if (stackElement.AlertValue <= 0) {
                    _stack.RemoveAt(i);
                    if (isTop) {
                        hasChange = true;
                    }
                }
            }

            TopDecreaseRate += TopDecreaseRecoverRate * deltaTime;
            if (hasChange) {
                this.Trigger(Events.AlertChanged, this);
            }
        }

        void NewPoi(float alertPoints, Vector3 position, bool knownPosition, IGrounded grounded) {
            alertPoints *= ParentModel.NewAlertModifierByDistanceToLastIdlePoint();
            AlertElement toMerge = null;
            foreach (var stackValue in _stack) {
                if (CanMerge(stackValue)) {
                    toMerge = stackValue;
                    break;
                }
            }
            if (toMerge != null) {
                var mergedGrounded = toMerge.AlertModel;
                if (mergedGrounded.Get() == null) {
                    mergedGrounded = new(grounded);
                }
                AddToAlert(alertPoints, position, toMerge, mergedGrounded);
            } else {
                InsertNewAlertElement(alertPoints, position, knownPosition, grounded);
            }
            
            _decreaseTimeDelay = ParentModel.InIdle ? DecreaseAlertTimeDelay : DecreaseAlertDuringAlertTimeDelay;

            bool CanMerge(AlertElement alertElement) {
                if (alertElement.KnownPosition != knownPosition) {
                    // Alert Target is different, so we can't merge
                    return false;
                }
                var distanceCondition = (alertElement.AlertTarget - position).sqrMagnitude < MaxMergeDistanceSqr;
                var hasCurrentGrounded = alertElement.AlertModel.TryGet(out var currentGrounded);
                var hasNewGrounded = grounded != null;
                var sameGroundedCondition = grounded == currentGrounded;
                var hasSingleGrounded = (!hasCurrentGrounded && hasNewGrounded) ||
                                        (hasCurrentGrounded && !hasNewGrounded);
                // We want to merge if:
                // a) Grounded are same and Is in merge range
                // b) Grounded are same and Not null
                // c) Has only single grounded and Is in merge range
                return (sameGroundedCondition && distanceCondition) ||
                       (hasCurrentGrounded && sameGroundedCondition) ||
                       (hasSingleGrounded && distanceCondition);
            }
        }

        void AddToAlert(float alertPointsToAdd, Vector3 position, AlertElement toMerge, WeakModelRef<IGrounded> grounded) {
            var currentPoints = toMerge.AlertValue;

            float newPoints;
            if (alertPointsToAdd < 5) {
                // When we add less than 5 points, we add it linearly cause using the formula makes it barely noticeable
                newPoints = currentPoints + alertPointsToAdd;
            } else {
                // Do points merge by doing (a^N + b^N)^1/N, which will bump value in not linear way
                newPoints = Mathf.Pow(
                    Mathf.Pow(currentPoints, MergePower) + Mathf.Pow(alertPointsToAdd, MergePower),
                    OneOverMergePower);
            }

            toMerge.AlertValue = Mathf.Clamp(newPoints, 0, (int)AlertStrength.Max);
            toMerge.AlertTarget = position;
            toMerge.AlertModel = grounded;
            AfterStackChanged(toMerge);
        }

        void InsertNewAlertElement(float alertPoints, Vector3 position, bool useAlertTarget, IGrounded grounded) {
            var alertElement = new AlertElement {
                AlertValue = alertPoints, AlertTarget = position, KnownPosition = useAlertTarget, AlertModel = new(grounded),
            };
            _stack.Add(alertElement);
            AfterStackChanged(alertElement);
        }

        void AfterStackChanged(AlertElement trigger) {
            _stack.Sort(static (prev, next) => next.AlertValue.CompareTo(prev.AlertValue));
            if (_stack[0] == trigger) {
                this.Trigger(Events.AlertChanged, this);
            }
        }

        class AlertElement {
            public float AlertValue { get; set; }
            public Vector3 AlertTarget { get; set; }
            public bool KnownPosition { get; set; }
            public WeakModelRef<IGrounded> AlertModel { get; set; }
        }

        public enum AlertStrength {
            Min = 1,
            NoiseKnownPosition = 6,
            Weak = 15,
            Medium = 25,
            NoiseDistant = 30,
            NoiseClose = 50,
            Strong = 50,
            Max = 100,
        }
    }
}
