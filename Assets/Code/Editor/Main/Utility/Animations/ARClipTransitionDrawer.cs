using Animancer;
using Animancer.Editor;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Utility.Animations {
    [CustomPropertyDrawer(typeof(ARClipTransition), true)]
    public class ARClipTransitionDrawer : TransitionDrawer
    {
        public ARClipTransitionDrawer() : base(ClipTransition.ClipFieldName) { }
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label) {
            property.GetValue<ARClipTransition>()?.ValidateAnimationClipProperty();
            // if (!UseSimplifiedDrawer && GUILayout.Button("Sample Root Horizontal Rotation")) {
            //     property.GetValue<ARClipTransition>()?.SampleRootHorizontalRotation();
            // }
            base.OnGUI(area, property, label);
        }
    }
}