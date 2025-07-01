using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.RenderingValidations.StaticValidators {
    // ReSharper disable once UnusedType.Global
    public class HalfSizeOfAlbedoRendererValidator : IStaticRenderingValidator<Material> {
        static readonly int LitBaseMapId = Shader.PropertyToID("_BaseColorMap");
        static readonly int LitNormalMapId = Shader.PropertyToID("_NormalMap");
        static readonly int LitMaskMapId = Shader.PropertyToID("_MaskMap");

        static readonly int ArchitectureBaseMapId = Shader.PropertyToID("_Base_BaseColor");
        static readonly int ArchitectureNormalMapId = Shader.PropertyToID("_Base_NormalMap");
        static readonly int ArchitectureMaskMapId = Shader.PropertyToID("_Base_MaskMap");
        static readonly int OverlayBaseMapId = Shader.PropertyToID("_Overlay_BaseColor");
        static readonly int OverlayNormalMapId = Shader.PropertyToID("_Overlay_NormalMap");
        static readonly int OverlayMaskMapId = Shader.PropertyToID("_Overlay_MaskMap");

        public void Check(Material material, List<RenderingErrorMessage> errorMessages) {
            var shader = material.shader;
            if (shader == null) {
                return;
            }
            var name = shader.name;
            if (name.Contains("HDRP/Lit")) {
                ProcessLit(material, errorMessages);
            } else if (name.Contains("KF/Architecture/Architecture")) {
                ProcessArchitecture(material, errorMessages);
            }
        }

        void ProcessLit(Material material, List<RenderingErrorMessage> errorMessages) {
            var baseMap = material.GetTexture(LitBaseMapId);
            if (baseMap == null) {
                errorMessages.Add(new("Lit material has no base map", RenderingErrorLogType.Error));
                return;
            }
            var desiredWidth = baseMap.width / 2f;
            var desiredHeight = baseMap.height / 2f;

            var logType = Mathf.Max(baseMap.width, baseMap.height) switch {
                >= 4096 => RenderingErrorLogType.Exception,
                >= 2048 => RenderingErrorLogType.Error,
                >= 1024 => RenderingErrorLogType.Warning,
                _       => RenderingErrorLogType.Assert,
            };

            var normalMap = material.GetTexture(LitNormalMapId);
            if (normalMap != null && (normalMap.width > desiredWidth || normalMap.height > desiredHeight)) {
                errorMessages.Add(new($"Normal map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }

            var maskMap = material.GetTexture(LitMaskMapId);
            if (maskMap != null && (maskMap.width > desiredWidth || maskMap.height > desiredHeight)) {
                errorMessages.Add(new($"Mask map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }
        }

        void ProcessArchitecture(Material material, List<RenderingErrorMessage> errorMessages) {
            var baseMap = material.GetTexture(ArchitectureBaseMapId);
            if (baseMap == null) {
                errorMessages.Add(new("Architecture material has no base map", RenderingErrorLogType.Error));
                return;
            }
            var desiredWidth = baseMap.width / 2f;
            var desiredHeight = baseMap.height / 2f;
            
            var logType = Mathf.Max(baseMap.width, baseMap.height) switch {
                >= 4096 => RenderingErrorLogType.Exception,
                >= 2048 => RenderingErrorLogType.Error,
                >= 1024 => RenderingErrorLogType.Warning,
                _       => RenderingErrorLogType.Assert,
            };

            var normalMap = material.GetTexture(ArchitectureNormalMapId);
            if (normalMap != null && (normalMap.width > desiredWidth || normalMap.height > desiredHeight)) {
                errorMessages.Add(new($"Normal map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }

            var maskMap = material.GetTexture(ArchitectureMaskMapId);
            if (maskMap != null && (maskMap.width > desiredWidth || maskMap.height > desiredHeight)) {
                errorMessages.Add(new($"Mask map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }

            baseMap = material.GetTexture(OverlayBaseMapId);
            if (baseMap == null) {
                return;
            }
            desiredWidth = baseMap.width / 2f;
            desiredHeight = baseMap.height / 2f;
            logType = Mathf.Max(baseMap.width, baseMap.height) switch {
                >= 4096 => RenderingErrorLogType.Exception,
                >= 2048 => RenderingErrorLogType.Error,
                >= 1024 => RenderingErrorLogType.Warning,
                _       => RenderingErrorLogType.Assert,
            };

            normalMap = material.GetTexture(OverlayNormalMapId);
            if (normalMap != null && (normalMap.width > desiredWidth || normalMap.height > desiredHeight)) {
                errorMessages.Add(new($"Normal map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }

            maskMap = material.GetTexture(OverlayMaskMapId);
            if (maskMap != null && (maskMap.width > desiredWidth || maskMap.height > desiredHeight)) {
                errorMessages.Add(new($"Mask map is bigger than {desiredWidth}x{desiredHeight}", logType));
            }
        }
    }
}
