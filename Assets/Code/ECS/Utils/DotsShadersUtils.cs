using System.Collections.Generic;
using Awaken.Utility.Debugging;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.ECS.Utils {
    [Il2CppEagerStaticClassConstruction]
    public static class DotsShadersUtils {
        static readonly HashSet<Shader> ValidShaders = new HashSet<Shader>();
        static readonly HashSet<Shader> InvalidShaders = new HashSet<Shader>();
        
        public static bool AreAllMaterialsDotsShaders(MeshRenderer meshRenderer) {
            var materials = meshRenderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++) {
                var material = materials[i];
                if (material == null) {
                    return false;
                }
                var shader = material.shader;
                if (shader == null) {
                    return false;
                }
                if (ValidShaders.Contains(shader)) {
                    continue;
                }
                if (!IsDotsShader(shader)) {
                    return false;
                }
            }
            return true;
        }
        
        public static bool IsDotsShader(Shader shader) {
            if (InvalidShaders.Contains(shader)) {
                return false;
            }
            var keyword = shader.keywordSpace.FindKeyword("DOTS_INSTANCING_ON");
            if (!keyword.isValid) {
                InvalidShaders.Add(shader);
                return false;
            } else {
                ValidShaders.Add(shader);
            }
            return true;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/TG/Is dots shader")] 
        public static void CheckSelectedMaterial() {
            if (UnityEditor.Selection.activeObject is Material material) {
                Log.Important?.Error($"Material {material} has dots shader: {IsDotsShader(material.shader)}");
            }
        }
#endif
    }
}
