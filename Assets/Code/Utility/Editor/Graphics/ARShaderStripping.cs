using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Utility.Editor.Graphics {
    public class ARShaderStripping : IPreprocessShaders {
        static readonly string[] KeywordsToStrip = new string[] {
            "_WINDDEBUG",
            "_INSTANCED_INDIRECT",
            "LOD_FADE_CROSSFADE",
            "_ADD_PRECOMPUTED_VELOCITY",
            "LIGHTMAP_ON",
            "DIRLIGHTMAP_COMBINED",
            "DYNAMICLIGHTMAP_ON",
            "USE_LEGACY_LIGHTMAPS",
            "DEBUG_DISPLAY",
            "SCREEN_SPACE_SHADOWS_OFF",
            "PUNCTUAL_SHADOW_LOW",
            "PUNCTUAL_SHADOW_HIGH",
            "DIRECTIONAL_SHADOW_LOW",
            "DIRECTIONAL_SHADOW_HIGH",
            "AREA_SHADOW_HIGH"
        };

        static readonly string[] ShadersToStripStrictMatch = new string[] {
            "Standard"
        };

        static readonly string[] ShadersToStripLooseMatch = new string[] {
            "AwesomeTechnologies/",
            "Legacy Shaders/",
            "Nature/"
        };

        public int callbackOrder => 0;

        static readonly MethodInfo IsKeywordNameEnabledMethod = typeof(ShaderKeywordSet).GetMethod("IsKeywordNameEnabled", BindingFlags.NonPublic | BindingFlags.Static);

        static readonly Func<ShaderKeywordSet, string, bool> IsKeywordNameEnabledDelegate = (Func<ShaderKeywordSet, string, bool>)Delegate.CreateDelegate(typeof(Func<ShaderKeywordSet, string, bool>), IsKeywordNameEnabledMethod);

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data) {
            var shaderName = shader.name;

            foreach (var shaderToStrip in ShadersToStripStrictMatch) {
                if (shaderName.Equals(shaderToStrip, StringComparison.InvariantCultureIgnoreCase)) {
                    data.Clear();
                    return;
                }
            }

            foreach (var shaderToStrip in ShadersToStripLooseMatch) {
                if (shaderName.Contains(shaderToStrip, StringComparison.InvariantCultureIgnoreCase)) {
                    data.Clear();
                    return;
                }
            }

            for (var i = data.Count - 1; i >= 0; i--) {
                var keywordSet = data[i].shaderKeywordSet;
                foreach (var keyword in KeywordsToStrip) {
                    if (IsKeywordNameEnabledDelegate(keywordSet, keyword)) {
                        data.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}
