using System;
using UnityEngine;

namespace Awaken.TG.Main.UI.Stickers
{
    /// <summary>
    /// Represents a single sticker container, responsible for positioning and
    /// watching the UI inside.
    /// </summary>
    public class Sticker : MonoBehaviour
    {
        // === Events

        public event Action<Sticker> WhenDone;

        // === State and properties

        public Transform anchor;
        public StickerPositioning positioning;

        bool _active;

        // === Creation

        public static Sticker Create(GameObject prefab, Transform anchor, StickerPositioning positioning) {
            GameObject gob = Instantiate(prefab);
            Sticker sticker = gob.GetComponent<Sticker>();
            sticker.anchor = anchor;
            sticker.positioning = positioning;
            sticker._active = true;
            return sticker;
        }

        // === Operation

        public virtual void Realign(Camera referenceCamera, Canvas referenceCanvas) {
            // was our anchor destroyed in the meantime?
            if (anchor == null) {
                WhenDone?.Invoke(this);
                return;
            }

            if (!_active) {
                return;
            }
            
            // where are we anchored in the world?
            Vector3 worldAnchor = anchor.position + positioning.worldOffset;
            // where is that in GUI space? (manual calculation, GUIUtility can't be used here)
            Vector2 screenAnchor = referenceCamera.WorldToScreenPoint(worldAnchor);
            Vector2 guiAnchor = screenAnchor / referenceCanvas.scaleFactor;
            // screen offset is scaled with the world position
            Vector2 scaleReference = referenceCamera.WorldToScreenPoint(worldAnchor + Vector3.up);
            float screenScale = (scaleReference - screenAnchor).magnitude / 50f;
            guiAnchor += screenScale * positioning.screenOffset;
            // place the transform there
            RectTransform rt = GetComponent<RectTransform>(); 
            if ((rt.anchoredPosition - guiAnchor).sqrMagnitude > 0.0001f) {
                rt.pivot = positioning.pivot;
                rt.anchoredPosition = guiAnchor;
            }
        }

        public void SetActive(bool active) {
            _active = active;
        }

        // === Unity lifecycle

        void OnTransformChildrenChanged() {
            if (transform.childCount == 0) {
                WhenDone?.Invoke(this);
            }
        }

        // === Convenience methods

        public static implicit operator Transform(Sticker sticker) => sticker.transform;
    }
}
