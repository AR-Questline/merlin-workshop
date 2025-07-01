using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose {
    [UsesPrefab("CharacterSheet/Equipment/Choose/" + nameof(VItemChooseUI))]
    public class VItemChooseUI : View<IItemChooseUI>, IFocusSource {
        [SerializeField] Transform itemsHost;

        public Transform ItemsHost => itemsHost;
        public bool ForceFocus => true;
        public Component DefaultFocus => this;
    }
}