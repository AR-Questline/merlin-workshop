using System.Collections.Generic;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class DummyHeadCutOff : DummyBoneCutOff {
        public override ushort TypeForSerialization => SavedModels.DummyHeadCutOff;

        Transform _neck;
        
        protected override bool TryStartInit() {
            _neck = ParentModel.Neck;
            return _neck != null;
        }
        
        protected override IEnumerable<Transform> GetBones() {
            yield return _neck;
        }

        protected override void OnInit() {
            RemoveFacialMeshFeatures();
        }

        void RemoveFacialMeshFeatures() {
            if (ParentModel.TryGetElement(out BodyFeatures bodyFeatures)) {
                bodyFeatures.Hair = null;
                bodyFeatures.Beard = null;
            }
        }

        protected override AnimationPostProcessing.Entry[] GetAdditionalEntries() {
            return new[] { new AnimationPostProcessing.Entry(CommonReferences.Get.dummyNoHeadPP) };
        }
    }
}