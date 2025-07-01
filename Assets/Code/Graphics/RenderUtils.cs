using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.Authoring.LinkedEntities;
using Awaken.Kandra;
using UnityEngine;

namespace Awaken.TG.Graphics {
    public static class RenderUtils {

        static Dictionary<Renderer, Material[]> s_defaultMaterialsByRenderer = new();

        [UnityEngine.Scripting.Preserve]
        public static void SetReplacementShader(this Renderer renderer, Shader shader, int materialIndex) {
            CacheRenderer(renderer);
            renderer.materials[materialIndex].shader = shader;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void SetReplacementMaterial(this Renderer renderer, Material material, int materialIndex) {
            CacheRenderer(renderer);
            var materials = renderer.materials;
            materials[materialIndex] = material;
            renderer.materials = materials;
        }

        public static void SetReplacementMaterials(this Renderer renderer, Material[] materials) {
            CacheRenderer(renderer);
            renderer.materials = materials;
        }

        public static void ResetReplacements(this Renderer renderer) {
            if (s_defaultMaterialsByRenderer.Remove(renderer, out var materials)) {
                renderer.materials = materials;
            }
        }

        static void CacheRenderer(Renderer renderer) {
            if (!s_defaultMaterialsByRenderer.ContainsKey(renderer)) {
                s_defaultMaterialsByRenderer.Add(renderer, renderer.sharedMaterials.ToArray());
            }
        }

        public static bool HasAnyRenderer(Transform transform) {
            if (transform == null) {
                return false;
            }
            if (transform.GetComponentInChildren<Renderer>() != null) {
                return true;
            }
            if (transform.GetComponentInChildren<KandraRenderer>() != null) {
                return true;
            }
            if (transform.GetComponentInChildren<LinkedEntityLifetime>() != null) {
                return true;
            }
            if (transform.GetComponentInChildren<LinkedEntitiesAccess>() != null) {
                return true;
            }
            if (transform.GetComponentInChildren<SharedLinkedEntitiesLifetime>() != null) {
                return true;
            }
            // Medusa and Leshy can't be found
            return false;
        }
    }
}