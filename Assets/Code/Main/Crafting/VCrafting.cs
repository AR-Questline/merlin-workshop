using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Crafting {
    public abstract class VCrafting<T> : View<T>, IVCrafting, IPromptListener where T : Crafting {
        public Transform inventoryParent;
        public Transform workbenchParent;
        [SerializeField, Space] FillBar craftProgressBar;
        [SerializeField] VGenericPromptUI craftPrompt;
        
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;
        
        [field: SerializeField] public Transform StaticTooltip { get; set; }
        
        bool _recipeState;
        Prompt _createPrompt;

        public Transform InventoryParent => inventoryParent;
        public Transform WorkbenchParent => workbenchParent;
        public EventReference CreateHoldSound => CommonReferences.Get.AudioConfig.CraftingAudio.CreateHoldSound;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        protected abstract bool IsInteractable { get; } 
        protected virtual float HoldTime => 0.37f;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            if (!Target.ParentModel.FullyCreated) {
                Target.ParentModel.ListenTo(CraftingTabsUI.Events.CraftingTabsInitialized, InitializePrompts, this);
            } else {
                InitializePrompts();
            }
            StaticTooltip.gameObject.SetActive(Target.FilteredHeroItems.Any());
            craftProgressBar.SetPercent(0f);
            PrepareEmptyInfo();
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups);
        }

        void InitializePrompts() {
            _createPrompt = Target.ParentModel.Prompts.BindPrompt(Prompt.Hold(KeyBindings.UI.Crafting.CraftOne, LocTerms.CraftingPromptCraft.Translate(), Create, holdTime: HoldTime), Target, craftPrompt);
            _createPrompt.AddAudio(new PromptAudio {
                KeyDownSound = CreateHoldSound
            });
            _createPrompt.AddListener(this);
            
            Target.ListenTo(Model.Events.AfterChanged, UpdateIntractability, this);
            UpdateIntractability();
        }
        
        void UpdateIntractability() {
            _createPrompt.SetActive(IsInteractable);
        }

        void Create() {
            if (!IsInteractable) {
                return;
            }
            
            Target.Create();
            FMODManager.PlayOneShot(Target.CraftCompletedSound);
        }
        
        public void SetName(string name) {}

        public void SetActive(bool active) {}

        public void SetVisible(bool visible) {}

        public void OnHoldKeyHeld(Prompt source, float percent) {
            if (source != _createPrompt) return;
            craftProgressBar.SetPercent(percent);
            RewiredHelper.VibrateLowFreq(VibrationStrength.Low, VibrationDuration.Continuous);
        }

        public void OnHoldKeyUp(Prompt source, bool completed) {
            if (source != _createPrompt) return;
            craftProgressBar.SetPercent(0);
            
            if (completed) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
            }
        }
    }
}