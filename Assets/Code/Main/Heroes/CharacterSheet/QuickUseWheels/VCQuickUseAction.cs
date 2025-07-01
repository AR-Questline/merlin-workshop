using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Sources;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public abstract class VCQuickUseAction : VCQuickUseOption, IUIAware {
        [SerializeField] protected GameObject previewObject;
        [RichEnumExtends(typeof(KeyBindings.UI), typeof(KeyBindings.Gamepad)), RichEnumSearchBox]
        [SerializeField] RichEnumReference keyBinding;

        KeyBindings KeyBinding => keyBinding.EnumAs<KeyBindings>();
        
        protected override void OnAttach() {
            base.OnAttach();

            if (KeyBinding is not null) {
                World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.All, this, Target));
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (RewiredHelper.IsGamepad && evt is UIKeyAction action) {
                if (action is UIKeyDownAction) {
                    if (action.Name == KeyBinding) {
                        OnHoverStart();
                        return UIResult.Accept;
                    }
                }
                
                if (action is UIKeyUpAction && action.Name == KeyBinding) {
                    OnHoverEnd();
                    OnSelect(false);
                    return UIResult.Accept;
                }
            }
            return UIResult.Ignore;
        }
    }
}