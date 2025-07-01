using Awaken.TG.Main.Heroes.Sketching;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    [SpawnsView(typeof(VSketchPopupUI))]
    public partial class SketchPopupUI : Model {
        Sketch _sketch;
        Texture2D _texture2D;

        public Sketch Sketch => _sketch;
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        public SketchPopupUI(Sketch sketch) {
            _sketch = sketch;
        }

        public Texture2D GetSketchTexture() {
            _texture2D = HeroSketches.LoadSketch(_sketch.SketchIndex);
            return _texture2D;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_texture2D != null) {
                Object.Destroy(_texture2D);
                _texture2D = null;
            }
        }
    }
}
