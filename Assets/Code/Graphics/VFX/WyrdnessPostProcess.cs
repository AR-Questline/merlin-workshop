// using System;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics.VFX {
    // [Serializable, VolumeComponentMenu("Post-processing/Custom/Wyrdness")]
    // public sealed class WyrdnessPostProcess : CustomPostProcessVolumeComponent, IPostProcessComponent {
    //     static readonly int MainTex = Shader.PropertyToID("_MainTex");
    //     static readonly int Visibility = Shader.PropertyToID("_Visibility");
    //     static readonly int SpherePosition = Shader.PropertyToID("_SpherePosition");
    //     static readonly int SpherePositionB = Shader.PropertyToID("_SpherePositionB");
    //     static readonly int SphereScale = Shader.PropertyToID("_SphereScale");
    //     static readonly int SphereContrast = Shader.PropertyToID("_SphereContrast");
    //     static readonly int Tint = Shader.PropertyToID("_Tint");
    //
    //     const string ShaderName = "Hidden/FullScreen_WyrdNight_v2";
    //
    //     public BoolParameter enabled = new BoolParameter(false, true);
    //     public ClampedFloatParameter visibility = new ClampedFloatParameter(1f, 0f, 1f);
    //     public Vector3Parameter spherePosition = new Vector3Parameter(new Vector3(), true);
    //     public Vector3Parameter spherePositionB = new Vector3Parameter(new Vector3(), true);
    //     public ClampedFloatParameter sphereScale = new ClampedFloatParameter (32f, 0f, 128f);
    //     public ClampedFloatParameter sphereContrast = new ClampedFloatParameter (1f, 0.01f, 128f);
    //     public ColorParameter tint = new ColorParameter(new Color(1f, 0f, 0f), true, true, true);
    //
    //     Material _material;
    //
    //     public bool IsActive() => enabled.value && _material != null;
    //
    //     // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    //     public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;
    //
    //     public override void Setup() {
    //         if (Shader.Find(ShaderName) != null) {
    //             _material = new Material(Shader.Find(ShaderName));
    //         } else {
    //             Debug.LogError($"Unable to find shader '{ShaderName}'. Post Process Volume Wyrdness is unable to load.");
    //         }
    //     }
    //
    //     public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination) {
    //         if (_material == null) {
    //             return;
    //         }
    //
    //         _material.SetTexture(MainTex, source);
    //         _material.SetFloat(Visibility, visibility.value);
    //         _material.SetVector(SpherePosition, spherePosition.value);
    //         _material.SetVector(SpherePositionB, spherePositionB.value);
    //         _material.SetFloat(SphereScale, sphereScale.value);
    //         _material.SetFloat(SphereContrast, sphereContrast.value);
    //         _material.SetColor(Tint, tint.value);
    //
    //         HDUtils.DrawFullScreen(cmd, _material, destination, shaderPassId: 0);
    //     }
    //
    //     public override void Cleanup() {
    //         CoreUtils.Destroy(_material);
    //     }
    // }
}
