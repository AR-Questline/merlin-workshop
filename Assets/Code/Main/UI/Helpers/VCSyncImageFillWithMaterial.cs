using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Helpers {
    public class VCSyncImageFillWithMaterial : ViewComponent, UnityUpdateProvider.IWithUpdateGeneric {
        static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
        [SerializeField, Required] Image image;
        Material _material;

        bool _registered;
        
        void Awake() {
            _material = new Material(image.material);
            image.material = _material;
        }
        
        public void RegisterUpdate() {
            if (_registered) return;
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
            _registered = true;
        }

        public void UnregisterUpdate() {
            if (!_registered) return;
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            _registered = false;
        }

        public void UnityUpdate() {
            Sync();
        }
        
        public void Sync() {
            _material.SetFloat(FillAmountID, image.fillAmount);
        }

        protected override void OnDiscard() {
            UnregisterUpdate();
            
            if (_material != null) {
                Destroy(_material);
                _material = null;
            }
            
            base.OnDiscard();
        }
    }
}
