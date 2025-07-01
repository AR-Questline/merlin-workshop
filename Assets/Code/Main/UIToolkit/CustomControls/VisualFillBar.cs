using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit.CustomControls {
    public class VisualFillBar : IVisualPresenter {
        public VisualElement Content { get; }

        VisualElement _fillBar;
        VisualFillBarType _type;
        Vector3 _defaultScale = Vector3.zero;
        
        public VisualFillBar(VisualElement contentRoot) {
            Content = contentRoot;
            CacheElements();
        }
        
        public void CacheElements() {
            _fillBar = Content.Q<VisualElement>("fill-background");
        }
        
        [UnityEngine.Scripting.Preserve]
        public VisualFillBar Set(VisualFillBarType type) {
            _type = type;
            return this;
        }
        
        public VisualFillBar Set(VisualFillBarType type, Vector3 defaultScale) {
            _type = type;
            _defaultScale = defaultScale;
            Reset();
            return this;
        }

        public void Fill(float percent) {
            if (_type is VisualFillBarType.HorizontalReversed or VisualFillBarType.VerticalReversed) {
                percent = 1f - percent;
            }

            _fillBar.transform.scale = _type switch {
                VisualFillBarType.Horizontal or VisualFillBarType.HorizontalReversed => new Vector3(percent, 1f, 1f),
                VisualFillBarType.Vertical or VisualFillBarType.VerticalReversed => new Vector3(1f, percent, 1f),
                _ => _fillBar.transform.scale
            };
        }
        
        public void Reset() {
            _fillBar.transform.scale = _defaultScale;
        }
    }
    
    public enum VisualFillBarType {
        Horizontal,
        Vertical,
        HorizontalReversed,
        VerticalReversed
    }
}