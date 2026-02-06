using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.UI {
    [SpawnsView(typeof(VDynamicBackground))]
    public class DynamicBackground : Element {
        public override bool IsNotSaved => true;
        
        VDynamicBackground View => View<VDynamicBackground>();
        public RectTransform TransformHost { get; private set; }

        public DynamicBackground(RectTransform target) {
            TransformHost = target;
        }
    }
}