using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.General.NewThings {
    public class VCNewThingMarker : ViewComponent, IUIAware {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image icon;
        [SerializeField] bool autoHide = true;

        bool _isContainer;
        bool _isHiding;
        IUINewThing _newThing;
        IUINewThing NewThing => _newThing ??= TryFindNewThing();
        
        IUINewThing TryFindNewThing() {
            if (GenericTarget is IUINewThing nt) {
                return nt;
                // ReSharper disable once SuspiciousTypeConversion.Global
            } else if (ParentView is IUINewThing nt2) {
                return nt2;
            }
            return GetComponentInParent<IUINewThing>();
        }

        void Awake() {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        
        protected override void OnAttach() {
            if (NewThing == null) {
                return;
            }

            NewThing.onNewThingRefresh += Refresh;
            if (NewThing.IsInitialized) {
                Refresh();
            }

            if (autoHide) {
                World.Only<Focus>().ListenTo(Focus.Events.FocusChanged, OnFocusChange, this);
                ParentView.ListenTo(Hovering.Events.HoverChanged, OnHoverChange, this);
            }
        }

        public void Refresh() {
            if (_isHiding) return;

            _isContainer = NewThing is INewThingContainer;
            if (NewThing is INewThingCarrier carrier) {
                carrier.NewThingModel?.ListenTo(IModelNewThing.Events.NewThingRefreshed, Refresh, this);
                ParentView.ListenTo(Hovering.Events.HoverChanged, OnHoverChange, this);
            }
            
            bool shouldBeActive = NewThing.IsNew;
            bool isActive = canvasGroup.alpha > 0.1f;
            
            if (shouldBeActive && !isActive) {
                icon.transform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            } else if (!shouldBeActive && isActive) {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Happens only if autoHide is enabled
        /// </summary>
        void OnFocusChange(FocusChange change) {
            if (_isContainer) return;
            Component targetFocus = change.current;
            if (targetFocus != null) {
                View view = targetFocus.GetComponentInParent<View>();
                while (view != null && !ViewsMatch(view)) {
                    view = view.transform.parent.GetComponentInParent<View>();
                }

                if (view != null) {
                    MarkSeen();
                }
            }
        }
        
        /// <summary>
        /// Happens only if autoHide is enabled
        /// </summary>
        void OnHoverChange(HoverChange change) {
            if (_isContainer) return;
            View view = change.View;
            if (view != null && change.Hovered) {
                while (!ViewsMatch(view)) {
                    view = view.transform.parent.GetComponentInParent<View>();
                }

                if (view != null) {
                    MarkSeen();
                }
            }
        }

        bool ViewsMatch(View view) {
            if (NewThing is IModel model) {
                return model.Views.Contains(view);
                // ReSharper disable once SuspiciousTypeConversion.Global
            } else if (NewThing is View v2) {
                return v2 == view;
            } else {
                return (NewThing as Component)?.GetComponentInParent<View>() == view;
            }
        }

        void MarkSeen() {
            if (_isHiding) return;
            Hide();
            if (NewThing is INewThingCarrier carrier) {
                carrier.MarkSeen();
            }
        }

        void Hide() {
            _isHiding = true;
            canvasGroup.blocksRaycasts = false;
            DOTween.Sequence()
                .Append(icon.transform.DOScale(Vector3.one * 0.6f, 0.2f).SetEase(Ease.OutCubic))
                .Join(canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() => _isHiding = false)
                .Play();
        }

        public UIResult Handle(UIEvent evt) {
            if (!autoHide || _isContainer) {
                return UIResult.Ignore;
            }
            
            if (evt is UIEPointTo) {
                MarkSeen();    
            }
            return UIResult.Ignore;
        }

        // onDestroy used because onDiscard is incompatible with RecyclableView
        protected override void OnDestroy() {
            if (NewThing != null) {
                NewThing.onNewThingRefresh -= Refresh;
            }
            base.OnDestroy();
        }
    }
}