using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Skills;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [Serializable]
    public struct ProjectileData {
        public ShareableARAssetReference logicPrefab;
        public ShareableARAssetReference visualPrefab;
        public IEnumerable<SkillReference> skills;
        public ProjectileLogicData logicData;

        public ProjectileData(ItemProjectileAttachment.ItemProjectileData itemProjectileData) {
            logicPrefab = itemProjectileData.logicPrefab;
            visualPrefab = itemProjectileData.visualPrefab;
            skills = itemProjectileData.skills;
            logicData = itemProjectileData.logicData;
        }
        
        public ProjectileData(ShareableARAssetReference logicPrefab, ShareableARAssetReference visualPrefab, IEnumerable<SkillReference> skills, ProjectileLogicData logicData) {
            this.logicPrefab = logicPrefab;
            this.visualPrefab = visualPrefab;
            this.skills = skills;
            this.logicData = logicData;
        }
    }
}