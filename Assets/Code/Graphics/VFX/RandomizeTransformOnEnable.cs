using Awaken.TG.Main.General;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Graphics.VFX {
    public class RandomizeTransformOnEnable : MonoBehaviour {
        [SerializeField, OnValueChanged(nameof(EDITOR_UpdateValues))] bool randomizeLocalPosition;
        [SerializeField, ShowIf(nameof(randomizeLocalPosition))] bool randomizeXPosition;
        [SerializeField, ShowIf(nameof(randomizeXPosition))] FloatRange xPositionRange;
        [SerializeField, ShowIf(nameof(randomizeLocalPosition))] bool randomizeYPosition;
        [SerializeField, ShowIf(nameof(randomizeYPosition))] FloatRange yPositionRange;
        [SerializeField, ShowIf(nameof(randomizeLocalPosition))] bool randomizeZPosition;
        [SerializeField, ShowIf(nameof(randomizeZPosition))] FloatRange zPositionRange;
        
        [SerializeField, OnValueChanged(nameof(EDITOR_UpdateValues))] bool randomizeLocalRotation;
        [SerializeField, ShowIf(nameof(randomizeLocalRotation))] bool randomizeXRotation;
        [SerializeField, ShowIf(nameof(randomizeXRotation))] FloatRange xRotationRange;
        [SerializeField, ShowIf(nameof(randomizeLocalRotation))] bool randomizeYRotation;
        [SerializeField, ShowIf(nameof(randomizeYRotation))] FloatRange yRotationRange;
        [SerializeField, ShowIf(nameof(randomizeLocalRotation))] bool randomizeZRotation;
        [SerializeField, ShowIf(nameof(randomizeZRotation))] FloatRange zRotationRange;
        
        [SerializeField, OnValueChanged(nameof(EDITOR_UpdateValues))] bool randomizeLocalScale;
        [SerializeField, ShowIf(nameof(randomizeLocalScale))] bool randomizeXScale;
        [SerializeField, ShowIf(nameof(randomizeXScale))] FloatRange xScaleRange = new FloatRange(1f, 1f);
        [SerializeField, ShowIf(nameof(randomizeLocalScale))] bool randomizeYScale;
        [SerializeField, ShowIf(nameof(randomizeYScale))] FloatRange yScaleRange = new FloatRange(1f, 1f);
        [SerializeField, ShowIf(nameof(randomizeLocalScale))] bool randomizeZScale;
        [SerializeField, ShowIf(nameof(randomizeZScale))] FloatRange zScaleRange = new FloatRange(1f, 1f);
        
        void OnEnable() {
            RandomizePosition();
            RandomizeRotation();
            RandomizeScale();
        }
        
        void RandomizePosition() {
            if (!randomizeLocalPosition) {
                return;
            }

            float x = randomizeXPosition ? xPositionRange.RogueRandomPick() : transform.localPosition.x;
            float y = randomizeYPosition ? yPositionRange.RogueRandomPick() : transform.localPosition.y;
            float z = randomizeZPosition ? zPositionRange.RogueRandomPick() : transform.localPosition.z;
            transform.localPosition = new Vector3(x, y, z);
        }
        
        void RandomizeRotation() {
            if (!randomizeLocalRotation) {
                return;
            }
            
            float x = randomizeXRotation ? xRotationRange.RogueRandomPick() : transform.localRotation.eulerAngles.x;
            float y = randomizeYRotation ? yRotationRange.RogueRandomPick() : transform.localRotation.eulerAngles.y;
            float z = randomizeZRotation ? zRotationRange.RogueRandomPick() : transform.localRotation.eulerAngles.z;
            transform.localRotation = Quaternion.Euler(x, y, z);
        }
        
        void RandomizeScale() {
            if (!randomizeLocalScale) {
                return;
            }
            
            float x = randomizeXScale ? xScaleRange.RogueRandomPick() : transform.localScale.x;
            float y = randomizeYScale ? yScaleRange.RogueRandomPick() : transform.localScale.y;
            float z = randomizeZScale ? zScaleRange.RogueRandomPick() : transform.localScale.z;
            transform.localScale = new Vector3(x, y, z);
        }

        void EDITOR_UpdateValues() {
            if (!randomizeLocalPosition) {
                randomizeXPosition = false;
                randomizeYPosition = false;
                randomizeZPosition = false;
            }
            if (!randomizeLocalRotation) {
                randomizeXRotation = false;
                randomizeYRotation = false;
                randomizeZRotation = false;
            }
            if (!randomizeLocalScale) {
                randomizeXScale = false;
                randomizeYScale = false;
                randomizeZScale = false;
            }
        }
    }
}
