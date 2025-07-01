using System;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.Utility.Animations;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.Options.Views {
    public abstract class VFocusableSetting : View<ISettingHolder>, IVSetting, IHoverableView, IWithTooltip {
        [SerializeField] Image[] selectionDecors = Array.Empty<Image>();
        [SerializeField] Selectable dummySelectable;
        [SerializeField] CanvasGroup content;

        public PrefOption GenericOption { get; private set; }
        public virtual Selectable MainSelectable => dummySelectable;
        public TooltipConstructor TooltipConstructor => GenericOption.TooltipConstructor?.Invoke();
        
        public virtual void Setup(PrefOption option) {
            GenericOption = option;
            OnOptionAssigned();
            if (dummySelectable is ARSelectable arSelectable) {
                arSelectable.RegisterUIAware(this);
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, Setting.Events.SettingRefresh, this, UpdateInteractability);
            UpdateInteractability();
            this.ListenTo(Hovering.Events.HoverChanged, _ => OnHoverChanged(false), this);
            OnHoverChanged(true);
        }

        public void UpdateInteractability() {
            content.alpha = GenericOption.Interactable ? 1f : 0.4f;
            Refresh();
        }

        void OnHoverChanged(bool instant) {
            var isHovered = Hovering.IsHovered(this);
            foreach (var decor in selectionDecors) {
                decor.DOKill();
                decor.DOFade(isHovered ? 1 : 0, 0.3f).SetUpdate(true).SetInstant(instant);
            }

            RemovePrompts();
            if (!isHovered) {
                return;
            }
            if (GenericOption.Interactable) {
                SpawnPrompts();
            }
            RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
        }
        
        protected override IBackgroundTask OnDiscard() {
            RemovePrompts();
            Cleanup();
            return base.OnDiscard();
        }

        protected abstract void RemovePrompts();
        protected abstract void SpawnPrompts();
        protected abstract void Cleanup();
        protected abstract void Refresh();
        protected virtual void OnOptionAssigned() {}

        public virtual UIResult Handle(UIEvent evt) => UIResult.Ignore;
    }
    
    public abstract class VFocusableSetting<T> : VFocusableSetting where T : PrefOption {
        public T Option { get; private set; }
        
        protected sealed override void OnOptionAssigned() {
            Option = (T)GenericOption;
        }
    }
}
