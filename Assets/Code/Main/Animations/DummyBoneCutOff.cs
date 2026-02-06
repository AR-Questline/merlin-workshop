using System.Collections.Generic;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Animations {
    public abstract partial class DummyBoneCutOff : Element<NpcDummy> {
        static readonly Vector3 AlmostZeroScale = Vector3.one * 0.0001f;
        
        protected override void OnInitialize() {
            ParentModel.OnCompletelyInitialized(_ => Init());
        }

        void Init() {
            if (!TryStartInit()) {
                Discard();
                return;
            }
            
            var ragdollController = ParentModel.ParentTransform.GetComponentInChildren<RagdollController>();
            foreach (var bone in GetBones()) {
                BeforeInitBone(ragdollController, bone);
            }
            OnInit();
            ApplyAnimPP();
            foreach (var bone in GetBones()) {
                AfterInitBone(bone);
            }
        }
        
        protected abstract IEnumerable<Transform> GetBones();
        protected abstract bool TryStartInit();

        protected virtual void BeforeInitBone(RagdollController ragdollController, Transform bone) {
            ragdollController.RemoveBoneFromRagdoll(bone);
        }

        protected virtual void AfterInitBone(Transform bone) {
            bone.localScale = AlmostZeroScale;
        }
        
        protected virtual void OnInit() { }
        
        void ApplyAnimPP() {
            var animPP = ParentModel.ParentTransform.GetComponentInChildren<AnimationPostProcessing>();
            if (animPP != null) {
                animPP.ChangeAdditionalEntries(GetAdditionalEntries());
            }
        }
        
        protected abstract AnimationPostProcessing.Entry[] GetAdditionalEntries();
    }
}