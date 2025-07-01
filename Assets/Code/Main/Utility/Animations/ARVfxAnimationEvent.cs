using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Utility.Animations {
    [UnityEngine.Scripting.Preserve]
    public class ARVfxAnimationEvent : ScriptableObject {
        const string PositioningGroup = "Positioning";
        
        [ARAssetReferenceSettings(new[] { typeof(VisualEffect) }, group: AddressableGroup.VFX)]
        [SerializeField] ShareableARAssetReference vfx;
        [SerializeField] float vfxLifetime = PrefabPool.DefaultVFXLifeTime;

        [SerializeField, FoldoutGroup(PositioningGroup)] Vector3 positionOffset;
        [SerializeField, FoldoutGroup(PositioningGroup)] Vector3 rotationOffset;
        [SerializeField, FoldoutGroup(PositioningGroup)] bool parentToCharacter;
        
        public async UniTask<IPooledInstance> SpawnOnCharacter(ICharacter character) {
            if (character == null) {
                return null;
            }
            
            var vfxPosition = positionOffset;
            Quaternion vfxRotation = Quaternion.Euler(rotationOffset);

            Transform parent = null;
            
            if (parentToCharacter) {
                parent = character.ParentTransform;
            } else {
                vfxPosition += character.Coords;
                vfxRotation = character.Rotation * vfxRotation;
            }
            
            var instance = await PrefabPool.InstantiateAndReturn(vfx, vfxPosition, vfxRotation, vfxLifetime, parent);
            return instance;
        }
    }
}