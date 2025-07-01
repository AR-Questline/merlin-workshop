using UnityEngine;

namespace Awaken.TG.Graphics {
    public class PlanarReflectionController : MonoBehaviour{
    
        public GameObject m_ReflectionPlane;
        public Material m_Material;
        [SerializeField] LayerMask m_RenderLayers = 0;
    
        private Camera m_ReflectionCamera;
        private Camera m_MainCamera;
        private RenderTexture m_RenderTarget;
    
        void Start()
        {
            GameObject reflectionCameraGo = new GameObject("VReflectionCamera");
            m_ReflectionCamera = reflectionCameraGo.AddComponent<Camera>();
            m_ReflectionCamera.enabled = false;

            m_MainCamera = Camera.main;

            m_RenderTarget = new RenderTexture(Screen.width, Screen.height, 24) {name = "Runtime_PlanarReflectionController"};
        }

        private void OnPreRender(){
            RenderReflection();
        }

        void RenderReflection(){
            m_ReflectionCamera.CopyFrom(m_MainCamera);
            m_ReflectionCamera.cullingMask = m_RenderLayers;

            Vector3 cameraDirectionWorldSpace = m_MainCamera.transform.forward;
            Vector3 cameraUpWorldSpace = m_MainCamera.transform.up;
            Vector3 cameraPositionWorldSpace = m_MainCamera.transform.position;
        
            // Transform the vectors to the floor's space
            Vector3 cameraDirectionPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraDirectionWorldSpace);
            Vector3 cameraUpPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraUpWorldSpace);
            Vector3 cameraPositionPlaneSpace = m_ReflectionPlane.transform.InverseTransformDirection(cameraPositionWorldSpace);
        
            // Mirror the vectors
            cameraDirectionPlaneSpace.y *= -1.0f;
            cameraUpPlaneSpace.y *= -1.0f;
            cameraPositionPlaneSpace.y *= -1.0f;
        
            // Transform the vectors back to world space
            cameraDirectionWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraDirectionPlaneSpace);
            cameraUpWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraUpPlaneSpace);
            cameraPositionWorldSpace = m_ReflectionPlane.transform.TransformDirection(cameraPositionPlaneSpace);
        
            // Set camera position and rotation
            m_ReflectionCamera.transform.position = cameraPositionWorldSpace;
            m_ReflectionCamera.transform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);
        
            // Set render target for the reflection camera
            m_ReflectionCamera.targetTexture = m_RenderTarget;
        
            //Render the reflection camera
            m_ReflectionCamera.Render();
        
            m_Material.SetTexture("_ReflectionTex", m_RenderTarget);
        }
    }
}
