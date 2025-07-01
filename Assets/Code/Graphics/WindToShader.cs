using UnityEngine;

namespace Awaken.TG.Graphics {
    [ExecuteAlways]
    public class WindToShader : MonoBehaviour{
        WindZone _windZone;

        void Start() {
            _windZone = gameObject.GetComponent<WindZone>();
        }

        void LateUpdate(){
            ApplySettings();
        }

        void OnValidate(){
            ApplySettings();
        }

        void ApplySettings(){
            if (_windZone != null){
                Shader.SetGlobalVector("_WINDZONE_Direction", _windZone.transform.forward);
                Shader.SetGlobalFloat("_WINDZONE_Main", _windZone.windMain);
                Shader.SetGlobalFloat("_WINDZONE_Turbulence", _windZone.windTurbulence);
                Shader.SetGlobalFloat("_WINDZONE_Pulse_Frequency", _windZone.windPulseFrequency);
                Shader.SetGlobalFloat("_WINDZONE_Pulse_Magnitude", _windZone.windPulseMagnitude);
            }
        }
    }
}