using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Fights {
    public struct HitboxWrapper {
        SerializedArrayDictionary<EquatableCollider, HitboxData> _defaultHitboxes;
        StructListDictionary<EquatableCollider, HitboxData> _additionalHitboxes;
        
        [UnityEngine.Scripting.Preserve]
        public HitboxWrapper(SerializedArrayDictionary<EquatableCollider, HitboxData> defaultHitboxes) {
            this._defaultHitboxes = defaultHitboxes;
            _additionalHitboxes = new StructListDictionary<EquatableCollider, HitboxData>(0);
        }

        public void EnsureInitialized() {
            if (_defaultHitboxes.IsCreated) {
                return;
            }
            _defaultHitboxes = SerializedArrayDictionary<EquatableCollider, HitboxData>.Empty;
            _additionalHitboxes = new StructListDictionary<EquatableCollider, HitboxData>(0);
        }
        
        public void SetDefaultHitboxes(SerializedArrayDictionary<EquatableCollider, HitboxData> hitboxes) {
            _defaultHitboxes = hitboxes;
        }

        public void AddHitbox(Collider collider, in HitboxData hitbox) {
            _additionalHitboxes.Add(collider, hitbox);
        }
        
        public void RemoveHitbox(Collider collider) {
            _additionalHitboxes.Remove(collider);
        }
        
        public ref readonly HitboxData GetHitbox(Collider collider, out bool exists) {
            if (_defaultHitboxes.TryGetIndex(collider, out var index)) {
                exists = true;
                return ref _defaultHitboxes[index];
            } else if (_additionalHitboxes.TryGetIndex(collider, out index)) {
                exists = true;
                return ref _additionalHitboxes[index];
            } else {
                exists = false;
                return ref HitboxData.Default;
            }
        }

        public bool HasHitbox(Collider collider) {
            return _defaultHitboxes.ContainsKey(collider) || _additionalHitboxes.ContainsKey(collider);
        }
    }
}