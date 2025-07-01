using Awaken.TG.Main.Rendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.UI.ObjectCloseup {
    [UsesPrefab("VObjectCloseup")]
    public class VObjectCloseup : View<Model> {
        public Transform position;
        public Camera closeupCamera;
        
        public RenderTexture Texture { get; private set; }

        public void Init(int width, int height) {
            Texture = RenderTexture.GetTemporary(width, height);
            closeupCamera.targetTexture = Texture;
            closeupCamera.enabled = true;
        }

        public void PlaceObject(Transform transform) {
            transform.SetParent(position);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            // foreach (Transform trans in transform.GetComponentsInChildren<Transform>()) {
            //     trans.gameObject.layer = RenderLayers.OverUI;
            // }
            InitializeViewComponents(transform);
        }

        protected override IBackgroundTask OnDiscard() {
            if (Texture != null) {
                Texture.Release();
            }
            return null;
        }
    }
}
