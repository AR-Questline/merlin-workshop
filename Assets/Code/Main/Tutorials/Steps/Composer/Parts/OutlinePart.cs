using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Parts {
    [Serializable]
    public class OutlinePart : BasePart {
        public GameObject objectToOutline;
        public bool overrideColor;
        public OffsetSize offsetSize = OffsetSize.Normal;
        [ShowIf("@offsetSize == OffsetSize.Custom")]
        public Vector2 offsetLeftDown = new Vector2(4,4);
        [ShowIf("@offsetSize == OffsetSize.Custom")]
        public Vector2 offsetRightUp = new Vector2(4,4);

        static Type[] s_allowedComponentTypes = {typeof(Image), typeof(RectTransform), typeof(CanvasRenderer), typeof(LazyImage)};

        public override UniTask<bool> OnRun(TutorialContext context) {
            GameObject outlineGO = SpawnOutlineObject();
            context.onFinish += () => Object.Destroy(outlineGO);
            return UniTask.FromResult(true);
        }

        public override void TestRun(TutorialContext context) {
            GameObject outlineGO = SpawnOutlineObject();
            if (outlineGO != null) {
                context.onFinish += () => GameObjects.DestroySafely(outlineGO);
            }
        }

        GameObject SpawnOutlineObject() {
            if (objectToOutline == null) {
                Log.Important?.Error($"Null object to outline");
                return null;
            }

            // create outline GO
            GameObject newGO = Object.Instantiate(objectToOutline, objectToOutline.transform.parent);
            newGO.name = $"{objectToOutline.name} - Outline";
            RectTransform rectTrans = newGO.GetComponent<RectTransform>();

            // remove unnecessary components
            foreach (var component in newGO.GetComponents<Component>().ToList()) {
                if (!s_allowedComponentTypes.Contains(component.GetType())) {
                    GameObjects.DestroySafely(component);
                }
            }
            
            // ignore layout
            var layout = newGO.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            // set parent and offsets
            rectTrans.SetAsFirstSibling();
            rectTrans.offsetMin -= GetOffset(true);
            rectTrans.offsetMax += GetOffset(false);

            // remove children
            GameObjects.DestroyAllChildrenSafely(newGO.transform);

            // set material
            Image image = newGO.GetComponent<Image>();
            image.material = Resources.Load<Material>("Rendering/UIOutline");
            if (overrideColor) {
                image.color = new Color(1f, 1f, 1f, 1f);
            }
            
            // ensure image stays the same
            Image originalImage = objectToOutline.GetComponent<Image>();
            EnsureImageEquality(originalImage, image).Forget();

            return newGO;
        }
        
        async UniTaskVoid EnsureImageEquality(Image original, Image target) {
            Sprite sprite = original.sprite;
            while (target != null && original != null && target.gameObject != null) {
                if (original.sprite != sprite) {
                    sprite = original.sprite;
                    target.sprite = sprite;
                }
                await UniTask.DelayFrame(5);
            }
        }

        Vector2 GetOffset(bool leftDown) {
            return offsetSize switch {
                OffsetSize.Normal => new Vector2(4, 4),
                OffsetSize.Custom when leftDown => offsetLeftDown,
                OffsetSize.Custom => offsetRightUp,
                _ => new Vector2(4, 4)
            };
        }

        public enum OffsetSize {
            Normal = 0,
            Custom = 999,
        }
    }
}