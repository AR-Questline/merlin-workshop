using Awaken.Utility.SerializableTypeReference;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public interface IDissolveAble {
        public GameObject gameObject { get; }
        public Transform transform { get; }

        public bool IsInDissolvableState { get; }
        public bool IsWeapon { get; }
        public bool IsCloth { get; }

        public void Init(bool fromStart = false);
        public void AssignController(IDissolveAbleDissolveController controller);

        public void ChangeToDissolveAble();
        public void RestoreToOriginal();

        public void InitPropertyModification(SerializableTypeReference serializedType, float value);
        public void FinishPropertyModification(SerializableTypeReference serializedType);
        public void UpdateProperty(SerializableTypeReference serializedType, float value);
    }
}
