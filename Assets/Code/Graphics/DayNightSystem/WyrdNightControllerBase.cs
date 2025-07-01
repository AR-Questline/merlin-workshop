using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Awaken.TG.Graphics.DayNightSystem {
    public abstract class WyrdNightControllerBase : StartDependentView<Hero> {
        [EnumToggleButtons, SerializeField, BoxGroup("Mode", centerLabel: true), PropertySpace(spaceBefore: 0, spaceAfter: 10), HideLabel]
        ControllerMode mode;

        [SerializeField, ShowIf(nameof(DayNightMode), false), BoxGroup("Mode"), InlineProperty]
        CurvePair dayNight;

        [SerializeField, ShowIf(nameof(DayNightMode), false), BoxGroup("Mode")]
        bool affectedByRepellers;

        [SerializeField, ShowIf(nameof(RepellerMode), false), BoxGroup("Mode"), InlineProperty]
        CurvePair wyrdRepelled;

        [SerializeField, ShowIf("@mode == ControllerMode.Repeller", false), BoxGroup("Mode"), InlineProperty]
        bool dayTimeEnable;

        [SerializeField, ShowIf(nameof(StatusMode), false), BoxGroup("Mode"), InlineProperty]
        CurvePair statusChange;

        [ShowInInspector, PropertyOrder(100), Title("Debug")]
        string _currentCurve; // for debugging

        AnimationCurve _activeCurve;

        /// <summary>
        /// at what point on the curve we are. to get the actual value we evaluate the curve at this point
        /// </summary>
        [ShowInInspector, PropertyOrder(101)]
        float _curvePosition;

        bool DayNightMode => mode == ControllerMode.DayNight;
        bool RepellerMode => mode == ControllerMode.Repeller || (affectedByRepellers && DayNightMode);
        bool StatusMode => mode == ControllerMode.Status;
        
        protected float EnabledValue => 
            DayNightMode 
                ? dayNight.onCurve.keys[^1].value
                : RepellerMode 
                    ? wyrdRepelled.onCurve.keys[^1].value
                    : StatusMode 
                        ? statusChange.onCurve.keys[^1].value
                        : 0;

        protected float DisabledValue =>
            DayNightMode
                ? dayNight.offCurve.keys[^1].value
                : RepellerMode
                    ? wyrdRepelled.offCurve.keys[^1].value
                    : StatusMode
                        ? statusChange.offCurve.keys[^1].value
                        : 0;

        [ShowInInspector, PropertyOrder(102)]
        public float CurrentValue { get; private set; }

        protected override void OnMount() {
            if (!enabled) return;
            if (StatusMode) Target.ListenTo(HeroWyrdNight.Events.StatusChanged, OnStatusChanged, this);
            if (DayNightMode || dayTimeEnable) Target.ListenTo(HeroWyrdNight.Events.WyrdNightChanged, OnNightChange, this);
            if (RepellerMode) {
                Target.ListenTo(HeroWyrdNight.Events.RepellerChanged, OnRepellerChange, this);
            }
            
            var wyrdNight = Target.TryGetElement<HeroWyrdNight>();
            if (wyrdNight != null) {
                if (StatusMode) OnStatusChanged(wyrdNight.IsHeroInWyrdness);
                if (DayNightMode || dayTimeEnable) OnNightChange(wyrdNight.Night);
                if (RepellerMode && wyrdNight.Night) OnRepellerChange();
            }

            Target.ListenTo(Hero.Events.AfterHeroRested, _ => {
                if (_activeCurve != null) {
                    _curvePosition = _activeCurve.keys[_activeCurve.length - 1].time;
                }
            }, this);

            World.EventSystem.ListenTo(SceneLifetimeEvents.Get.ID, SceneLifetimeEvents.Events.OpenWorldStateChanged, this,
                openWorld => {
                    if (openWorld) {
                        ApplyEffect(CurrentValue);
                        this.enabled = true;
                    } else {
                        this.enabled = false;
                        ApplyEffect(DisabledValue);
                    }
                });
        }

        protected abstract void ApplyEffect(float value);

        void Update() {
            if (_activeCurve == null) {
                return;
            }

            _curvePosition = math.clamp(_curvePosition + Time.deltaTime, _activeCurve.keys[0].time, _activeCurve.keys[_activeCurve.length - 1].time);
            CurrentValue = _activeCurve.Evaluate(_curvePosition);
            ApplyEffect(CurrentValue);

            // if we are at the end of the curve, we don't need to do anything
            if (_curvePosition == _activeCurve.keys[_activeCurve.length - 1].time) {
                _activeCurve = null;
                _currentCurve = "No active curve, was: " + _currentCurve;
            }
        }

        void OnNightChange(bool enabled) {
            bool isRepellerDaytimeInverse = mode == ControllerMode.Repeller && dayTimeEnable;
            if (isRepellerDaytimeInverse) {
                enabled = !enabled;
            }
            _currentCurve = enabled ? "DayNight: Enabling Effect" : "DayNight: Disabling Effect";
            UpdateActiveParameters(enabled, isRepellerDaytimeInverse ? wyrdRepelled : dayNight);
            UpdateCurvePositionToNewCurve();
        }

        void OnRepellerChange() {
            var enabled = Target.IsSafeFromWyrdness;
            _currentCurve = enabled ? "Repel: Disabling Effect" : "Repel: Enabling Effect";
            UpdateActiveParameters(!enabled, wyrdRepelled);
            UpdateCurvePositionToNewCurve();
        }

        void OnStatusChanged(bool enabled) {
            _currentCurve = enabled ? "Status: Enabling Effect" : "Status: Disabling Effect";
            UpdateActiveParameters(enabled, statusChange);
            UpdateCurvePositionToNewCurve();
        }

        void UpdateActiveParameters(bool enable, in CurvePair pair) {
            _activeCurve = enable ? pair.onCurve : pair.offCurve;
        }

        void UpdateCurvePositionToNewCurve() {
            _curvePosition = BinarySearchValueOnCurve(_activeCurve, CurrentValue, _activeCurve.keys[0].time, _activeCurve.keys[_activeCurve.length - 1].time);
        }

        static float BinarySearchValueOnCurve(AnimationCurve curve, float target, float min, float max, float epsilon = 0.01f) {
            float left = min;
            float right = max;
            float epsSqr = epsilon * epsilon;

            float leftValue = curve.Evaluate(left);
            float rightValue = curve.Evaluate(right);
            bool isAscending = leftValue < rightValue;


            if (Math.Abs(target - leftValue) < epsSqr) {
                return left;
            }

            if (Math.Abs(target - rightValue) < epsSqr) {
                return right;
            }

            while (Math.Abs(left - right) > epsSqr) {
                float mid = left + (right - left) / 2;

                float midValue = curve.Evaluate(mid);
                if (Mathf.Abs(midValue - target) < epsilon) {
                    return mid;
                }

                if (isAscending && midValue < target
                    || !isAscending && midValue > target) {
                    left = mid;
                } else {
                    right = mid;
                }
            }

            throw new AssertionException("Binary search failed", "Curve is not continuously increasing or decreasing");
        }
    }

    [Serializable]
    struct CurvePair {
        public AnimationCurve onCurve;
        public AnimationCurve offCurve;
    }

    enum ControllerMode : byte {
        DayNight,
        Repeller,
        Status,
    }
}