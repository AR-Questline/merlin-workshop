using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Authoring {
    /// <summary>
    /// If there is no rendering target then the ECS Graphics system will leak GraphicBuffers.
    /// So this creates dummy triangle to prevent that.
    /// </summary>
    public class CreateAlwaysPresentEcsGraphicsEntity : MonoBehaviour {
        const float SqrtOf3 = 1.7320508F;
        void Awake() {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            var mesh = new Mesh();
            mesh.SetVertices(new[] {
                new Vector3(0, 2 * SqrtOf3),
                new Vector3(3, -SqrtOf3),
                new Vector3(-3, -SqrtOf3),
            });
            mesh.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(true);
            
            var material = new Material(Shader.Find("HDRP/Lit"));

            var egs = entityManager.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var materialId = egs.RegisterMaterial(material);
            var meshId = egs.RegisterMesh(mesh);

            var desc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false);

            // Create an array of mesh and material required for runtime rendering.
            var entity = entityManager.CreateEntity();
#if UNITY_EDITOR
            entityManager.SetName(entity, "GRAPHICS MEMORY FIX HACK");
#endif

            // Call AddComponents to populate base entity with the components required
            // by Entities Graphics
            RenderMeshUtility.AddComponents(
                entity,
                entityManager,
                desc,
                new MaterialMeshInfo(materialId, meshId));
            entityManager.AddComponentData(entity, new LocalToWorld() {
                Value = float4x4.TRS(new float3(-5500, -5500, -5500), quaternion.identity, new float3(1, 1, 1)),
            });
        }
    }
}
