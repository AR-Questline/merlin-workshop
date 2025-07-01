using System;
using Awaken.Utility.Debugging;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Utility.UI.Layouts {
    [RequireComponent(typeof(LayoutElement))]
    public class InterpolatedLayoutAnchor : MonoBehaviour {
        [SerializeField] LayoutElement self;
        [SerializeField] RectTransform target;
        [Space(10f)] 
        [SerializeField] bool vertical;
        [SerializeField] bool horizontal;
        [Space(10f)]
        [SerializeField, Range(0, 1)] float weight;

        public event Action LayoutChanged;

        float GetWeight() {
            return weight;
        }
        
        public void SetWeight(float weight) {
            this.weight = weight;
            Refresh();
        }

        void Update() {
            if (weight == 1) {
                Refresh();
            }
        }

        void Refresh() {
            Vector2 targetSize = target.sizeDelta;

            bool changed = false;
            
            if (horizontal) {
                var width = Mathf.Lerp(self.minWidth, targetSize.x, weight);
                if (width != self.preferredWidth) {
                    self.preferredWidth = width;
                    changed = true;
                }
            }
            
            if (vertical) {
                var height =Mathf.Lerp(self.minHeight, targetSize.y, weight);
                if (height != self.preferredHeight) {
                    self.preferredHeight = height;
                    changed = true;
                }
            }

            if (changed) {
                LayoutChanged?.Invoke();
            }
        }
        
        public Tween TweenWeight(float weight, float time) {
            return DOTween.To(GetWeight, SetWeight, weight, time);
        }

        void OnValidate() {
            var selfRect = transform as RectTransform;

            if (selfRect == null) {
                Log.Important?.Error("[InterpolatedLayoutElement] InterpolatedLayoutElement must be component of UI", this);
            } else {
                self ??= GetComponent<LayoutElement>();

                if (self.minWidth < 0) {
                    self.minWidth = 0;
                }

                if (self.minHeight < 0) {
                    self.minHeight = 0;
                }

                if (self.preferredWidth < self.minWidth) {
                    self.preferredWidth = self.minWidth;
                }

                if (self.preferredHeight < self.minHeight) {
                    self.preferredHeight = self.minHeight;
                }
                
                if (target != null) {
                    if (target.anchorMin != target.anchorMax) {
                        Log.Important?.Error("[InterpolatedLayoutElement] target.anchorMin must be equal to target.anchorMax", this);
                    }

                    if (selfRect.pivot != target.pivot) {
                        Log.Important?.Error("[InterpolatedLayoutElement] target.anchorMin must be equal to target.anchorMax", this);
                    }

                    if (target.anchoredPosition != Vector2.zero) {
                        Log.Important?.Error("[InterpolatedLayoutElement] target.anchoredPosition must be equal to Vector2.zero", this);
                    }
                }

                SetWeight(weight);
            }
        }
    }
}