using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.Utility.Animations {
    [CreateAssetMenu(fileName = "RagdollSetupSO", menuName = "Awaken/Ragdoll Setup")]
    public class RagdollSetupSO : ScriptableObject {
        static readonly RagdollBoneConfig DummyConfig = new RagdollBoneConfig();

        public float wholeMass;
        public uint rigidBodyCount;
        public RagdollBoneConfig[] boneConfigs;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
        public string[] boneNames;
#endif

        public ref readonly RagdollBoneConfig GetBoneConfig(int index, string boneName, RagdollController debugContex) {
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
            if (boneNames[index] != boneName) {
                Log.Critical?.Error($"Bone name '{boneName}' not found in boneNames array for index {index} for {debugContex}", debugContex);
                return ref DummyConfig;
            }
#endif
            return ref boneConfigs[index];
        }
    }
}

