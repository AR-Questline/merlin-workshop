using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.Resting {
    [UsesPrefab("Hero/VRestPopupUI")]
    public class VRestPopupUI : View<RestPopupUI>, IAutoFocusBase, IFocusSource, IUIAware, IPromptHost {
        const int HoursOnTheClock = 24;
        const float Atan2Offset = 90f;
        
        [SerializeField] Transform arm;
        [SerializeField] RectTransform clockBgFillTransform;
        [SerializeField] RectTransform clockFillTransform;
        [SerializeField] Image clockFill;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI currentTimeText;
        [SerializeField] TextMeshProUGUI currentTimeValueText;
        [SerializeField] TextMeshProUGUI restingTimeUntilText;
        [SerializeField] TextMeshProUGUI restingTimeUntilValueText;
        [SerializeField] TextMeshProUGUI warningText;
        [SerializeField] Transform promptsHost;
        [SerializeField] float mouseDeadzone;
        [SerializeField] float mouseAntiDeadzone;
        
        float _padDeadzoneSq;
        float _mouseDeadzoneSq;
        float _mouseAntiDeadzoneSq;

        public Transform PromptsHost => promptsHost;
        public bool ForceFocus => true;
        public Component DefaultFocus => this;
        public override Transform DetermineHost() => Target.ViewParent ? Target.ViewParent : World.Services.Get<ViewHosting>().OnMainCanvas();

        float? _initialAngleRotation;

        protected override void OnInitialize() {
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            titleText.SetText(LocTerms.Resting.Translate());
            warningText.SetText(LocTerms.WyrdnessRestWarning.Translate());
            currentTimeText.SetText(LocTerms.CurrentTime.Translate());
            restingTimeUntilText.SetText(LocTerms.RestingUntil.Translate());
            currentTimeValueText.SetText(FormatCurrentTime());
            warningText.gameObject.SetActive(Target.WillBeSurprisedByWyrdNight);
            
            InitPrompts();
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target));
            
            float scaleFactor = World.Services.Get<CanvasService>().MainCanvasScaleFactor;
            _padDeadzoneSq = 0.25f;
            mouseDeadzone *= scaleFactor;
            mouseAntiDeadzone *= scaleFactor;
            _mouseDeadzoneSq = mouseDeadzone * mouseDeadzone;
            _mouseAntiDeadzoneSq = mouseAntiDeadzone * mouseAntiDeadzone;
        }
        
        void Update() {
            HandleClockArmSetupForGamepad();
        }

        void InitPrompts() {
            var prompts = new Prompts(this);
            Target.AddElement(prompts);
            prompts.AddPrompt(PopupUI.AcceptTapPrompt(Target.Rest), Target);
            prompts.AddPrompt(PopupUI.CancelTapPrompt(Target.Close), Target);
        }

        void Refresh() {
            warningText.gameObject.SetActive(Target.WillBeSurprisedByWyrdNight);
            
            float currentHourFractionWithChange = (Target.WeatherHour + Target.HourValueChange) + (Target.WeatherMinute / 60f);
            float angle = (currentHourFractionWithChange / 24f) * 360f;
            
            float angleOffsetted = -angle + Atan2Offset;
            arm.rotation = Quaternion.Euler(0, 0, angleOffsetted);

            if (_initialAngleRotation == null) {
                _initialAngleRotation = angle > 0 ? angle : angle + 360f;
                clockFillTransform.rotation = Quaternion.Euler(0, 0, angleOffsetted);
            }
            
            clockFill.fillAmount = Target.HourValueChange / 24f;
            restingTimeUntilValueText.text = FormatHourPreviewString();
        }
        
        string FormatCurrentTime() {
            return FormatHourPreviewString(Target.WeatherHour, Target.WeatherMinute);
        }

        string FormatHourPreviewString() {
            return FormatHourPreviewString(Target.WeatherHour + Target.HourValueChange, Target.WeatherMinute);
        }

        static string FormatHourPreviewString(int hour, int minute) {
            return $"{hour % 24:00}:{minute % 60:00}";
        }

        public UIResult Handle(UIEvent evt) {
            switch (evt) {
                case UIEMouseDown {IsLeft: true} mouseEvent:
                    return HandleClockArmSetupForMouse(mouseEvent);
                case UIEMouseLongHeld {IsLeft: true} mouseHoldEvent:
                    return HandleClockArmSetupForMouse(mouseHoldEvent);
                case UIKeyDownAction action when action.Name == KeyBindings.Gamepad.DPad_Right || (action.Name == KeyBindings.UI.Generic.IncreaseValue && !RewiredHelper.IsGamepad):
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.Gamepad.DPad_Right || (holdAction.Name == KeyBindings.UI.Generic.IncreaseValue && !RewiredHelper.IsGamepad):
                    Target.IncreaseHourValue();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.Gamepad.DPad_Left || (action.Name == KeyBindings.UI.Generic.DecreaseValue && !RewiredHelper.IsGamepad):
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.Gamepad.DPad_Left || (holdAction.Name == KeyBindings.UI.Generic.DecreaseValue && !RewiredHelper.IsGamepad):
                    Target.DecreaseHourValue();
                    return UIResult.Accept;
            }

            return UIResult.Ignore;
        }
        
        void HandleClockArmSetupForGamepad() {
            bool anyInput = true;
            if (RewiredHelper.IsGamepad) {
                float x = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraHorizontal);
                float y = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.CameraVertical);
                float x2 = 0;//RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal);
                float y2 = 0;//-RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical);
                x += x2;
                y += y2;
                
                if (x * x + y * y < _padDeadzoneSq && x2 * x2 + y2* y2 < _padDeadzoneSq) {
                    anyInput = false;
                }
                
                if (anyInput) {
                    float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                    SetHourChangeBasedOnAngle(-angle + _initialAngleRotation!.Value);
                }
            }
        }

        UIResult HandleClockArmSetupForMouse(UIMouseButtonEvent mouseEvent) {
            Vector2 clickPosition = mouseEvent.Position.screen;
            Vector2 localPoint = clickPosition - (Vector2)clockBgFillTransform.position;

            float distanceSqr = localPoint.sqrMagnitude;

            if (distanceSqr < _mouseDeadzoneSq || distanceSqr > _mouseAntiDeadzoneSq) {
                return UIResult.Ignore;
            }

            float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
            SetHourChangeBasedOnAngle(angle + _initialAngleRotation!.Value);
            return UIResult.Accept;
        }
        
        void SetHourChangeBasedOnAngle(float angle) {
            float hourAngle = -angle + Atan2Offset;
            if (hourAngle < 0) {
                hourAngle += 360;
            }
            
            float hourFraction = hourAngle / 360f;
            int hourChange = Mathf.RoundToInt(hourFraction * HoursOnTheClock);
            Target.SetHourChange(hourChange);
        }

        void OnDrawGizmosSelected() {
            if (Application.isPlaying) {
                return;
            }
            
            Canvas canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas.scaleFactor;
            
            var pos = clockFill.transform.position;
            using (new GizmosColor(Color.red)) {
                Gizmos.DrawWireSphere(pos, mouseDeadzone * scaleFactor);
            }
            using (new GizmosColor(Color.red)) {
                Gizmos.DrawWireSphere(pos, mouseAntiDeadzone * scaleFactor);
            }
        }
    }
}