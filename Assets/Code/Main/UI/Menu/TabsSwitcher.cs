using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu {
    public partial class TabsSwitcher : Element<IModel>, IUIAware {
        public sealed override bool IsNotSaved => true;

        List<ITab> _tabs;
        bool _carousel;
        bool _alternativeTabs;
        ITab _defaultTab;
        
        IEnumerable<ITab> Tabs => _tabs.Where(IsVisible);
        ITab Current => Tabs.FirstOrDefault(t => t.IsActive);
        ITab Previous => Tabs.PreviousItem(Current, _carousel);
        ITab Next => Tabs.NextItem(Current, _carousel);

        KeyBindings PreviousKey => _alternativeTabs ? KeyBindings.UI.Generic.PreviousAlt : KeyBindings.UI.Generic.Previous;
        KeyBindings NextKey => _alternativeTabs ? KeyBindings.UI.Generic.NextAlt : KeyBindings.UI.Generic.Next;

        public TabsSwitcher(IEnumerable<ITab> tabs, bool isCarousel = false, ITab defaultTab = null, bool alternativeTabs = false) {
            _tabs = tabs.ToList();
            _carousel = isCarousel;
            _defaultTab = defaultTab;
            _alternativeTabs = alternativeTabs;
        }

        protected override void OnInitialize() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, this, this));
        }

        protected override void OnFullyInitialized() {
            _defaultTab?.Select();
        }

        public UIResult Handle(UIEvent evt) {
            bool isDuringRebinding = World.Any<AllSettingsUI>()?.View<VNewKeyBinding>() ?? false;
            
            if (!RewiredHelper.IsGamepad || isDuringRebinding) {
                return UIResult.Ignore;
            }
            
            if (evt is UIKeyDownAction action) {
                if (action.Name == PreviousKey) {
                    Previous?.Select();
                    return UIResult.Accept;
                } else if (action.Name == NextKey) {
                    Next?.Select();
                    return UIResult.Accept;
                }
            }

            return UIResult.Ignore;
        }

        bool IsVisible(ITab tab) {
            if (tab == null) {
                return false;
            }
            if (tab is Component component) {
                return component.gameObject.activeInHierarchy;
            }

            return true;
        }
    }
}