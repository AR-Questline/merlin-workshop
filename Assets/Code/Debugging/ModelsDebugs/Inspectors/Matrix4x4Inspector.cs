using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class Matrix4X4Inspector : MemberListItemInspector<Matrix4x4> {

        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var matrixValue = CastedValue(value);

            var position = matrixValue.ExtractPosition();
            var rotation = matrixValue.ExtractRotation();
            var rotationVector = new Vector4(rotation.x, rotation.y, rotation.z, rotation.z);
            var scale = matrixValue.ExtractScale();

            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                GUILayout.Label($"{member.Name} [{nameof(Matrix4x4)}]");
                TGGUILayout.BeginIndent();
                var resultPos = TGGUILayout.Vector3Field("", position);
                var resultRot = TGGUILayout.Vector4Field("", rotationVector);
                GUILayout.Label(rotation.eulerAngles.ToString());
                var resultScale = TGGUILayout.Vector3Field("", scale);
                TGGUILayout.EndIndent();
                if (checkScope) {
                    var result = Matrix4x4.TRS(
                        resultPos,
                        new Quaternion(resultRot.x, resultRot.y, resultRot.z, resultRot.w),
                        resultScale
                    );
                    member.SetValue(target, result);
                }
            }
        }
    }
}