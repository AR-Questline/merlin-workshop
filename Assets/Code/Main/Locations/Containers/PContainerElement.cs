using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.UIToolkit.CustomControls;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Containers {
    public class PContainerElement : Presenter<ContainerUI>, IPromptListener {
        BetterOutlinedLabel _nameLabel;
        BetterOutlinedLabel _quantityLabel;
        VisualElement _selectedImage;
        VisualElement _betterIcon;
        VisualItemIcon _icon;
        VisualElement _theftAlert;
        VisualFillBar _theftFillBar;

        Prompt _theftTakePrompt;
        ContainerUI _container;
        IEventListener _alertListener;

        bool WillBeIllegal => _crime.IsCrime();
        int _elementIndex;
        Crime _crime;

        readonly Color _theftAlertColor = new (1f, 1f, 1f, 0);
        readonly Vector3 _theftAlertScale = new (0f, 1f, 1f);

        public PContainerElement(VisualElement parent) : base(parent) { }
        
        protected override void CacheVisualElements(VisualElement contentRoot) {
            _nameLabel = contentRoot.Q<BetterOutlinedLabel>("title-label");
            _selectedImage = contentRoot.Q<VisualElement>("selected");
            _quantityLabel = contentRoot.Q<BetterOutlinedLabel>("quantity-label");
            _betterIcon = contentRoot.Q<VisualElement>("better-item-icon");
            _theftAlert = contentRoot.Q<VisualElement>("theft-alert");

            _icon = new VisualItemIcon(contentRoot.Q<VisualElement>("item-icon"));
            _theftFillBar = new VisualFillBar(contentRoot.Q<VisualElement>("theft-fill-bar")).Set(VisualFillBarType.Horizontal, _theftAlertScale);
            _selectedImage.SetActiveOptimized(false);
        }

        protected override void DiscardInternal() {
            Deselect();
        }
        
        public void SetData(int index, Item item, Prompt theftTakePrompt) {
            _elementIndex = index;
            _container = TargetModel;
            _theftTakePrompt = theftTakePrompt;
            
            {
                Location ownerLocation = TargetModel.ParentModel;
                NpcTemplate npcTemplate = NpcTemplate.FromNpcOrDummy(ownerLocation);

                _crime = npcTemplate != null
                             ? Crime.Pickpocket(item, npcTemplate.CrimeValue, ownerLocation)
                             : Crime.Theft(item, ownerLocation);
            }
            
            _nameLabel.text = item.DisplayName;
            _nameLabel.SetTextColor(item.Quality.NameColor);
            _betterIcon.SetActiveOptimized(item.IsGearBetterThanEquipped() > 0);

            bool moreThanOne = item.Quantity > 1;
            _quantityLabel.SetActiveOptimized(moreThanOne);
            _quantityLabel.text = moreThanOne ? $"{item.Quantity}" : string.Empty;
            
            var iconReference = item.Template?.IconReference;

            if (iconReference?.IsSet ?? false) {
                _icon.Set(iconReference, this, item.Quality.BgColor.Color, WillBeIllegal);
            }
        }
        
        public void OnHoldKeyDown(Prompt source) {
            if (!WillBeIllegal) return;
            ResetPickpocketAlert(true);
        }

        public void OnHoldKeyHeld(Prompt source, float percent) {
            if (!WillBeIllegal) return;
            _theftFillBar.Fill(percent);
        }

        public void OnHoldKeyUp(Prompt source, bool completed) {
            ResetPickpocketAlert(false);
        }
        
        public void SelectCallback(IEnumerable<int> selectedIndices) {
            if (selectedIndices.Contains(_elementIndex)) {
                this.Select();
            } else {
                this.Deselect();
            }
        }

        public void SetName(string name) { }
        public void SetActive(bool active) { }
        public void SetVisible(bool visible) { }
        
        void Select() {
            _selectedImage.SetActiveOptimized(true);
            
            if (!WillBeIllegal) return;
            _theftTakePrompt.AddListener(this);
        }

        void Deselect() {
            _selectedImage.SetActiveOptimized(false);
            
            if (!WillBeIllegal) return;
            _theftTakePrompt.RemoveListener(this);
        }
        
        void ResetPickpocketAlert(bool keyDown) {
            if (!WillBeIllegal) return;
            World.EventSystem.TryDisposeListener(ref _alertListener);
            
            if (keyDown) {
                _alertListener = _container.ParentModel.ListenTo(NpcCrimeReactions.Events.PickpocketAlertChange, RefreshPickpocketAlert);
                _theftAlert.SetBackgroundTintColor(_theftAlertColor);
                _theftFillBar.Reset();
            }

            _theftFillBar.Content.SetActiveOptimized(keyDown);
            _theftAlert.SetActiveOptimized(keyDown);
        }
        
        void RefreshPickpocketAlert(float value) {
            float theftAlertAlpha = value.Remap(0.6f, 0.85f, 0f, 1f, true);
            _theftAlert.SetBackgroundTintColor(new Color(1f, 1f, 1f, theftAlertAlpha));
        }
    }
}