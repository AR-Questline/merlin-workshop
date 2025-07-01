using System;
using UnityEngine;

namespace Awaken.Utility.Maths {
    [Serializable]
    public class RectTransformData {
        public Vector3 localPosition;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector3 scale;
        public Quaternion rotation = Quaternion.identity;
 
        public void PullFromTransform(RectTransform transform) {
            this.localPosition = transform.localPosition;
            this.anchorMin = transform.anchorMin;
            this.anchorMax = transform.anchorMax;
            this.pivot = transform.pivot;
            this.anchoredPosition = transform.anchoredPosition;
            this.sizeDelta = transform.sizeDelta;
            this.rotation = transform.localRotation;
            this.scale = transform.localScale;
        }
 
        public void PushToTransform(RectTransform transform) {
            transform.localPosition = this.localPosition;
            transform.anchorMin = this.anchorMin;
            transform.anchorMax = this.anchorMax;
            transform.pivot = this.pivot;
            transform.anchoredPosition = this.anchoredPosition;
            transform.sizeDelta = this.sizeDelta;
            transform.localRotation = this.rotation;
            transform.localScale = this.scale;
        }

        public static void ClearRectTransform_Stretch(RectTransform transform) {
            transform.localPosition = default;
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.pivot = new Vector2(0.5f, 0.5f);
            transform.anchoredPosition = default;
            transform.sizeDelta = default;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}