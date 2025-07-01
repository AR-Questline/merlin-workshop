using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI.RadialMenu {
    public abstract class VRadialMenuUI<T> : View<T>, IUIAware, IFocusSource, IAutoFocusBase, IPromptHost where T : IRadialMenuUI {
        const float FullyAppearTime = 0.3f;
        protected const int SetupDelayFrame = 2;
        
        [SerializeField] Transform promptHost;
        
        [ShowInInspector, ToggleGroup(nameof(_editRadii), "edit radii")] bool _editRadii;
        [SerializeField, ToggleGroup(nameof(_editRadii))] float padDeadzone = 0.5f;
        [SerializeField, ToggleGroup(nameof(_editRadii))] float mouseDeadzone;
        [SerializeField, ToggleGroup(nameof(_editRadii))] float mouseAntiDeadzone;
        [InfoBox("Shall be adjust only on 720p resolution", InfoMessageType.Warning)]
        [SerializeField, ToggleGroup(nameof(_editRadii))] float optionsRadius;

        VCRadialMenuOption<T>[] _options;
        VCRadialMenuOption<T> _hovered;

        protected bool _fullyAppear;

        Tween _arrowTween;
        
        bool _optionsSet;
        bool _closing;
        
        Prompt _promptSelect;
        Prompt _promptUse;
        Prompt _promptClose;

        float _padDeadzoneSq;
        float _mouseDeadzoneSq;
        float _mouseAntiDeadzoneSq;
        
        bool HoldToShow { get; set; }
        bool ToggleToShow => !HoldToShow;
        float MouseDeadzone => mouseDeadzone; //todo: apply canvas scaling somehow
        float MouseAntiDeadzone => mouseAntiDeadzone; //todo: apply canvas scaling somehow
        float OptionsRadius => optionsRadius;

        public Transform PromptsHost => promptHost;
        protected Prompts Prompts { get; private set; }

        bool IFocusSource.ForceFocus => true;
        Component IFocusSource.DefaultFocus => this;
        
        /// <summary> Appear UI logic. Called OnInitialize </summary>
        protected abstract void Appear();
        
        /// <summary> Disappear UI logic. Must call Target.Discard(). </summary>
        protected abstract void Disappear();

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        protected override void OnInitialize() {
            Prompts = Target.AddElement(new Prompts(this));
            HoldToShow = World.Only<QuickUseWheelSetting>().HoldEnabled;
            
            Appear();
            SetupAndAppear().Forget();
            InitPrompts();
            
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target, -1));
            
            _mouseDeadzoneSq = MouseDeadzone * MouseDeadzone;
            _mouseAntiDeadzoneSq = MouseAntiDeadzone * MouseAntiDeadzone;
            _padDeadzoneSq = padDeadzone * padDeadzone;
        }
        
        async UniTaskVoid SetupAndAppear() {
            SetupAfter(SetupDelayFrame).Forget();
            if (await AsyncUtil.WaitUntil(this, () => _optionsSet)) {
                FullyAppearAfter(FullyAppearTime).Forget();
            }
        }
        
        protected async UniTaskVoid SetupAfter(int frames) {
            if (await AsyncUtil.DelayFrame(this, frames) && !_closing) {
                _options = GetComponentsInChildren<VCRadialMenuOption<T>>(false).Where(o => !o.isQuickAction).ToArray();
                foreach (var option in _options) {
                    option.Setup(option.transform.position - transform.position);
                }
                _optionsSet = true;
            }
        }

        protected void ClearOptions() {
            if (_options == null) {
                return;
            }
            
            _optionsSet = false;
            foreach (var option in _options) {
                option.ResetOption();
            }
            _hovered = null;
            _options = null;
        }

        async UniTaskVoid FullyAppearAfter(float time) {
            if (await AsyncUtil.DelayTime(this, time, true)) {
                _fullyAppear = true;
            }
        }
        
        [UnityEngine.Scripting.Preserve]
        protected abstract VCRadialMenuOption<T> InitialOptionFrom(VCRadialMenuOption<T>[] options);

        protected virtual void InitPrompts() {
            _promptUse = Prompt.Tap(KeyBindings.UI.QuickWheel.QuickWheelUse, LocTerms.UIGenericUse.Translate(), UseQuickSlot);
            _promptSelect = Prompt.Tap(KeyBindings.UI.QuickWheel.QuickWheelSelect, LocTerms.Select.Translate(), () => Select());
            _promptClose = Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close);
            Prompts.AddPrompt<VBrightPromptUI>(_promptSelect, Target, false);
            Prompts.AddPrompt<VBrightPromptUI>(_promptUse, Target, false, false);

            if (ToggleToShow) {
                Prompts.AddPrompt<VBrightPromptUI>(_promptClose, Target);
            }
        }

        void Update() {
            if (_options == null | _closing) return;

            float x, y;
            bool anyInput = true;
            if (RewiredHelper.IsGamepad) {
                x = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal);
                y = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical);
                
                if (x * x + y * y < _padDeadzoneSq) {
                    anyInput = false;
                }
            } else {
                x = Input.mousePosition.x - Screen.width * 0.5f;
                y = Input.mousePosition.y - Screen.height * 0.5f;
                
                if (x * x + y * y < _mouseDeadzoneSq || x * x + y * y > _mouseAntiDeadzoneSq) {
                    anyInput = false;
                }
            }
            
            if (anyInput && _optionsSet) {
                Refresh(Mathf.Atan2(y, x));
            } else {
                Hover(null);
            }
        }
        
        void Refresh(float theta) {
            var option = OptionAt(theta);
            Hover(option);
        }

        VCRadialMenuOption<T> OptionAt(float theta) {
            return _options.MinBy(option => {
                float delta = Mathf.Abs(option.Theta - theta);
                return Mathf.Min(delta, 2 * Mathf.PI - delta);
            }, true);
        }

        void Hover(VCRadialMenuOption<T> option) {
            if (_hovered == option) {
                return;
            }
            
            _hovered?.OnHoverEnd();
            _hovered = option;
            VCRadialMenuOption<T>.OptionDescription description;
            if (_hovered != null) {
                _hovered.OnHoverStart();
                description = _hovered.Description;
                RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);

                bool isQuickSlot = _hovered is VCQuickSlot;
                _promptUse.SetupState(isQuickSlot, isQuickSlot);
            } else {
                description = VCRadialMenuOption<T>.OptionDescription.Empty;
            }

            _promptSelect.SetActive(description.active);
            _promptSelect.ChangeName(description.name);
        }

        public void Close() {
            if (!_closing) {
                _closing = true;
                Disappear();
                _promptSelect.SetActive(false);
                _promptUse.SetActive(false);
            }
        }

        void UseQuickSlot() {
            if (_hovered is VCQuickItemBase quickSlot) {
                quickSlot.UseItemAction();
            }
        }
        
        void Select(bool onClose = false) {
            _hovered?.OnSelect(onClose);
        }
        
        bool ActionKeyMatches(UIAction keyAction) {
            return keyAction.Name == Target.MainKey;
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UINaviAction) {
                return UIResult.Ignore;
            }

            if (evt is UIKeyLongUpAction longUpAction && ActionKeyMatches(longUpAction)) {
                if (ToggleToShow) {
                    return UIResult.Ignore;
                }
            }
            
            if (evt is UIKeyUpAction upAction && ActionKeyMatches(upAction)) {
                if (HoldToShow || (ToggleToShow && _fullyAppear && _options != null)) {
                    Select(onClose: true);
                    Close();
                }
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        void OnDrawGizmosSelected() {
            if (_editRadii) {
                var pos = transform.position;
                using (new GizmosColor(Color.red)) {
                    Gizmos.DrawWireSphere(pos, MouseDeadzone);
                }
                using (new GizmosColor(Color.red)) {
                    Gizmos.DrawWireSphere(pos, MouseAntiDeadzone);
                }
                using (new GizmosColor(Color.green)) {
                    Gizmos.DrawWireSphere(pos, OptionsRadius);
                }
            }
        }

#if UNITY_EDITOR
        [Button, ToggleGroup(nameof(_editRadii))] 
        void DebugSnapOptionsToRadius() {
            foreach (var option in GetComponentsInChildren<VCRadialMenuOption<T>>().Where(ro => !ro.isQuickAction)) {
                var optionTransform = option.transform;
                var radialPosition = transform.position;
                var offset = optionTransform.position - radialPosition;
                offset = offset.normalized * OptionsRadius;
                optionTransform.position = radialPosition + offset;
            }
        }
        
        [Button("Rotate slots to match menu rotation")]
        void DebugRotateItemSlots() {
            foreach (var itemSlotUI in GetComponentsInChildren<ItemSlotUI>()) {
                itemSlotUI.transform.rotation = transform.rotation;
            }
            
            foreach (Image img in transform.GetComponentsInChildren<Image>().Where(im => im.gameObject.name == "BGIcon")) {
                img.transform.rotation = transform.rotation;
            }
            
            foreach (Image img in transform.GetComponentsInChildren<Image>().Where(im => im.gameObject.name == "PreviewIcon")) {
                img.transform.GetChild(0).transform.rotation = transform.rotation;
            }
            
            Log.Important?.Info("Rotated all item slots to match menu rotation");
        }
#endif
    }
}