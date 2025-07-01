using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Blendshapes {
    [AddComponentMenu("")] // for not being able to add it from AddComponent menu
    public class CopyBlendshapesRuntime : CopyBlendshapes {
        [ShowInInspector, ReadOnly] bool _registered;

        void Start() {
            RefreshParent();
            RefreshBlendShapesMapping();
            RefreshUpdate();
        }

        void OnEnable() {
            RefreshUpdate();
        }

        void OnDisable() {
            RefreshUpdate();
        }

        public void NotifyMeshChanged() {
            RefreshBlendShapesMapping();
            RefreshUpdate();
        }
        
        public void UnityLateUpdate() {
            Process();
        }
        
        void RefreshUpdate() {
            bool shouldBeRegistered = isActiveAndEnabled && CanProcess;
            if (shouldBeRegistered && !_registered) {
                _registered = true;
                UnityUpdateProvider.GetOrCreate().RegisterCopyBlendshapes(this);
            } else if (!shouldBeRegistered && _registered) {
                _registered = false;
                UnityUpdateProvider.GetOrCreate().UnregisterCopyBlendshapes(this);
            }
        }
    }
}