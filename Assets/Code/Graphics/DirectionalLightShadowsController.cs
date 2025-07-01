using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Graphics {
    [RequireComponent(typeof(Light), typeof(HDAdditionalLightData))]
    public class DirectionalLightShadowsController : MonoBehaviour {
#if !UNITY_GAMECORE
        public int alwaysUpdate = 1;

        HDAdditionalLightData _lightData;
        uint _updateTick;
        Camera _previousCamera;

        void OnEnable() {
            _lightData = GetComponent<HDAdditionalLightData>();

            if (_lightData) {
                if (_lightData.shadowUpdateMode != ShadowUpdateMode.OnDemand) {
                    _lightData.shadowUpdateMode = ShadowUpdateMode.OnDemand;
                }
                RenderPipelineManager.beginCameraRendering += RenderPipelineManagerOnBeginCameraRendering;
            }
        }

        void OnDisable() {
            if (_lightData != null) {
                if (_lightData.shadowUpdateMode != ShadowUpdateMode.EveryFrame) {
                    _lightData.shadowUpdateMode = ShadowUpdateMode.EveryFrame;
                }
            }
            _previousCamera = null;
            _updateTick = 0;
            _lightData = null;
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManagerOnBeginCameraRendering;
        }

        void RenderPipelineManagerOnBeginCameraRendering(ScriptableRenderContext _, Camera camera) {

#if UNITY_EDITOR
            if (FrameDebugger.enabled) {
                // Render all cascades when frame debugger is enabled since it's impossible to use when events jump around.
                _lightData.RequestShadowMapRendering();
            } else
#endif
            {
                var tick = _updateTick++;

                if (camera != _previousCamera) {
                    _lightData.RequestShadowMapRendering();
                    _previousCamera = camera;
                } else {
                    for (int i = 0; i < alwaysUpdate; i++) {
                        _lightData.RequestSubShadowMapRendering(i);
                    }
                    var t = (tick % (4-alwaysUpdate)) + alwaysUpdate;
                    _lightData.RequestSubShadowMapRendering((int)t);
                }
            }
        }
#endif
    }
}
