using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.CameraStack {
    public class CameraState {
        public Camera Camera { get; }
        public IModel Owner { get; }
        public bool Additive { get; }

        public CameraState(Camera cam, IModel owner, bool additive = false) {
            Camera = cam;
            Owner = owner;
            Additive = additive;
        }
    }
}