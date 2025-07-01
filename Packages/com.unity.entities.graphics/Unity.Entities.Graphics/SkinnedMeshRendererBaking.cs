using System.Collections.Generic;
using Unity.Assertions;
using Unity.Deformations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    [TemporaryBakingType]
    struct SkinnedMeshRendererBakingData : IComponentData
    {
        public UnityObjectRef<SkinnedMeshRenderer> SkinnedMeshRenderer;
    }

    class SkinnedMeshRendererBaker : Baker<SkinnedMeshRenderer>
    {
        static int s_SkinMatrixIndexProperty = Shader.PropertyToID("_SkinMatrixIndex");
        static int s_ComputeMeshIndexProperty = Shader.PropertyToID("_ComputeMeshIndex");

#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
        static int s_DOTSDeformedProperty = Shader.PropertyToID("_DotsDeformationParams");
#endif

        public override void Bake(SkinnedMeshRenderer authoring)
        {
            var materials = new List<Material>();
            authoring.GetSharedMaterials(materials);

            foreach (var material in materials)
            {
                if (material == null)
                    continue;

                var supportsSkinning = material.HasProperty(s_SkinMatrixIndexProperty)
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                                       || material.HasProperty(s_DOTSDeformedProperty)
#endif
                                       || material.HasProperty(s_ComputeMeshIndexProperty);
                if (!supportsSkinning)
                {
                    string errorMsg = "";
                    errorMsg +=
                        $"Shader [{material.shader.name}] on [{authoring.name}] does not support skinning. This can result in incorrect rendering.{System.Environment.NewLine}";
                    errorMsg +=
                        $"Please see documentation for Linear Blend Skinning Node and Compute Deformation Node in Shader Graph.{System.Environment.NewLine}";
                    Debug.LogWarning(errorMsg, authoring);
                }
            }

            // Takes a dependency on the transform
            var root = authoring.rootBone ? GetComponent<Transform>(authoring.rootBone) : GetComponent<Transform>(authoring);

            var mesh = authoring.sharedMesh;
            MeshRendererBakingUtility.ConvertToMultipleEntities(this, authoring, mesh, materials, root, out var additionalEntities);

            var hasSkinning = mesh == null ? false : mesh.boneWeights.Length > 0 && mesh.bindposeCount > 0;
            var hasBlendShapes = mesh == null ? false : mesh.blendShapeCount > 0;
            var deformedEntity = GetEntity(TransformUsageFlags.Dynamic);
            foreach (var entity in additionalEntities)
            {
               AddTransformUsageFlags(entity, TransformUsageFlags.Dynamic);
               // Add relevant deformation tags to converted render entities and link them to the DeformedEntity.
               AddComponent(entity, new DeformedMeshIndex());
               AddComponent(entity, new DeformedEntity {Value = deformedEntity});

               // Add SkinnedMeshRendererBakingData on the additional entities to allow RenderMeshPostProcessSystem to process on SkinnedMeshRenderer as well
               AddComponent(entity, new SkinnedMeshRendererBakingData {SkinnedMeshRenderer = authoring});
               SetComponent(entity, new RenderBounds { Value = authoring.localBounds.ToAABB() });
            }

            // Fill the blend shape weights.
            if (hasBlendShapes)
            {
                var weights = AddBuffer<BlendShapeWeight>(deformedEntity);
                weights.ResizeUninitialized(mesh.blendShapeCount);

                for (int i = 0; i < weights.Length; ++i)
                {
                    weights[i] = new BlendShapeWeight {Value = authoring.GetBlendShapeWeight(i)};
                }
            }

            // Fill the skin matrices with bindpose skin matrices.
            if (hasSkinning)
            {
                var bones = authoring.bones;
                var rootMatrixInv = root.localToWorldMatrix.inverse;

                var skinMatrices = AddBuffer<SkinMatrix>(deformedEntity);
                skinMatrices.ResizeUninitialized(bones.Length);
                var bindposes = mesh.GetBindposes();

                for (int i = 0; i < bones.Length; ++i)
                {
                    if (bones[i] == null)
                        continue;

                    // If the transform changes the skin matrices need to be updated.
                    DependsOn(bones[i]);

                    Assert.IsTrue(i < authoring.sharedMesh.bindposeCount, $"No corresponding bindpose found for the bone ({bones[i].name}) at index {i}.");

                    var bindPose = bindposes[i];
                    var boneMatRootSpace = math.mul(rootMatrixInv, bones[i].localToWorldMatrix);
                    var skinMatRootSpace = math.mul(boneMatRootSpace, bindPose);
                    skinMatrices[i] = new SkinMatrix
                    {
                        Value = new float3x4(skinMatRootSpace.c0.xyz, skinMatRootSpace.c1.xyz, skinMatRootSpace.c2.xyz,
                            skinMatRootSpace.c3.xyz)
                    };
                }
            }
        }
    }
}
