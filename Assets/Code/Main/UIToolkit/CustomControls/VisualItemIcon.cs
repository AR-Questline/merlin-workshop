using Awaken.TG.Assets;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.CustomControls {
    public class VisualItemIcon : IVisualPresenter {
        public VisualElement Content { get; }

        VisualElement _qualityBlend;
        VisualElement _itemIcon;
        VisualElement _theftIcon;
        
        public VisualItemIcon(VisualElement contentRoot) {
            Content = contentRoot;
            CacheElements();
        }
        
        public VisualItemIcon Set(ShareableSpriteReference iconReference, IReleasableOwner owner, Color qualityColor, bool isTheft) {
            _qualityBlend.SetBackgroundTintColor(qualityColor);
            _theftIcon.SetActiveOptimized(isTheft);
            
            iconReference.RegisterAndSetup(owner, _itemIcon);
            return this;
        }

        public VisualItemIcon Set(ShareableSpriteReference iconReference, IReleasableOwner owner) {
            return Set(iconReference, owner, Color.white, false);
        }
        
        void CacheElements( ) {
            _qualityBlend = Content.Q<VisualElement>("quality-blend");
            _itemIcon = Content.Q<VisualElement>("icon");
            _theftIcon = Content.Q<VisualElement>("theft-icon");
        }
    }
}