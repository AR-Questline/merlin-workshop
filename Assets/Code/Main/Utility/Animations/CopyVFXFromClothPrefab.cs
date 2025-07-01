using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Awaken.TG.Main.Utility.Animations {
    [RequireComponent(typeof(VisualEffect)), Obsolete("Use KandraRenderer pipeline instead", false)]
    public class CopyVFXFromClothPrefab : MonoBehaviour {
        [VFXPropertyBinding("UnityEditor.VFX.SkinnedMeshRenderer", "UnityEngine.SkinnedMeshRenderer"), SerializeField]
        ExposedProperty property = "SkinnedMeshRenderer";

        public int PropertyId => property;

        public void SetSkinnedMeshRenderer(SkinnedMeshRenderer newSkinnedMeshRenderer) {
            VisualEffect visualEffect = GetComponent<VisualEffect>();
            visualEffect.SetSkinnedMeshRenderer(property, newSkinnedMeshRenderer);
        }
    }
}