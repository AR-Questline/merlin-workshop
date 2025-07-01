using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Containers {
    public class PContainerUI : QueryPresenter<ContainerUI>, IUIPlayerInput, IPromptListener, IVisualElementPromptPresenter {
        const float DistanceToSearchForTheftReactingNPCs = 40f;

        public IVisualElementPromptHost ViewPromptHost { get; private set; }
        public int InputPriority => 1;
        public override string ContentName => "PContainerUI";

        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get {
                yield return KeyBindings.HeroItems.NextItem;
                yield return KeyBindings.HeroItems.PreviousItem;
            }
        }

        ListView _containerList;
        BetterOutlinedLabel _titleLabel;
        VisualElement _header;
        List<Item> _items;
        int _currentSelected;

        Prompts _prompts;
        Prompt _takePrompt;
        Prompt _takeAllKeyboardPrompt;
        Prompt _theftTakePrompt;
        Prompt _theftTakeAllKeyboardPrompt;

        Item _theftItem;
        bool _wasIllegal;
        float _previousHoldPercent;

        public PContainerUI(VisualElement parent, IVisualElementPromptHost host) : base(parent) {
            RegisterPromptHost(host);
        }

        public void RegisterPromptHost(IVisualElementPromptHost host) {
            ViewPromptHost = host;
        }

        public void UnregisterPromptHost() {
            ViewPromptHost = null;
        }

        protected override void CacheVisualElements(VisualElement contentRoot) {
            _containerList = contentRoot.Q<ListView>("container-list");
            _titleLabel = contentRoot.Q<BetterOutlinedLabel>("title");
            _header = contentRoot.Q<VisualElement>("header");
        }

        protected override void OnFullyInitialized() {
            World.Only<PlayerInput>().RegisterPlayerInput(this, TargetModel);
            TargetModel.ListenTo(ContainerUI.ContainerEvents.ContentChanged, RefreshContainerData, this);

            SetTitle(TargetModel.ParentModel.DisplayName, TargetModel.IsIllegal);
            Content.SetActiveOptimized(true);
            InitializePrompts();
            CreatePrompts();
        }

        protected override void ClearContent() {
            Content.SetActiveOptimized(false);
            RefreshItems(new List<Item>());

            _titleLabel.text = string.Empty;
            ViewPromptHost.VisualPromptHost.Clear();
        }

        public async UniTaskVoid PopulateItems(List<Item> items) {
            _containerList.ClearSelection();
            RefreshItems(items);

            //Wait for the list items to be populated - to select the correct item
            if (await AsyncUtil.DelayFrame(TargetModel)) {
                _containerList.SetSelection(_currentSelected);
            }
        }

        public void Select(ContainerElement target) {
            RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
            bool isIllegal = target.Crime.IsCrime();
            ShowPrompts(isIllegal);

            if (isIllegal) {
                _theftItem = target.Item;
                var location = _theftItem.Inventory.GetModelInParent<Location>();
                var npc = NpcTemplate.FromNpcOrDummy(location);
                var crimeOwners = target.Crime.Owners.AllOwners;

                if (World.Services.Get<NpcGrid>()
                         .GetNpcsInSphere(location.Coords, DistanceToSearchForTheftReactingNPCs)
                         .All(nearbyNpcs => !crimeOwners.Any(o => {
                             if (npc == null) {
                                 return nearbyNpcs.GetCurrentCrimeOwnersFor(CrimeArchetype.Theft(_theftItem)).AllOwners.Contains(o);
                             }
                             return nearbyNpcs.GetCurrentCrimeOwnersFor(CrimeArchetype.Pickpocketing(_theftItem.CrimeValue, npc.CrimeValue)).AllOwners.Contains(o);
                         }))) {
                    _theftTakePrompt.HoldTime = 0;
                    _theftTakeAllKeyboardPrompt.HoldTime = 0;
                    return;
                }

                _theftTakePrompt.HoldTime = ContainerHoldUtil.CalculateHoldTime(TargetModel, _theftItem);
                _theftTakeAllKeyboardPrompt.HoldTime = ContainerHoldUtil.CalculateTakeAllHoldTime(TargetModel, _items);
            }
        }

        public void FocusNextItem() {
            int next = _currentSelected + 1;
            _currentSelected = next >= _containerList.itemsSource.Count ? _currentSelected : next;
            FocusItem();
        }

        public void FocusPreviousItem() {
            int prev = _currentSelected - 1;
            _currentSelected = prev < 0 ? _currentSelected : prev;
            FocusItem();
        }

        public void OnHoldKeyDown(Prompt source) {
            _previousHoldPercent = 0;
        }

        public void OnHoldKeyHeld(Prompt source, float percent) {
            Hero.Current.Element<IllegalActionTracker>().PerformingSuspiciousInteraction();

            Crime currentCrime = TargetModel.CurrentItem.Crime;
            NpcElement npc = TargetModel.ParentModel.TryGetElement<NpcElement>();
            
            // we are pickpocketing a living npc
            if (currentCrime.IsPickpocketing && npc is {IsUnconscious: false}) {
                float holdTimeDelta = (percent - _previousHoldPercent) * source.HoldTime;
                _previousHoldPercent = percent;
                float alert = npc.Element<NpcCrimeReactions>().Pickpocketing(holdTimeDelta, _theftItem);
                VibrationStrength strength = alert switch {
                    > 0.8f => VibrationStrength.Strong,
                    > 0.5f => VibrationStrength.Low,
                    _ => VibrationStrength.VeryLow,
                };
                RewiredHelper.VibrateLowFreq(strength, VibrationDuration.Continuous);
            } else if (percent > 0f) {
                RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Continuous);
            }
        }

        public void OnHoldKeyUp(Prompt source, bool completed) {
            if (TargetModel.ParentModel.TryGetElement(out NpcElement npc)) {
                npc.Element<NpcCrimeReactions>().PickpocketingEnded().Forget();
            }
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyDownAction keyDown) {
                if (keyDown.Name == KeyBindings.HeroItems.NextItem) {
                    TargetModel.SelectNextItem();
                } else if (keyDown.Name == KeyBindings.HeroItems.PreviousItem) {
                    TargetModel.SelectPreviousItem();
                } else {
                    return UIResult.Ignore;
                }

                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        public void SetName(string name) { }
        public void SetActive(bool active) { }
        public void SetVisible(bool visible) { }

        public void PrepareContainerList(VisualTreeAsset prototype) {
            _takePrompt.SetActive(true);
            _takeAllKeyboardPrompt.SetActive(true);

            _containerList.makeItem = () => {
                PContainerElement entryLogic = new(prototype.Instantiate());
                World.BindPresenter(TargetModel, entryLogic);
                entryLogic.Content.userData = entryLogic;
                return entryLogic.Content;
            };

            _containerList.bindItem = (item, index) => {
                if (item.userData is not PContainerElement element) {
                    return;
                }

                element.SetData(index, _items[index], _theftTakePrompt);
                _containerList.selectedIndicesChanged += element.SelectCallback;
            };

            _containerList.unbindItem = (item, _) => {
                if (item.userData is not PContainerElement element) {
                    return;
                }

                _containerList.selectedIndicesChanged -= element.SelectCallback;
            };

            _containerList.destroyItem = item => {
                if (item.userData is not PContainerElement element) {
                    return;
                }

                element.Discard();
            }; 
        }

        void RefreshContainerData() {
            RefreshItems(TargetModel.AllItemsInContainerSorted().ToList());
            _containerList.SetSelection(_currentSelected);
        }

        void RefreshItems(List<Item> items) {
            _items = items;
            _containerList.itemsSource = _items;
            
            if (_currentSelected >= _items.Count) {
                _currentSelected -= 1;
            }
        }

        void SetTitle(string title, bool isIllegal) {
            string newTitle = title;
            _header.SetActiveOptimized(!string.IsNullOrEmpty(title));

            if (isIllegal) {
                newTitle += $" ({LocTerms.Stealing.Translate()})";
                _titleLabel.SetTextColor(ARColor.MainRed);
            } else {
                _titleLabel.SetTextColor(ARColor.MainWhite);
            }

            _titleLabel.ToUpperCase(newTitle);
        }

        public void AssignPromptRoot() {
            ViewPromptHost.VisualPromptHost = Content.Q<VisualElement>("prompt-footer");
        }

        void InitializePrompts() {
            AssignPromptRoot();
            _prompts = new Prompts(ViewPromptHost);
            TargetModel.AddElement(_prompts);

            _theftTakePrompt = Prompt.Hold(KeyBindings.UI.Items.TakeItem, LocTerms.Steal.Translate(), () => TargetModel.TakeItemFromContainer(TargetModel.CurrentItem), Prompt.Position.Last);
            _theftTakeAllKeyboardPrompt = Prompt.Hold(KeyBindings.UI.Items.TransferItems, LocTerms.PickupAll.Translate(), TargetModel.TakeAllItems, Prompt.Position.Last);
            _takePrompt = Prompt.Tap(KeyBindings.UI.Items.TakeItem, LocTerms.Pickup.Translate(), () => TargetModel.TakeItemFromContainer(TargetModel.CurrentItem), Prompt.Position.Last);
            _takeAllKeyboardPrompt = Prompt.Tap(KeyBindings.UI.Items.TransferItems, LocTerms.PickupAll.Translate(), TargetModel.TakeAllItems, Prompt.Position.Last);
        }

        void CreatePrompts() {
            _prompts.AddPrompt<VGenericPresenterPrompt>(_theftTakePrompt, TargetModel).SetVisible(false);
            _prompts.AddPrompt<VGenericPresenterPrompt>(_theftTakeAllKeyboardPrompt, TargetModel).SetVisible(false);
            _theftTakePrompt.AddListener(this);
            _theftTakeAllKeyboardPrompt.AddListener(this);

            _prompts.AddPrompt<VGenericPresenterPrompt>(_takePrompt, TargetModel).SetActive(false);
            _prompts.AddPrompt<VGenericPresenterPrompt>(_takeAllKeyboardPrompt, TargetModel).SetActive(false);
        }

        void ShowPrompts(bool isIllegal) {
            if (isIllegal == _wasIllegal) {
                return;
            }

            _theftTakePrompt.SetVisible(isIllegal);
            _theftTakeAllKeyboardPrompt.SetVisible(isIllegal);

            _takePrompt.SetVisible(!isIllegal);
            _takeAllKeyboardPrompt.SetVisible(!isIllegal);

            _wasIllegal = isIllegal;
        }

        void FocusItem() {
            _containerList.ScrollToItem(_currentSelected);
            _containerList.SetSelection(_currentSelected);
        }

        static class ContainerHoldUtil {
            const float MinPickpocketTime = 0.2f;
            const float MaxPickpocketTime = 5.0f;
            const float MinItemPrice = 10f;
            const float MaxItemPrice = 100f;
            const float MinItemPriceMultiplier = 0.5f;
            const float MaxItemPriceMultiplier = 2.0f;
            const float MinPickpocketRandomness = 0.85f;
            const float MaxPickpocketRandomness = 1.15f;

            static float TheftPromptHoldTime => Hero.Current.HeroStats.TheftHoldTimeModifier * ButtonsHandler.HoldTime * 3;
            static float PickpocketPromptHoldTimeBase => Hero.Current.HeroStats.PickpocketHoldTimeModifier;

            public static float CalculateHoldTime(ContainerUI target, Item theftItem) {
                if (Hero.Current.HasElement<ToolboxOverridesMarker>()) {
                    return 0;
                }

                Crime crime = target.FirstCrime;
                return crime.IsPickpocketing ? GetPickpocketHoldDuration(theftItem, target.ParentModel, crime) : TheftPromptHoldTime;
            }

            public static float CalculateTakeAllHoldTime(ContainerUI target, List<Item> theftItems) {
                if (Hero.Current.HasElement<ToolboxOverridesMarker>()) {
                    return 0;
                }

                Crime crime = target.FirstCrime;
                const float TakeAllBonus = 0.8f;
                if (crime.IsPickpocketing) {
                    if (theftItems.Count == 1) {
                        return GetPickpocketHoldDuration(theftItems[0], target.ParentModel, crime);
                    }

                    return theftItems.Sum(i => GetPickpocketHoldDuration(i, target.ParentModel, crime)) * TakeAllBonus;
                } else {
                    if (theftItems.Count == 1) {
                        return TheftPromptHoldTime;
                    }

                    return theftItems.Count * TheftPromptHoldTime * TakeAllBonus;
                }
            }

            static float GetPickpocketHoldDuration(Item item, Location loc, Crime crime) {
                float duration = PickpocketPromptHoldTimeBase;

                NpcTemplate npc = NpcTemplate.FromNpcOrDummy(loc);
                
                var crimeSettings = crime.Owners.PrimaryOwner;
                duration *= crimeSettings.ItemBounty(item.CrimeValue).pickpocketLengthMultiplier;
                duration *= crimeSettings.NpcBounty(npc.CrimeValue).pickpocketMultiplier;

                float itemValue = item.Price * item.Quantity;
                duration *= itemValue.Remap(MinItemPrice, MaxItemPrice, MinItemPriceMultiplier, MaxItemPriceMultiplier, true);

                duration *= Random.Range(MinPickpocketRandomness, MaxPickpocketRandomness);

                return Mathf.Clamp(duration, MinPickpocketTime, MaxPickpocketTime);
            }
        }
    }
}