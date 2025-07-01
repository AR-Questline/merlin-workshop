using Awaken.CommonInterfaces.Assets;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class XboxReplace : MonoBehaviour, IEditorOnlyMonoBehaviour {
        public GameObject replacement;

        [UnityEngine.Scripting.Preserve]
        public Transform Replace() {
            if (replacement == null) {
                DestroyImmediate(gameObject);
                return null;
            }

            var myTransform = transform;
            myTransform.GetPositionAndRotation(out var position, out var rotation);

            var replace = Instantiate(replacement, myTransform.parent);
            var replaceTransform = replace.transform;
            replaceTransform.SetPositionAndRotation(position, rotation);
            replaceTransform.localScale = myTransform.localScale;
            replaceTransform.SetSiblingIndex(myTransform.GetSiblingIndex());

            DestroyImmediate(gameObject);
            return replaceTransform;
        }
    }
}
