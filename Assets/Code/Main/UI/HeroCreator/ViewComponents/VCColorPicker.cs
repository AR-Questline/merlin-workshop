using System;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.HeroCreator.ViewComponents {
    public class VCColorPicker : ViewComponent, IUIAware {
        public Color color;
        public Image xLine;
        public Image yLine;
        public float cursorSpeed;
        public Vector2 cursorUv = new Vector2(.5f, .5f);
        public Transform cursor;

        public event Action<Color> onColorChanged; 

        bool _isFocused;
        Texture2D _texture;

        [UnityEngine.Scripting.Preserve] public bool IsFocused => _isFocused;

        protected override void OnAttach() {
            _texture = GetComponent<Image>().sprite.texture;
            if (xLine != null) {
                xLine.gameObject.SetActive(false);
            }
            if (yLine != null) {
                yLine.gameObject.SetActive(false);
            }
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEMouseDown { IsLeft: true }) {
                _isFocused = true;
                return UIResult.Accept;
            }

            if (evt is UIEMouseUp { IsLeft: true }) {
                _isFocused = false;
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        void Update() {
            cursor.gameObject.SetActive(RewiredHelper.IsGamepad);
            if (RewiredHelper.IsGamepad) {
                // var motion = new Vector2(
                //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Horizontal), 
                //     RewiredHelper.Player.GetAxis(KeyBindings.Gameplay.Vertical)) * (cursorSpeed * Time.deltaTime);
                // cursorUv.x = Mathf.Clamp01(cursorUv.x + motion.x);
                // cursorUv.y = Mathf.Clamp01(cursorUv.y + motion.y);
                // cursor.position = UVToWorld(cursorUv);
                // if (RewiredHelper.Player.GetButtonDown(KeyBindings.UI.Generic.Accept)) {
                //     PickUv(cursorUv);
                // }
            }
            
            if (_isFocused) {
                Vector3 worldPosition = Input.mousePosition;

                Pick(worldPosition: worldPosition);
            }
        }

        Vector3 UVToWorld(Vector2 uv) {
            Vector3[] corners = new Vector3[4];
            ((RectTransform) transform).GetLocalCorners(corners);
            Vector2 leftBottom = transform.TransformPoint(corners[0]);
            Vector2 rightTop = transform.TransformPoint(corners[2]);
            return new Vector3(
                Mathf.Lerp(leftBottom.x, rightTop.x, uv.x),
                Mathf.Lerp(leftBottom.y, rightTop.y, uv.y),
                corners[0].z
                );
        }

        void PickUv(Vector2 uv) {
            Pick(UVToWorld(uv));
        }

        void Pick(Vector3 worldPosition) {
            Vector3[] corners = new Vector3[4];
            ((RectTransform) transform).GetLocalCorners(corners);

            Vector2 leftBottom = transform.TransformPoint(corners[0]);
            Vector2 rightTop = transform.TransformPoint(corners[2]);

            Vector2 uv = new Vector2() {
                x = (worldPosition.x - leftBottom.x) / (rightTop.x - leftBottom.x),
                y = (worldPosition.y - leftBottom.y) / (rightTop.y - leftBottom.y)
            };

            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);

            if (xLine != null && yLine != null) {
                xLine.gameObject.SetActive(true);
                yLine.gameObject.SetActive(true);

                var xLinePos = xLine.transform.position;
                xLinePos.x = Mathf.Clamp(worldPosition.x, leftBottom.x, rightTop.x);
                xLine.transform.position = xLinePos;
                var yLinePos = yLine.transform.position;
                yLinePos.y = Mathf.Clamp(worldPosition.y, leftBottom.y, rightTop.y);
                yLine.transform.position = yLinePos;
            }

            color = _texture.GetPixelBilinear(uv.x, uv.y);
            onColorChanged?.Invoke(color);
        }
    }
}
