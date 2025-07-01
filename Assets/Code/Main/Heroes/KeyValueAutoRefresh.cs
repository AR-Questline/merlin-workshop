using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class KeyValueAutoRefresh : Element<Model> {
        public sealed override bool IsNotSaved => true;

        KeyBindings _keyBind;
        int _frameSinceLastUpdate;
        bool _isHeld;
        bool _isAllowed = true;

        public static implicit operator bool(KeyValueAutoRefresh keyValueAutoRefresh) => keyValueAutoRefresh._isHeld;

        protected override void OnInitialize() {
            ParentModel.GetOrCreateTimeDependent().WithLateUpdate(ProcessLateUpdate);
        }

        public void UpdateValue(bool value) {
            if (!_isAllowed) {
                value = false;
            }
            _isHeld = value;
            _frameSinceLastUpdate = Time.frameCount;
        }

        public void SetAllowed(bool allowed) {
            _isAllowed = allowed;
            if (!_isAllowed) {
                _isHeld = false;
            }
        }

        void ProcessLateUpdate(float deltaTime) {
            if (_isHeld && _frameSinceLastUpdate + PlayerInput.FramesProlongedInput < Time.frameCount) {
                _isHeld = false;
            }
        }

        public void Reset() {
            _isHeld = false;
            _frameSinceLastUpdate = 0;
        }
    }
}
