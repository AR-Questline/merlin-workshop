using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    [RequireComponent(typeof(KeyIcon))]
    public class VCStaticKeyIcon : ViewComponent {

        [SerializeField, RichEnumExtends(typeof(KeyBindings))] RichEnumReference key;
        [SerializeField] bool hold;
        
        protected override void OnAttach() {
            GetComponent<KeyIcon>().Setup(new KeyIcon.Data(key.EnumAs<KeyBindings>(), hold), ParentView);
        }
    }
}