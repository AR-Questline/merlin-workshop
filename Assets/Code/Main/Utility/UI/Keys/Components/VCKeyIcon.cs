using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Utility.UI.Keys.Components {
    [RequireComponent(typeof(KeyIcon))]
    public abstract class VCKeyIcon<TKeySelector> : ViewComponent<IKeyProvider<TKeySelector>> {
        [SerializeField] TKeySelector key;
        
        protected override void OnAttach() {
            var keyData = Target.GetKey(key);
            GetComponent<KeyIcon>().Setup(keyData, ParentView);
        }
    }

    public interface IKeyProvider<in TKeySelector> : IModel {
        KeyIcon.Data GetKey(TKeySelector key);
    }
}