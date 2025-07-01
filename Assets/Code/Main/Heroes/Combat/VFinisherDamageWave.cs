using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [UsesPrefab("Hero/Finishers/VFinisherDamageWave")]
    public class VFinisherDamageWave : View<FinisherDamageWave> {
        [SerializeField, Range(0.5f, 10f)] float damageMultiplier; 
        [SerializeField] float duration, endRadius;
        [SerializeField] bool inevitable;
        [SerializeField] LayerMask hitMask;
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.Weapons)]
        public ShareableARAssetReference vfx;
        
        public async UniTaskVoid Init(Vector3 position) {
            position = Ground.SnapToGround(position);
            float damage = Target.Damage * damageMultiplier;

            var parameters = DamageParameters.Default;
            parameters.ForceDamage = Target.ForceDamage;
            parameters.RagdollForce = Target.RagdollForce;
            parameters.Inevitable = inevitable;
            parameters.DamageTypeData = Target.DamageTypeData;
            
            var explosionParams = new SphereDamageParameters {
                rawDamageData = new RawDamageData(damage),
                duration = duration,
                endRadius = endRadius,
                hitMask = hitMask,
                item = Target.Item,
                baseDamageParameters = parameters
            };
            
            DealDamageInSphereOverTime dmg = new(explosionParams, position);
            Target.ParentModel.AddElement(dmg);
            PrefabPool.InstantiateAndReturn(vfx, position, Quaternion.identity, duration + PrefabPool.DefaultVFXLifeTime).Forget();
            if (await AsyncUtil.DelayTime(gameObject, duration)) {
                Discard();
            }
        }
    }
}