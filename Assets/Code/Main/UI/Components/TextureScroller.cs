using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    public class TextureScroller : MonoBehaviour {

        static readonly int MainTex = Shader.PropertyToID("_MainTex");

        public Image image;
        public Vector2 speed;
        public Vector2 tiling = Vector2.one;

        Material _material;
        Vector2 _offset;

        void Start() {
            _material = image.material;
            _offset = _material.GetTextureOffset(MainTex);
            _material.SetTextureScale(MainTex, tiling);
        }

        void Update() {
            _offset += speed * Time.deltaTime;
            _material.SetTextureOffset(MainTex, _offset);
        }
    }
}