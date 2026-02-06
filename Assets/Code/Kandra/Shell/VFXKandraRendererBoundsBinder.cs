using System;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.Kandra.VFXs {
    public class VFXKandraRendererBoundsBinder : VFXBinderBase {
        public KandraRenderer kandraRenderer;
        protected ExposedProperty _boundsProperty;

        public string Property {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        protected override void OnEnable() {
            throw new NotImplementedException();
        }

        public override bool IsValid(VisualEffect component) {
            throw new NotImplementedException();
        }

        public override void UpdateBinding(VisualEffect component) {
            throw new NotImplementedException();
        }
    }
}