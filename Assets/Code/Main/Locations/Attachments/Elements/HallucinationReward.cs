using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class HallucinationReward : Element<Location>, IRefreshedByAttachment<HallucinationRewardAttachment> {
        public override ushort TypeForSerialization => SavedModels.HallucinationReward;

        HallucinationRewardAttachment _spec;

        public void InitFromAttachment(HallucinationRewardAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, OnBeforeDeath, this);
        }

        void OnBeforeDeath(DamageOutcome _) {
            var statusTemplate = _spec.StatusTemplate;
            var statuses = Hero.Current.Statuses;

            var sporesLocation = SpawnSpores();
            var rewardLocationTemplate = _spec.RewardLocation;
            var rewardSpawnRadius = RandomUtil.UniformFloat(_spec.RewardSpawnRadius.x, _spec.RewardSpawnRadius.y);
            var rewardSpawnVfx = _spec.RewardSpawnVfxReference;
            var rewardHideVfx = _spec.RewardHideVfxReference;
            
            if (statuses.TryGetStatus(statusTemplate, out var status)) {
                SpawnReward(sporesLocation, status, rewardLocationTemplate, rewardSpawnRadius, rewardSpawnVfx, rewardHideVfx);
            } else {
                WaitToSpawnReward(sporesLocation, statuses, statusTemplate, rewardLocationTemplate, rewardSpawnRadius, rewardSpawnVfx, rewardHideVfx).Forget();
            }
            
            Discard();
        }
        
        Location SpawnSpores() {
            var center = ParentModel.Coords;
            var rotation = ParentModel.Rotation;
            if (_spec.SporesSpawnVfxReference is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(_spec.SporesSpawnVfxReference, center, rotation).Forget();
            }
            var location =  _spec.SporesLocation.SpawnLocation(center, rotation);
            location.MarkedNotSaved = true;
            return location;
        }

        static async UniTaskVoid WaitToSpawnReward(Location sporesLocation, CharacterStatuses statuses, StatusTemplate statusTemplate, LocationTemplate rewardLocationTemplate, float spawnRadius, ShareableARAssetReference rewardSpawnVfx, ShareableARAssetReference rewardHideVfx) {
            Status status;
            while (!statuses.TryGetStatus(statusTemplate, out status)) {
                if (!await AsyncUtil.DelayFrame(sporesLocation, 3)) {
                    return;
                }
            }
            SpawnReward(sporesLocation, status, rewardLocationTemplate, spawnRadius, rewardSpawnVfx, rewardHideVfx);
        }

        static void SpawnReward(Location sporesLocation, Status status, LocationTemplate rewardLocationTemplate, float spawnRadius, ShareableARAssetReference rewardSpawnVfx, ShareableARAssetReference rewardHideVfx) {
            var offset2 = RandomUtil.OnUnitCircle() * spawnRadius;
            var position = sporesLocation.Coords + new Vector3(offset2.x, 0, offset2.y);
            position = AstarPath.active.GetNearest(position, NNConstraint.Walkable).position;
            
            if (rewardSpawnVfx is { IsSet: true }) {
                PrefabPool.InstantiateAndReturn(rewardSpawnVfx, position, Quaternion.identity).Forget();
            }
            
            var rewardLocation = rewardLocationTemplate.SpawnLocation(position);
            rewardLocation.AddElement(new HallucinationRewardStatusLinkLifetime(status, status.Template, rewardHideVfx));
        }
    }
}