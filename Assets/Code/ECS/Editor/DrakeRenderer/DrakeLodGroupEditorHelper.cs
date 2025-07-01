using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeLodGroupEditorHelper {
        public static DrakeLodGroupState GetDrakeLodGroupState(DrakeLodGroup drakeLodGroup, out LODGroup lodGroup) {
            lodGroup = drakeLodGroup.GetComponent<LODGroup>();
            if (!drakeLodGroup.IsBaked) {
                return DrakeLodGroupState.NotBaked;
            } else if (lodGroup && drakeLodGroup.IsBaked) {
                return DrakeLodGroupState.BakedButUnityPresent;
            } else if (!lodGroup && drakeLodGroup.IsBaked) {
                return DrakeLodGroupState.CorrectlyBaked;
            }

            return DrakeLodGroupState.UnknownState;
        }
    }

    public enum DrakeLodGroupState : byte {
        NotBaked,
        CorrectlyBaked,
        BakedButUnityPresent,
        UnknownState,
    }
}
