using System;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    [Serializable]
    public class IconRenderingSettings {
        public GameObject prefab;
        public bool useCustomOffset;
        public TransformValues customTransformOffset = new () {
            position = Vector3.zero,
            rotation = Vector3.zero,
            scale = 0,
        };
    }
}