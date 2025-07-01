using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.UI.Components.Navigation;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts {
    public class VCLoadout : ViewComponent<LoadoutsUI> {
        [SerializeField] int loadoutIndex;
        [SerializeField] ButtonConfig select;
        [SerializeField] GameObject selectedOnButton;
        [SerializeField] VCLoadoutSlot main;
        [SerializeField] VCLoadoutSlot secondary;
        [SerializeField] CanvasGroup targetCanvasGroup;
        
        [Space(10f)] 
        [SerializeField] ExplicitComponentNavigation navigation;
        
        public int LoadoutIndex => loadoutIndex;
        public bool IsRanged => Target.HeroItems.LoadoutAt(LoadoutIndex).IsRanged;

        [UnityEngine.Scripting.Preserve] bool Selected => selectedOnButton.activeSelf;
        
        protected override void OnAttach() {
            var heroItems = Target.HeroItems;
            main.Init(this);
            secondary.Init(this);
            
            select.InitializeButton(OnSelect);
            select.button.OnHover += OnHover;
            select.button.OnEvent += HandleButtonNavi;
            
            heroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, OnLoadoutChanged, this);
            OnLoadoutChanged(new Change<int>(heroItems.CurrentLoadoutIndex, heroItems.CurrentLoadoutIndex));
        }

        public void SetIgnoreParentCanvasGroupsState(bool state) {
            targetCanvasGroup.ignoreParentGroups = state;
        }

        UIResult HandleButtonNavi(UIEvent evt) {
            return navigation.TryHandle(evt, out var result) ? result : UIResult.Ignore;
        }

        void OnSelect() {
            Target.SelectLoadout(LoadoutIndex);
        }

        void OnHover(bool active) {
            if (Target is { HasBeenDiscarded: true }) {
                return;
            }
            
            Target.OnLoadoutHoverChange(active);
        }

        void OnLoadoutChanged(Change<int> change) {
            select.SetSelection(change.to == LoadoutIndex);
            selectedOnButton.SetActive(change.to == LoadoutIndex);
        }
    }
}