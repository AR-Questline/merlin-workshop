using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    public static class VFXManager {
        [UnityEngine.Scripting.Preserve]
        public static void SpawnCombatVFX(Item itemDealingDamage, Item hitSurface, Vector3 position, Vector3 forward,
            IAliveVfx aliveHit, Collider hitCollider, Transform parent = null, float lifeTime = 5f) {
            SurfaceType surfaceType = itemDealingDamage?.DamageSurfaceType ?? SurfaceType.DamageMagic;
            SpawnCombatVFX(surfaceType, hitSurface.Template.DamageSurfaceType, position, forward, aliveHit, hitCollider, parent, lifeTime);
        }

        public static void SpawnCombatVFX(SurfaceType damageSurface, SurfaceType hitSurface, Vector3 position,
            Vector3 forward, IAliveVfx aliveHit, Collider hitCollider, Transform parent = null, float lifeTime = 5f) {
            ShareableARAssetReference vfxPrefab = VfxContainer(aliveHit).GetVFX(damageSurface, hitSurface);
            SpawnCombatVFX(vfxPrefab, position, forward, hitCollider, parent, lifeTime).Forget();
        }

        public static async UniTaskVoid SpawnCombatVFX(ShareableARAssetReference vfxPrefab, Vector3 position,
            Vector3 forward, Collider hitCollider, Transform parent = null, float lifeTime = 5f) {
            if (vfxPrefab?.IsSet ?? false) {
                IPooledInstance instance;
                if (parent == null && hitCollider != null) {
                    Transform hitTransform = hitCollider.transform;
                    Vector3 localPositionOffset = Quaternion.Inverse(hitTransform.rotation) * (position - hitTransform.position);
                    instance = await PrefabPool.Instantiate(vfxPrefab, position, Quaternion.LookRotation(forward));
                    if (instance == null || instance.Instance == null) {
                        return;
                    }
                    if (hitTransform != null) {
                        instance.Instance.transform.position = hitTransform.position + hitTransform.rotation * localPositionOffset;
                    }
                } else {
                    instance = await PrefabPool.Instantiate(vfxPrefab, position, Quaternion.LookRotation(forward), parent);
                    if (instance == null || instance.Instance == null) {
                        return;
                    }
                }

                VFXLifetime vfxLifetime = instance.Instance.GetComponent<VFXLifetime>();
                if (vfxLifetime != null) {
                    lifeTime = vfxLifetime.lifeTime;
                }
                instance.Return(lifeTime).Forget();

                if (hitCollider == null) {
                    return;
                }
                
                foreach (var attach in instance.Instance.GetComponentsInChildren<AttachMeToHitCollider>(true)) {
                    attach.AttachToTransform(hitCollider.transform);
                }
            }
        }

        static ItemVfxContainer VfxContainer(IAliveVfx aliveHit = null) => aliveHit?.AliveVfx?.VfxContainer ?? World.Services.Get<GameConstants>().DefaultItemVfxContainer;

        [UnityEngine.Scripting.Preserve]
        public static SurfaceType GetHitSurfaceFromDamage(this Damage damage) {
            SurfaceType surfaceType = damage.Target.AudioSurfaceType;
            if (damage.Target is ICharacter character) {
                Item item = character.Inventory?.EquippedItem(EquipmentSlotType.Cuirass);
                ItemAudio targetItemAudio = item?.TryGetElement<ItemAudio>();
                if (targetItemAudio != null) {
                    surfaceType = targetItemAudio.AudioContainer.ArmorHitType;
                }
            }

            return surfaceType;
        }
    }
}
