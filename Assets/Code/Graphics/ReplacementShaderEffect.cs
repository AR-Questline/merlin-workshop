using UnityEngine;

namespace Awaken.TG.Graphics {
    [ExecuteInEditMode]
    public class ReplacementShaderEffect : MonoBehaviour
    {
        public Shader ReplacementShader;

        void OnEnable() {
            if(ReplacementShader != null)
                GetComponent<Camera>().SetReplacementShader(ReplacementShader, "RenderType");
        }

        private void OnDisable()
        {
            GetComponent<Camera>().ResetReplacementShader();
        }
    }
}
