using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    public static class EmptyAnimationClip {
        [MenuItem ("CONTEXT/AnimationClip/Empty")]
        static void DoubleMass (MenuCommand command) {
            AnimationClip clip = (AnimationClip)command.context;
            AnimationCurve curve = AnimationCurve.Linear(0.0F, 1.0F, 0.017F, 1.0F);
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "DummyAnimationClip");
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            EditorUtility.SetDirty(clip);
        }
    }
}