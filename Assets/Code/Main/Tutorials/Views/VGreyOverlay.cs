using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials.Views {
    [UsesPrefab("UI/Tutorials/VGreyOverlay")]
    [ExecuteAlways]
    public class VGreyOverlay : View<IModel> {
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();

        public void SetOverlay(Sprite sprite, bool withColor) {
            Image image = GetComponentInChildren<Image>();
            image.sprite = sprite;
            image.color = withColor ? Color.white : Color.black;
        }

        void Start() {
            // set to full screen, no matter where it is
            RectTransform rectTrans = (RectTransform) transform;
            rectTrans.position = GetComponentInParent<Canvas>().transform.position;
            rectTrans.anchorMin = Vector2.one * 0.5f;
            rectTrans.anchorMax = Vector2.one * 0.5f;
            float scaleFactor = GetComponentInParent<CanvasScaler>().scaleFactor;
            Vector2 size = GetComponentInParent<Canvas>().GetComponent<RectTransform>().sizeDelta;

            rectTrans.sizeDelta = size * scaleFactor;
        }
    }
}