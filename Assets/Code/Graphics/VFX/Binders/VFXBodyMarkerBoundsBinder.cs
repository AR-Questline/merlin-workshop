using Awaken.Kandra.VFXs;
using UnityEngine;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Graphics.VFX.Binders {
    [AddComponentMenu("VFX/Property Binders/Character Body Kandra Renderer Bounds")]
    [VFXBinder("AR/Character Body Kandra Renderer Bounds")]
    public class VFXBodyMarkerBoundsBinder : VFXKandraRendererBoundsBinder {
        VFXBodyMarker _body;

        public void SetBody(VFXBodyMarker newBody) {
            if (enabled && _body) {
                _body.MarkBeingUnused();
            }
            _body = newBody;
            if (enabled && _body) {
                _body.MarkBeingUsed();
                kandraRenderer = _body.Renderer;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            _body = GetVfxBody();
            if (_body) {
                _body.MarkBeingUsed();
                kandraRenderer = _body.Renderer;
            }
        }

        protected override void OnDisable() {
            if (_body) {
                _body.MarkBeingUnused();
            }
            _body = null;
            kandraRenderer = null;
            base.OnDisable();
        }

        VFXBodyMarker GetVfxBody() {
            var searchTransform = GetComponentInParent<Animator>()?.transform ?? transform.parent;
            return searchTransform?.GetComponentInChildren<VFXBodyMarker>();
        }
    }
}
