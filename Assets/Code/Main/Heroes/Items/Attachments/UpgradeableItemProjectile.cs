using Awaken.Utility;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class UpgradeableItemProjectile : Element<Item>, IRefreshedByAttachment<UpgradeableItemProjectileAttachment>, ISkillOwner, ISkillProvider {
        public override ushort TypeForSerialization => SavedModels.UpgradeableItemProjectile;

        UpgradeableItemProjectileAttachment _spec;
        
        public int MaxLevel => _spec.projectilesData.Length - 1;
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public ProjectileData Data(int level) => _spec.projectilesData[level].ToProjectileData();
        ShareableARAssetReference VisualRef(int level) => _spec.projectilesData[level].visualPrefab;
        ShareableARAssetReference LogicRef(int level) => _spec.projectilesData[level].logicPrefab;
        List<SkillReference> SkillReferences(int level) => _spec.projectilesData[level].skills;
        ProjectileLogicData LogicData(int level) => _spec.projectilesData[level].logicData;
        
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        public ICharacter Character => null;

        public void InitFromAttachment(UpgradeableItemProjectileAttachment spec, bool isRestored) {
            _spec = spec;
            foreach (var skillRef in _spec.Skills) {
                // Skills are not saved (see AllowElementSave)
                var skill = skillRef.CreateSkill();
                AddElement(skill);
            }
        }
        
        public override bool AllowElementSave(Element ele) {
            return false;
        }

        // === In Hand Projectile
        [UnityEngine.Scripting.Preserve]
        public async UniTask<IPooledInstance> GetInHandProjectile(int level, Transform parent, CancellationTokenSource tokenSource, bool disableShadowCasting = true) {
            return await ItemProjectile.GetCustomInHandProjectile(VisualRef(level), parent, tokenSource, disableShadowCasting);
        }

        // === Preload
        [UnityEngine.Scripting.Preserve]
        public ProjectilePreload PreloadProjectile(int level) {
            var refs = new ProjectilePreload(LogicRef(level).Get(), VisualRef(level).Get());
            refs.Preload();
            return refs;
        }

        // === Real Projectile
        // From Item
        [UnityEngine.Scripting.Preserve]
        public async UniTask<CombinedProjectile> GetProjectile(int level, Vector3 pos, Quaternion rot, bool releaseOnDestroy, [CanBeNull] Transform followTransformWhileLoading, [CanBeNull] Transform firePoint, CancellationTokenSource tokenSource) {
            var afterLoadTransformData = new AfterLoadTransformData(followTransformWhileLoading);
            var creationData = new ProjectileData(LogicRef(level), VisualRef(level), SkillReferences(level), LogicData(level));
            var logicRef = creationData.logicPrefab.Get();
            var logicPrefab = PrefabUtil.InstantiateAsync(logicRef, pos, rot);
            var inHand = ItemProjectile.GetCustomVisualProjectile(creationData.visualPrefab, null, tokenSource);
            return await ItemProjectile.CombinePrefabs(logicRef, logicPrefab, inHand, afterLoadTransformData, firePoint, releaseOnDestroy, creationData);
        }
    }
}