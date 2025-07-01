using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.HUD.Bars;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    [UsesPrefab("Crafting/Handcrafting/VHandcrafting")]
    public class VRecipeCrafting : View<IRecipeCrafting>, IAutoFocusBase, IVCrafting, IPromptListener {
        [Title("Recipe Family Crafting")]
        public Transform gridUIParent;

        [SerializeField] Transform workbenchParent;
        [SerializeField, Space] FillBar craftProgressBar;
        [SerializeField] VGenericPromptUI craftOnePrompt;
        [SerializeField] VGenericPromptUI craftManyPrompt;
        [SerializeField] VGenericPromptUI craftManyPromptKeyBoard;
        [SerializeField] GameObject cookingProfBar, alchemyProfBar, handCraftingProfBar;
        [SerializeField] VCItemLevelOutcomesInfo itemLevelOutcomesInfo;
        [SerializeField] TMP_Text adjustIngredientsText;
        [SerializeField] GameObject promptsParent;
        [field: SerializeField] public Transform StaticTooltip { get; set; }
        
        [Title("Empty Info")]
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] VCEmptyInfo emptyInfo;
        
        Prompt _promptCreateOne;
        Prompt _promptCreateMany;
        Prompt _promptCreateManyKeyboard;

        bool _successActive;
        CanvasGroup _fadeIn;
        bool ButtonInteractable => Target.ButtonInteractability;
        [UnityEngine.Scripting.Preserve] bool RecipeSelected => Target.CurrentRecipe != null;

        public Transform InventoryParent => null;
        public Transform WorkbenchParent => workbenchParent;
        public VCItemLevelOutcomesInfo ItemLevelOutcomesInfo => itemLevelOutcomesInfo;
        public CanvasGroup[] ContentGroups => new[] { contentGroup };
        public VCEmptyInfo EmptyInfoView => emptyInfo;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        EventReference _createHoldSound;

        protected override void OnInitialize() {
            _fadeIn = gameObject.AddComponent<CanvasGroup>();
            _fadeIn.alpha = 0;
            UpdateProficiencyDisplayed();
            craftProgressBar.SetPercent(0f);
            adjustIngredientsText.SetText(LocTerms.CraftingAdjustIngredientsInfo.Translate());
            adjustIngredientsText.TrySetActiveOptimized(false);
            promptsParent.SetActiveOptimized(false);
            
            _createHoldSound = CommonReferences.Get.AudioConfig.CraftingAudio.CreateHoldSound;
            PrepareEmptyInfo();
        }

        protected override void OnFullyInitialized() {
            AsyncOnFullyInitialized().Forget();
        }

        protected async UniTaskVoid AsyncOnFullyInitialized() {
            SetupButtons();
            
            await AsyncUtil.DelayFrame(Target);
            _fadeIn.alpha = 1;
        }
        
        public void PrepareEmptyInfo() {
            emptyInfo.Setup(ContentGroups);
        }

        void SetupButtons() {
            _promptCreateOne = Target.ParentModel.Prompts.BindPrompt(Prompt.Hold(KeyBindings.UI.Crafting.CraftOne, LocTerms.CraftingPromptCraft.Translate(), Create), Target, craftOnePrompt);
            _promptCreateOne.AddAudio(new PromptAudio {
                KeyDownSound = _createHoldSound
            });
            _promptCreateOne.AddListener(this);

            _promptCreateMany = Target.ParentModel.Prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Crafting.CraftOne, LocTerms.CraftingPromptCraftMany.Translate(), CreateMany, controllers: ControlSchemeFlag.Gamepad), Target, craftManyPrompt);
            _promptCreateManyKeyboard = Target.ParentModel.Prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Crafting.CraftMany, LocTerms.CraftingPromptCraftMany.Translate(), CreateMany, controllers: ControlSchemeFlag.KeyboardAndMouse), Target, craftManyPromptKeyBoard);
            Target.ListenTo(Model.Events.AfterChanged, _ => Refresh(), this);
            
            Refresh();
        }
        
        void UpdatePromptsInteractability() {
            _promptCreateOne.SetActive(ButtonInteractable);
            adjustIngredientsText.TrySetActiveOptimized(ButtonInteractable);
            promptsParent.SetActiveOptimized(ButtonInteractable);

            bool canCraftMany = ButtonInteractable && Target.CraftableItemsCount() > 1;
            _promptCreateMany.SetActive(canCraftMany);
            _promptCreateManyKeyboard.SetActive(canCraftMany);
        }

        void Refresh() {
            if (Target.CurrentRecipe is BaseRecipe recipe) {
                string promptName = LocTerms.CraftingPromptCraft.Translate();
                promptName += recipe.quantity > 1 ? $" ({recipe.quantity.ToString()})" : "";
                _promptCreateOne.ChangeName(promptName);
            }
            
            UpdatePromptsInteractability();
        }
        
        void Create() {
            if (!ButtonInteractable) {
                return;
            }
            
            Target.Create();
            HandleCreationEffects();
        }

        void CreateMany() {
            _promptCreateOne.OnHoldInterrupted();

            if (!ButtonInteractable || Target.CraftableItemsCount() <= 1) {
                return;
            }

            int quantity = Target.CurrentRecipe is BaseRecipe baseRecipe ? baseRecipe.quantity : 1;
            Target.CreateMany(HandleCreationEffects, quantity);
        }

        public void HandleCreationEffects() {
            if(!Target.CraftCompletedSound.IsNull) {
                FMODManager.PlayOneShot(Target.CraftCompletedSound);
            }
        }
        
        void UpdateProficiencyDisplayed() {
            CraftingTabTypes type = Target.ParentModel.CurrentType;
            cookingProfBar.SetActive(CraftingTabTypes.Cooking.Contains(type));
            alchemyProfBar.SetActive(CraftingTabTypes.Alchemy.Contains(type));
            handCraftingProfBar.SetActive(CraftingTabTypes.Handcrafting.Contains(type));
        }

        public void SetName(string name) { }

        public void SetActive(bool active) { }

        public void SetVisible(bool visible) { }
        
        public void OnHoldKeyHeld(Prompt source, float percent) {
            if (source != _promptCreateOne) return;
            craftProgressBar.SetPercent(percent);
            RewiredHelper.VibrateLowFreq(VibrationStrength.Low, VibrationDuration.Continuous);
        }

        public void OnHoldKeyUp(Prompt source, bool completed) {
            if (source != _promptCreateOne) return;
            craftProgressBar.SetPercent(0);
            
            if (completed) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
            }
        }
    }
}