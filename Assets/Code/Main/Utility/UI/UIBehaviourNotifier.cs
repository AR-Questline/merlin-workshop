using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Provides a way to listen to UIBehaviour events of the RectTransform.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIBehaviourNotifier : UIBehaviour {
        public event Action<RectTransform> RectTransformDimensionsChanged = delegate { };
        
        public RectTransform RectTransform => _rect = _rect != null ? _rect : GetComponent<RectTransform>();
        RectTransform _rect;
        
        protected override void OnRectTransformDimensionsChange() {
            RectTransformDimensionsChanged?.Invoke(RectTransform);
        }

        protected override void OnDestroy() {
            RectTransformDimensionsChanged = null;
        }
    }
}