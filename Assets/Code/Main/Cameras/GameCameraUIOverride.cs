using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Cameras {
    public class GameCameraUIOverride : Element<IModel> {
        public sealed override bool IsNotSaved => true;

        int _originalCullingMask;
        LayerMask _originalVolumeLayerMask;
        
        Camera _camera;
        HDAdditionalCameraData _hdData;
        
        protected override void OnInitialize() {
            _camera = World.Only<GameCamera>().MainCamera;
            _hdData = _camera.GetComponent<HDAdditionalCameraData>();
            
            _originalCullingMask = _camera.cullingMask;
            _camera.cullingMask = RenderLayers.Mask.UI;
            
            _originalVolumeLayerMask = _hdData.volumeLayerMask;
            _hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            _hdData.backgroundColorHDR = Color.black;
            _hdData.volumeLayerMask = RenderLayers.Mask.UI;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            _camera.cullingMask = _originalCullingMask;
            
            _hdData.volumeLayerMask = _originalVolumeLayerMask;
            _hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            
            _camera = null;
            _hdData = null;
        }
    }
}