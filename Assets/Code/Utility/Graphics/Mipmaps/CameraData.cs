using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Graphics.Mipmaps {
    public struct CameraData {
        public float3 cameraPosition;
        public float cameraEyeToScreenDistanceSq;

        public CameraData(Camera camera) {
            cameraPosition = camera.transform.position;

            float cameraHalfAngle = math.radians(camera.fieldOfView * 0.5f);
            float screenHalfHeight = camera.pixelHeight * 0.5f;
            var aspectRatio = camera.aspect;

            cameraEyeToScreenDistanceSq = math.square(screenHalfHeight / math.tan(cameraHalfAngle));
            if (aspectRatio > 1.0f) {
                cameraEyeToScreenDistanceSq *= aspectRatio;
            }
        }
    }
}
