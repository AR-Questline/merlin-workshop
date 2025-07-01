using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.SkinnedBones {
    public class AdditionalClothBonesCatalog : MonoBehaviour {
        public ClothStitcher.TransformCatalog CurrentCatalog { get; } = new ClothStitcher.TransformCatalog();

        public void CloneAndCatalog(ClothStitcher.TransformCatalog mainCatalog, out Transform createdAdditionalRoot) {
            CurrentCatalog.Clear();
            
            Transform root = ClothStitcher.DictionaryExtensions.Find(mainCatalog, transform.parent.name);
            Queue<BoneRootPair> toClone = new Queue<BoneRootPair>();
            toClone.Enqueue(new BoneRootPair{bone = transform, root = root});
            createdAdditionalRoot = null;
            
            while (toClone.Count > 0) {
                (Transform currentBone, Transform currentRoot) = toClone.Dequeue();

                GameObject boneInstance = new GameObject(currentBone.name);
                Transform boneInstanceTransform = boneInstance.transform;
                
                boneInstanceTransform.SetParent(currentRoot);
                currentBone.GetLocalPositionAndRotation(out var pos, out var rot);
                boneInstanceTransform.SetLocalPositionAndRotation(pos, rot);
                boneInstanceTransform.localScale = currentBone.localScale;

                createdAdditionalRoot ??= boneInstanceTransform;

                foreach (Transform childBone in currentBone) {
                    toClone.Enqueue(new BoneRootPair {bone = childBone, root = boneInstanceTransform});
                }
                
                CurrentCatalog.Catalog(boneInstanceTransform);
                mainCatalog.CatalogSingleAdditional(boneInstanceTransform);
                
                if (currentRoot == root) {
                    boneInstance.AddComponent<RuntimeAdditionalBonesRoot>();
                }
            }
        }

        public void Clear() {
            CurrentCatalog.Clear();
        }

        struct BoneRootPair {
            public Transform bone;
            public Transform root;
            
            public void Deconstruct(out Transform bone, out Transform root) {
                bone = this.bone;
                root = this.root;
            }
        }
    }
}