using Awaken.TG.Main.UI.Components;
using UnityEngine;

namespace Awaken.TG.Main.UI.Helpers {
    /// <summary>
    /// Sets particles properties to instantiated UI material
    /// </summary>
    [RequireComponent(typeof(UIMaterialHelper))]
    public class UIParticlesInit : MonoBehaviour {
        static readonly int ParticlesTex = Shader.PropertyToID("_ParticlesTex");
        static readonly int ParticlesColor = Shader.PropertyToID("_ParticlesColor");

        public Color color;
        public Texture2D particlesTexture;

        void Start() {
            UIMaterialHelper helper = GetComponent<UIMaterialHelper>();
            helper.InstantiatedMaterial.SetColor(ParticlesColor, color);
            helper.InstantiatedMaterial.SetTexture(ParticlesTex, particlesTexture);
        }
    }
}