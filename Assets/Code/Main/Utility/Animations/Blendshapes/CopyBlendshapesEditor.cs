using System;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.Blendshapes {
    [ExecuteAlways]
    public class CopyBlendshapesEditor : CopyBlendshapes {
        void OnEnable() {
            RefreshParent();
            RefreshBlendShapesMapping();
        }
        
        void LateUpdate() {
            if (CanProcess) {
                Process();
            }
        }
    }
}
