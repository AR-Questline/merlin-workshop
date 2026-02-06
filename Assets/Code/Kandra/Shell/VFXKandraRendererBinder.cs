using System;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.Kandra.VFXs {
    public class VFXKandraRendererBinder : VFXBinderBase {
        public KandraRenderer kandraRenderer;
        protected ExposedProperty _property;

        public string Property {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool RequiresStitchingRebind => throw new NotImplementedException();

        protected override void OnEnable() {
            throw new NotImplementedException();
        }

        protected override void OnDisable() {
            throw new NotImplementedException();
        }

        public override bool IsValid(VisualEffect component) {
            throw new NotImplementedException();
        }

        public override void UpdateBinding(VisualEffect component) {
            throw new NotImplementedException();
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}