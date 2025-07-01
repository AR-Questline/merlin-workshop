using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Main.Cameras {
    public partial class GameCameraVoidOverride : Model {
        public sealed override bool IsNotSaved => true;

        readonly bool _fullVoid;
        int _originalCullingMask;
        LayerMask _originalVolumeLayerMask;
        
        Camera _camera;
        HDAdditionalCameraData _hdData;
        UpScalingController _upScalingController;

        public GameCameraVoidOverride(bool fullVoid) {
            _fullVoid = fullVoid;
        }
        
        public override Domain DefaultDomain => Domain.CurrentScene();
        
        protected override void OnInitialize() {
            _camera = World.Only<GameCamera>().MainCamera;
            _hdData = _camera.GetComponent<HDAdditionalCameraData>();
            _upScalingController = _camera.GetComponent<UpScalingController>();
            
            _originalCullingMask = _camera.cullingMask;
            _camera.cullingMask = RenderLayers.Mask.Void;

            if (_fullVoid) {
                _hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                _hdData.backgroundColorHDR = Color.black;
                _originalVolumeLayerMask = _hdData.volumeLayerMask;
                _hdData.volumeLayerMask = 0;
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            _camera.cullingMask = _originalCullingMask;
            if (_fullVoid) {
                _hdData.volumeLayerMask = _originalVolumeLayerMask;
                _hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            }

            _camera = null;
            _hdData = null;
            _upScalingController = null;
        }
    }
}