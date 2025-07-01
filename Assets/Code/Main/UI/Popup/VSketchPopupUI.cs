using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Sketching;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/" + nameof(VSketchPopupUI))]
    public class VSketchPopupUI : View<SketchPopupUI>, IAutoFocusBase {
        const float SketchRemoveHoldTime = 1.0f;
        
        [SerializeField] public EventReference closeSound;
        [SerializeField] RawImage storyArtImage;
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] VGenericPromptUI removeButton;
        [SerializeField] RectTransform displayParent;
        [SerializeField, FoldoutGroup("Random Angle")] float minAngle = -2.5f;
        [SerializeField, FoldoutGroup("Random Angle")] float maxAngle = 2.5f;
        
        Prompts _prompts;
        Prompt _closePrompt;
        Prompt _removePrompt;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            base.OnInitialize();
            SetRandomRotation();
            UIStateStack.Instance.PushState(UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime(), Target);
        }

        protected override void OnFullyInitialized() {
            storyArtImage.texture = Target.GetSketchTexture();
            FMODManager.PlayOneShot(closeSound);
            InitPrompts();
        }

        void SetRandomRotation() {
            displayParent.localRotation = Quaternion.Euler(0, 0, RandomUtil.UniformFloat(minAngle, maxAngle));
        }
        
        void InitPrompts() {
            _prompts = Target.AddElement(new Prompts(null));
            
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), OnClose), Target, closeButton);
            _closePrompt.SetVisible(true);
            
            _closePrompt.AddAudio(new PromptAudio {
                TapSound = closeSound
            });
            
            _removePrompt = _prompts.BindPrompt(Prompt.Hold(KeyBindings.UI.Items.DropItem, LocTerms.Remove.Translate(), OnSketchRemove, holdTime: SketchRemoveHoldTime), Target, removeButton);
            _removePrompt.SetVisible(Sketch.AllowGlobalRemoval && storyArtImage.texture != null);
        }

        void OnSketchRemove() {
            Target.Sketch.RemoveSketch();
            Target.Sketch.ParentModel.Discard();
            FMODManager.PlayOneShot(closeSound);
            Target.Discard();
        }
        
        void OnClose() {
            Target.Discard();
        }
    }
}