using System;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Cameras.CameraStack {
    /// <summary>
    /// Always contains a reference to current main camera
    /// </summary>
    public class CameraHandle {
        public Camera Camera { get; private set; }
        public IModel Owner { get; private set; }
        Action<Camera> OnChange { get; }

        public CameraHandle(IModel owner = null, Action<Camera> onChange = null) {
            Owner = owner;
            OnChange = onChange;
        }

        public void ChangeCamera(Camera cam) {
            Camera = cam;
            OnChange?.Invoke(Camera);
        }

        public static implicit operator Camera(CameraHandle handle) => handle.Camera;
    }
}