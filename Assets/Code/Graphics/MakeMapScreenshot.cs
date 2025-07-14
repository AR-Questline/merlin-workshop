#if UNITY_EDITOR
using System.Linq;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet.Map;
using Awaken.Utility.Graphics;
using AwesomeTechnologies.VegetationSystem;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor; 
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics {
    public class MakeMapScreenshot : MonoBehaviour {

        [SerializeField] float dpi = 8;
        [SerializeField] int width = 7680;
        [SerializeField] int height = 4320;
        [SerializeField] float aspectRatio;
        
        [Button]
        void DoScreenshot() {
            RecalculateSize();
            CreateSetup();
            AdjustCamera();
            PrepareForScreenshot();
            MakeScreenshot();
        }

        [FoldoutGroup("Advanced"), Button]
        void RecalculateSize() {
            var groundBounds = FindAnyObjectByType<GroundBounds>();
            var bounds = groundBounds.CalculateGameBounds();
            var size = bounds.size;
            width = MultipleOfFour(Mathf.RoundToInt(size.x * dpi));
            height = MultipleOfFour(Mathf.RoundToInt(size.z * dpi));
            aspectRatio = size.x / size.z;

            int MultipleOfFour(int n) {
                var mod = n % 4;
                return mod == 0 ? n : n + 4 - mod;
            }
        }
        
        [FoldoutGroup("Advanced"), Button]
        void CreateSetup() {
            var cameraGO = new GameObject("Screenshot camera", typeof(Camera));
            cameraGO.hideFlags = HideFlags.DontSave;
            cameraGO.transform.parent = transform;
            
            var camera = GetComponentInChildren<Camera>();
            camera.orthographic = true;
            camera.cullingMask = -1 & ~(1 << LayerMask.NameToLayer("RainObstacle"));
            var hdCamera = camera.GetOrAddComponent<HDAdditionalCameraData>();
            hdCamera.volumeLayerMask = -1;

            var cameraTransform = cameraGO.transform;
            cameraTransform.localRotation = Quaternion.Euler(90, 0, 0);
            cameraTransform.localPosition = new(0, 900, 0);

            AdjustCamera();
        }
        
        [FoldoutGroup("Advanced"), Button]
        void AdjustCamera() {
            var camera = GetComponentInChildren<Camera>();
            var groundBounds = FindAnyObjectByType<GroundBounds>();
            var bounds = groundBounds.CalculateGameBounds();
            
            camera.orthographicSize = MapUI.GetOrthoSize(1f, bounds.size, aspectRatio);
            var cameraTransform = camera.transform;
            cameraTransform.localPosition = new(0, bounds.max.y, 0);

            var position = bounds.center;
            position.y = 0;
            transform.position = position;
        }
        
        [FoldoutGroup("Advanced"), Button]
        void PrepareForScreenshot() {
            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            var globalVolumes = volumes.Where(v => v.isGlobal).ToArray();
            foreach (var volume in globalVolumes) {
                var profile = volume.GetSharedOrInstancedProfile();
                DisableFog(profile);
                DisableChromaticAberration(profile);
            }
            var camera = GetComponentInChildren<Camera>();
            //FindAnyObjectByType<VegetationSystemPro>().AddCamera(camera);

            foreach (var lodGroup in FindObjectsByType<LODGroup>(FindObjectsSortMode.None)) {
                lodGroup.ForceLOD(0);
            }

            foreach (var terrain in FindObjectsByType<Terrain>(FindObjectsSortMode.None)) {
                terrain.heightmapPixelError = 250;
            }
        }

        [FoldoutGroup("Advanced"), Button]
        void MakeScreenshot() {
            PrepareForScreenshot();
            
            var path = EditorUtility.SaveFilePanel(
                "Save map texture as PNG",
                "",
                "map.png",
                "png");

            if (path.Length < 8) {
                return;
            }

            var camera = GetComponentInChildren<Camera>();
            var rt = new RenderTexture(width, height, 32);
            camera.targetTexture = rt;
            var screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            camera.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            byte[] bytes = screenShot.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            DestroyImmediate(screenShot);
        }

        [FoldoutGroup("Advanced"), Button]
        void Clear() {
            DestroyImmediate(gameObject);
        }

        static void DisableFog(VolumeProfile profile) {
            if (!profile.TryGet<Fog>(out var fog)) {
                return;
            }
            fog.enabled = new(false, true);
            fog.enableVolumetricFog = new(false, true);
        }

        static void DisableChromaticAberration(VolumeProfile profile) {
            if (!profile.TryGet<ChromaticAberration>(out var chromatic)) {
                return;
            }
            chromatic.active = false;
        }
    }
}
#endif
