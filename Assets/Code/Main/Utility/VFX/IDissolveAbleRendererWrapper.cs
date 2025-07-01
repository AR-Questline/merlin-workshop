using Awaken.Utility.SerializableTypeReference;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public interface IDissolveAbleRendererWrapper {
        public Material[] InstancedMaterialsForExternalModifications { get; }
        public bool InDissolvableState { get; }

        public void Init();
        public void Destroy();
        public void ChangeToDissolveAble();
        public void RestoreToOriginalMaterials();
        public void InitPropertyModification(SerializableTypeReference serializedType, float value);
        public void UpdateProperty(SerializableTypeReference serializedType, float value);
        public void FinishPropertyModification(SerializableTypeReference serializedType);
    }
}