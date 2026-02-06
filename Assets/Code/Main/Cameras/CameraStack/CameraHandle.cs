using System;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.CameraStack {
    /// <summary>
    /// Always contains a reference to current main camera
    /// </summary>
    public class CameraHandle {
        public Camera Camera { get; private set; }

        public CameraHandle() {
        }

        public void ChangeCamera(Camera cam) {
            Camera = cam;
        }

        public static implicit operator Camera(CameraHandle handle) => handle.Camera;
    }
}