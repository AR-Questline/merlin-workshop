using System;
#if UNITY_EDITOR
using UnityEditor.Toolbars;
#endif

namespace Awaken.ECS.Editor.DrakeRenderer {
    public class DrakeHackToolbarButton
#if UNITY_EDITOR
        : EditorToolbarButton
#endif
    {
        public static event Action SceneAuthoringHackChanged;

        public static bool SceneAuthoringHack {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public DrakeHackToolbarButton() {
            throw new NotImplementedException();
        }
    }
}