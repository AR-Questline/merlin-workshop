using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class TargetContentSizeFitter : UIBehaviour, ILayoutSelfController {
        /// <summary>
        /// The size fit modes available to use.
        /// </summary>
        public enum FitMode {
            /// <summary>
            /// Don't perform any resizing.
            /// </summary>
            Unconstrained = 0,
            /// <summary>
            /// Resize to the minimum size of the content.
            /// </summary>
            MinSize,
            /// <summary>
            /// Resize to the preferred size of the content.
            /// </summary>
            PreferredSize,
            /// <summary>
            /// Resize to the preferred size of the content with size limit applied
            /// </summary>
            LimitedPreferredSize
        }

        //TODO: temporary need to replace with UIBehaviourNotifier, only for compatibility with current state of the project
        [SerializeField] protected RectTransform m_ReferenceTransform;
        [SerializeField] protected UIBehaviourNotifier m_ReferenceNotifier;

        [SerializeField] protected FitMode m_HorizontalFit;
        [SerializeField] protected float m_HorizontalLimit = 200;
        [SerializeField] protected float m_HorizontalAllowance;

        [SerializeField] protected FitMode m_VerticalFit;
        [SerializeField] protected float m_VerticalLimit = 200;
        [SerializeField] protected float m_VerticalAllowance;
        
        RectTransform ReferenceTransform => m_ReferenceNotifier ? m_ReferenceNotifier.RectTransform ? m_ReferenceNotifier.RectTransform : m_ReferenceTransform : m_ReferenceTransform;
        
        public FitMode HorizontalFit { 
            get => m_HorizontalFit;
            set { if (SetPropertyUtility.SetStruct(ref m_HorizontalFit, value)) SetDirty(); } 
        }
        
        public FitMode VerticalFit { 
            get => m_VerticalFit;
            set { if (SetPropertyUtility.SetStruct(ref m_VerticalFit, value)) SetDirty(); } 
        }
        
        RectTransform RectTransform => _rect = _rect != null ? _rect : GetComponent<RectTransform>();
        RectTransform _rect;
        
        protected Vector2 _contentSize = Vector2.zero;
        DrivenRectTransformTracker _rectTracker;
        bool _delayedRecalculation;
        bool _firstRecalculation;

        protected TargetContentSizeFitter() { }

        protected override void Awake() {
            if (PlatformUtils.IsPlaying) {
                m_ReferenceNotifier = m_ReferenceNotifier ? m_ReferenceNotifier : m_ReferenceTransform.gameObject.GetOrAddComponent<UIBehaviourNotifier>();
                m_ReferenceNotifier.RectTransformDimensionsChanged += _ => DelayedRecalculation().Forget();
                DelayedRecalculation().Forget();
            }
        }

#if UNITY_EDITOR
        void LateUpdate() {
            if (PlatformUtils.IsPlaying == false) {
                RecalculateSize();
            }
        }
#endif

        async UniTaskVoid DelayedRecalculation() {
            if (!await AsyncUtil.WaitForPlayerLoopEvent(this, PlayerLoopTiming.EarlyUpdate)) {
                return;
            }
            
            FullRecalculate();
        }

        protected override void OnEnable() {
            base.OnEnable();
            _contentSize = ReferenceTransform.rect.size;
            SetDirty();
        }

        protected override void OnDisable() {
            _rectTracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            base.OnDisable();
        }
        
        /// <summary>
        /// Calculate and apply the horizontal component of the size to the RectTransform
        /// </summary>
        public virtual void SetLayoutHorizontal() {
            _rectTracker.Clear();
            HandleSelfFittingAlongAxis(RectTransform.Axis.Horizontal);
        }

        /// <summary>
        /// Calculate and apply the vertical component of the size to the RectTransform
        /// </summary>
        public virtual void SetLayoutVertical() {
            HandleSelfFittingAlongAxis(RectTransform.Axis.Vertical);
        }

        protected override void OnRectTransformDimensionsChange() {
            SetDirty();
        }

        void HandleSelfFittingAlongAxis(RectTransform.Axis axis) {
            FitMode fitting = axis == RectTransform.Axis.Horizontal ? HorizontalFit : VerticalFit;
            if (fitting == FitMode.Unconstrained) {
                // Keep a reference to the tracked transform, but don't control its properties:
                _rectTracker.Add(this, RectTransform, DrivenTransformProperties.None);
                return;
            }

            _rectTracker.Add(this, RectTransform, axis == RectTransform.Axis.Horizontal ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);
            
            if (ReferenceTransform == null) return;
            float allowance = axis == RectTransform.Axis.Horizontal ? m_HorizontalAllowance : m_VerticalAllowance;
            int axisIndex = axis == RectTransform.Axis.Horizontal ? 0 : 1;
            
            switch (fitting) {
                // Set size to min or preferred size
                case FitMode.MinSize:
                    RectTransform.SetSizeWithCurrentAnchors(axis, LayoutUtility.GetMinSize(ReferenceTransform, axisIndex) + allowance);
                    break;
                case FitMode.PreferredSize:
                    RectTransform.SetSizeWithCurrentAnchors(axis, LayoutUtility.GetPreferredSize(ReferenceTransform, axisIndex) + allowance);
                    break;
                case FitMode.LimitedPreferredSize:
                    float sizeLimit = axis == RectTransform.Axis.Horizontal ? m_HorizontalLimit : m_VerticalLimit;
                    RectTransform.SetSizeWithCurrentAnchors(axis, Mathf.Min(LayoutUtility.GetPreferredSize(ReferenceTransform, axisIndex), sizeLimit) + allowance);
                    break;
            }
        }

        void FullRecalculate() {
            RecalculateSize();
            
            if (IsActive()) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
            }
        }
        
        void RecalculateSize() {
            if (ReferenceTransform == null) return;

            Vector2 newSize = ReferenceTransform.sizeDelta;
            if (newSize == _contentSize) return;
            bool isDirty = false;
            
            if (HorizontalFit != FitMode.Unconstrained && !Mathf.Approximately(_contentSize.x, newSize.x)) {
                _contentSize.x = newSize.x;
                isDirty = true;
            }
            
            if (VerticalFit != FitMode.Unconstrained && !Mathf.Approximately(_contentSize.y, newSize.y)) {
                _contentSize.y = newSize.y;
                isDirty = true;
            }
            
            if (isDirty) {
                SetDirty();
            }
        }

        void SetDirty() {
            if (!IsActive()) return;
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            SetDirty();
        }
#endif
    }

    public static class SetPropertyUtility
    {
        [UnityEngine.Scripting.Preserve]
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        [UnityEngine.Scripting.Preserve]
        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}