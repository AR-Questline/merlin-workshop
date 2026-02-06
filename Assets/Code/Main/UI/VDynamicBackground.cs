using Awaken.TG.Assets;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI {
    [UsesPrefab("UI/" + nameof(VDynamicBackground))]
    public class VDynamicBackground : View<DynamicBackground> {
        [SerializeField] RectTransform backgroundContainer;
        [SerializeField] Image backgroundImage;
        [SerializeField, UIAssetReference] SpriteReference backgroundSpriteRef;

        public override Transform DetermineHost() => Target.TransformHost;
        
        protected override void OnInitialize() {
            SetupSprite();
        }

        protected override void OnMount() {
            backgroundContainer.SetAsFirstSibling();
        }

        void SetupSprite() {
            backgroundContainer.StretchToParent();

            if(backgroundSpriteRef.IsSet) {
                backgroundSpriteRef.RegisterAndSetup(this, backgroundImage);
            }
        }
    }
}