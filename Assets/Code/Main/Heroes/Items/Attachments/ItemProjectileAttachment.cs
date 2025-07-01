using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "For items with projectiles, like arrows.")]
    public class ItemProjectileAttachment : MonoBehaviour, IAttachmentSpec {
        public ItemProjectileData data = new();
        
        public IEnumerable<SkillReference> Skills => data.skills;
        
        public Element SpawnElement() {
            return new ItemProjectile();
        }

        public bool IsMine(Element element) => element is ItemProjectile;

        [Serializable]
        public class ItemProjectileData {
#if UNITY_EDITOR
            [Sirenix.OdinInspector.OnValueChanged(nameof(UpdateProjectileType), true)]
#endif            
            [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, group: AddressableGroup.Weapons)]
            public ShareableARAssetReference logicPrefab;
            [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, group: AddressableGroup.Weapons)]
            public ShareableARAssetReference visualPrefab;
            [SerializeField] public List<SkillReference> skills = new();
            [SerializeField] public ProjectileLogicData logicData = ProjectileLogicData.Default;
            
            public ProjectileData ToProjectileData() {
                return new ProjectileData(this);
            }
            
#if UNITY_EDITOR
            ProjectileType? EDITOR_LogicPrefabType = null;
            [Sirenix.OdinInspector.ShowInInspector]
            public ProjectileType LogicPrefabType {
                get {
                    EDITOR_LogicPrefabType ??= GetProjectileType();
                    return EDITOR_LogicPrefabType.Value;
                }
            }

            void UpdateProjectileType() {
                EDITOR_LogicPrefabType = GetProjectileType();
            }
            
            ProjectileType GetProjectileType() {
                if (logicPrefab is { IsSet: true } ) {
                    var ddp = logicPrefab.Get().EditorLoad<DamageDealingProjectile>();
                    logicData.EDITOR_ProjectileType = ddp switch {
                        ExplodingArrow => ProjectileType.ExplodingArrow,
                        Arrow => ProjectileType.Arrow,
                        MagicProjectile => ProjectileType.MagicProjectile,
                        _ => ProjectileType.NotSetLogicPrefab
                    };
                    return logicData.EDITOR_ProjectileType;
                }
                logicData.EDITOR_ProjectileType = ProjectileType.NotSetLogicPrefab;
                return ProjectileType.NotSetLogicPrefab;
            }
#endif          
            public enum ProjectileType : byte {
                NotSetLogicPrefab,
                Arrow,
                ExplodingArrow,
                MagicProjectile,
            }
        }
    }
}
