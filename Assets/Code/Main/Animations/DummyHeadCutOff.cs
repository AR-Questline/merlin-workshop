using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public partial class DummyHeadCutOff : Element<NpcDummy> {
        static readonly Vector3 AlmostZeroScale = Vector3.one * 0.0001f;
        
        public override ushort TypeForSerialization => SavedModels.DummyHeadCutOff;

        Transform _neck;
        
        protected override void OnInitialize() {
            ParentModel.OnCompletelyInitialized(_ => Init());
        }

        void Init() {
            _neck = ParentModel.Neck;
            if (_neck == null) {
                Discard();
                return;
            }
            
            RagdollUtilities.RemoveRagdollPermanently(_neck);
            RemoveFacialMeshFeatures();
            ApplyAnimPP();
            _neck.localScale = AlmostZeroScale;
        }
        
        void RemoveFacialMeshFeatures() {
            if (ParentModel.TryGetElement(out BodyFeatures bodyFeatures)) {
                bodyFeatures.Hair = null;
                bodyFeatures.Beard = null;
            }
        }
        
        void ApplyAnimPP() {
            var animPP = ParentModel.ParentTransform.GetComponentInChildren<AnimationPostProcessing>();
            if (animPP != null) {
                animPP.ChangeAdditionalEntries(new[] { new AnimationPostProcessing.Entry(CommonReferences.Get.dummyNoHeadPP) });
            }
        }
    }
}