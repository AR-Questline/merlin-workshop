using Awaken.TG.Main.Settings.Controllers;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Utility.Cameras {
    public static class CameraUtils {
        const float ShakeFreq = 6f;

        public static Vector3 ScreenPositionToWorldAtY(Vector2 screenPos, float y, Camera camera = null) {
            if (camera == null) camera = Camera.main;
            Ray mouseRay = camera.ScreenPointToRay(screenPos);
            return IntersectAtY(mouseRay, y);
        }

        [UnityEngine.Scripting.Preserve]
        public static Vector3 MousePositionAtY(float y, Camera camera = null) => ScreenPositionToWorldAtY(Input.mousePosition, y, camera);

        [UnityEngine.Scripting.Preserve]
        public static Vector3 SwitchCoordinateSystems(Camera source, Camera target, Vector3 sourcePosition, float targetZ) {
            Vector3 vpPos = source.WorldToViewportPoint(sourcePosition);
            return ViewportPointToWorldPointAtY(target, vpPos, targetZ);
        }

        public static Vector3 ViewportPointToWorldPointAtY(Camera camera, Vector3 viewportPoint, float y) {
            Ray ray = camera.ViewportPointToRay(viewportPoint);
            return IntersectAtY(ray, y);
        }

        public static Vector3 IntersectAtY(Ray ray, float y) {
            // make a plane and intersect with it
            Plane yPlane = new Plane(Vector3.up, new Vector3(0, y, 0));
            float intersectionDistance;
            yPlane.Raycast(ray, out intersectionDistance);
            // find the point of intersection
            Vector3 point = ray.GetPoint(intersectionDistance);
            point.y = y; // for exactness
            return point;
        }

        [UnityEngine.Scripting.Preserve]
        public static Vector2 PixelSizeToCameraSize(Vector2 size, float z, Camera camera = null) {
            if (camera == null) camera = Camera.main;
            Vector3 origin = camera.ScreenToWorldPoint(new Vector3(0f, 0f, z));
            Vector3 corner = camera.ScreenToWorldPoint(new Vector3(size.x, size.y, z));
            Vector3 diff = corner - origin;
            return new Vector2(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        }

        [UnityEngine.Scripting.Preserve]
        public static float ScaleFactorBasedOnDistance(Vector3 objectWorldPos, float focalDistance, Camera camera = null) {
            if (camera == null) camera = Camera.main;

            float z = Mathf.Abs((camera.transform.worldToLocalMatrix * (objectWorldPos - camera.transform.position)).z);
            // scale popup based on camera distance to object
            float multi = focalDistance / z;
            return multi;
        }

        [UnityEngine.Scripting.Preserve]
        public static Vector3 GetShakeOffset(float shakeForce, float shakeValue) {
            var force = shakeForce * Easing.Cubic.In(shakeValue);
            var t = Time.time * ShakeFreq;
            return new Vector3(
                (Mathf.PerlinNoise(t, 123.217f) - .5f) * force,
                (Mathf.PerlinNoise(t, 0.1277213f) - .5f) * force,
                (Mathf.PerlinNoise(0f, t) - .5f) * force
            );
        }

        public static async UniTask Render(Camera camera, RenderTexture texture, MonoBehaviour monoBehaviour) {
            if (Application.isPlaying && monoBehaviour == null) {
                Log.Important?.Error("Trying to render screenshot with null MonoBehaviour! This is not allowed!");
                return;
            }

            camera.enabled = false;
            var previousTarget = camera.targetTexture;
            camera.targetTexture = texture;
            
            if (Application.isPlaying) {
                await UniTask.WaitForEndOfFrame(monoBehaviour);
            }
            
            camera.Render();
            camera.targetTexture = previousTarget;
            camera.enabled = true;
        }

        public static Matrix4x4 GetOrthographicViewToClipMatrix(float orthoSize, float nearPlane, float farPlane) {
            var oneOverSize = 1f / orthoSize;
            var deltaDepth = nearPlane - farPlane;

            var c0 = new Vector4(oneOverSize, 0, 0, 0);
            var c1 = new Vector4(0, oneOverSize, 0, 0);
            var c2 = new Vector4(0, 0, -2 / deltaDepth, 0);
            var c3 = new Vector4(0, 0, (farPlane + nearPlane) / deltaDepth, 1);

            Matrix4x4 matrix = new();
            matrix.SetColumn(0, c0);
            matrix.SetColumn(1, c1);
            matrix.SetColumn(2, c2);
            matrix.SetColumn(3, c3);

            return matrix;
        }
    }
}