using System;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Graphics;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    [UsesPrefab("Locations/VLockpicking")]
    public class VLockpicking : View<LockpickingInteraction>, IAutoFocusBase {
        const float FadeInTime = 0.1f;
        
        [Title("Element References")]
        [SerializeField] TextMeshProUGUI lockpickCount;
        [SerializeField] TextMeshProUGUI complexity;
        [Space, Title("Other")]
        [SerializeField] Volume _volume;
        [SerializeField] VGenericPromptUI closeButton, rotateLockButton;
        [SerializeField, ListDrawerSettings(IsReadOnly = true), LocStringCategory(Category.UI)]
        LocString[] complexityTranslations = new LocString[5];
        [Space, Title("Audio")]
        [SerializeField] public LockpickingAudio audioEvents;
        [SerializeField] public ARFmodEventEmitter audioEmitter;

        RenderTexture _backgroundTexture;
        Vector2 _originalBlurValues;
        Sequence _blurSequence;

        Prompts _prompts;
        Prompt _rotateLockPrompt;

        bool _lockpickMoved;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            Init3D();
            InitPrompts();
            
            Target.ParentModel.ListenTo(LockpickingInteraction.Events.PickBroke, _ => DisplayPicklockQuantity(), Target);

            SetupStaticText();
            DisplayPicklockQuantity();
            
            AfterFirstCanvasCalculate().Forget();
        }

        void Init3D() {
            if (_volume.GetSharedOrInstancedProfile().TryGet<DepthOfField>(out var depthOfField)) {
                _originalBlurValues.y = depthOfField.nearMaxBlur;
                depthOfField.nearMaxBlur = 0;
                _originalBlurValues.x = depthOfField.farMaxBlur;
                depthOfField.farMaxBlur = 0;
            }
        }

        void InitPrompts() {
            _prompts = Target.AddElement(new Prompts(null));
            _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Close), Target, closeButton);

            _rotateLockPrompt = Prompt.VisualOnlyHold(KeyBindings.Gameplay.Interact, LocTerms.RotateLock.Translate());
            _prompts.BindPrompt(_rotateLockPrompt, Target, rotateLockButton);
        }

        async UniTaskVoid AfterFirstCanvasCalculate() {
            if (!await AsyncUtil.WaitForPlayerLoopEvent(this, PlayerLoopTiming.PostLateUpdate)) {
                return;
            }

            if (_volume.GetSharedOrInstancedProfile().TryGet<DepthOfField>(out var depthOfField)) {
                _blurSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(DOTween.To(() => depthOfField.farMaxBlur, m => depthOfField.farMaxBlur = m, _originalBlurValues.x, FadeInTime))
                    .Join(DOTween.To(() => depthOfField.nearMaxBlur, m => depthOfField.nearMaxBlur = m, _originalBlurValues.y, FadeInTime));
            }
            
            Target.View<VLockpicking3D>().SetupCamera();
        }

        void SetupStaticText() {
            string coloredComplexity = complexityTranslations[Target.Properties.Tolerance.index].ToString().ColoredText(ARColor.MainWhite);
            complexity.text = $"{LocTerms.Complexity.Translate()}: {coloredComplexity}";
        }

        void DisplayPicklockQuantity() {
            var quantity = World.Only<HeroItems>().Items.FirstOrDefault(i => i.HasElement<Lockpick>())?.Quantity ?? 0;
            string coloredQuantity = quantity.ToString().ColoredText(ARColor.MainWhite);
            lockpickCount.text = $"{LocTerms.PicklockCount.Translate(coloredQuantity)}";
        }

        void Update() {
            if (Target.IsBlocked) {
                return;
            }
            var deltaTime = Time.unscaledDeltaTime;
            float axis = 0;//Math.Abs(RewiredHelper.Player.GetAxisRaw(KeyBindings.Minigames.LockOpenAxis));
            if (axis > 0.001f) {
                Target.PlayerTryOpen(deltaTime, axis);
            } else if (false/*RewiredHelper.Player.GetButton(KeyBindings.Gameplay.Interact)*/) {
                Target.PlayerTryOpen(deltaTime, 1);
            } else {
                Target.PlayerTryOpen(deltaTime, 0);
                UpdatePickRotation(deltaTime);
            }
        }

        void UpdatePickRotation(float deltaTime) {
            float axis = 0;//RewiredHelper.Player.GetAxisRaw(KeyBindings.Minigames.PickRotate);
            if (Math.Abs(axis) > 0.001f) {
                if (!_lockpickMoved) {
                    Target.OnLockpickStartMoving();
                    _lockpickMoved = true;
                }

                Target.PlayerRotatePick(deltaTime, axis * (RewiredHelper.IsGamepad ? 3.5f : 1));
            } else if(_lockpickMoved) {
                _lockpickMoved = false;
                Target.OnLockpickStoppedMoving();
            }
            
            Target.ResetLockState();
        }

        protected override IBackgroundTask OnDiscard() {
            _blurSequence.Kill();
            _backgroundTexture?.Release();
            _backgroundTexture = null;
            return base.OnDiscard();
        }
    }
}