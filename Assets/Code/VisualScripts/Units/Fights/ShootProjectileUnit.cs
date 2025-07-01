using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Archers;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory(UnitCategory)]
    [UnitTitle("Shoot Projectile")]
    [UnityEngine.Scripting.Preserve]
    public class ShootProjectileUnit : ARUnit {
        const string UnitCategory = "AR/AI_Systems/Combat/Projectiles";
        
        protected override void Definition() {
            var shooterPort = FallbackARValueInput<ICharacter>("shooter", static _ => null);
            var itemPort = InlineARValueInput<Item>("itemProjectile", null);
            var logicCustomPort = InlineARValueInput<ShareableARAssetReference>("logicCustom", null);
            var visualCustomPort = InlineARValueInput<ShareableARAssetReference>("visualCustom", null);
            var logicDataCustomPort = FallbackARValueInput<ProjectileLogicData>("logicDataCustom", null);
            var skillsCustomPort = FallbackARValueInput<List<SkillReference>>("skillsCustom", null);
            var movementPort = RequiredARValueInput<MovementParams>("movement");
            var damagePort = RequiredARValueInput<DamageParams>("damage");
            DefineSimpleAction(flow => {
                var shooter = shooterPort.Value(flow);
                var item = itemPort.Value(flow);
                var movement = movementPort.Value(flow);
                var damage = damagePort.Value(flow);

                ProjectileData projectileData = item?.TryGetElement<ItemProjectile>()?.Data ?? new ProjectileData();
                var logic = logicCustomPort.Value(flow);
                var visual = visualCustomPort.Value(flow);
                ProjectileLogicData? logicData = logicDataCustomPort.Value(flow);
                List<SkillReference> skills = skillsCustomPort.Value(flow);
                if (logic is { IsSet: true }) {
                    projectileData.logicPrefab = logic;
                }
                if (visual is { IsSet: true }) {
                    projectileData.visualPrefab = visual;
                }
                if (logicData != null && logicData.HasValue) {
                    projectileData.logicData = logicData.Value;
                }
                if (skills != null) {
                    projectileData.skills = skills;
                }
                
                VGUtils.ShootProjectile(shooter, projectileData, movement.from, movement.velocity, damage.strength, damage.type, damage.slotType, damage.amount);
            });
        }

        public class MovementParams {
            public Vector3 from;
            public Vector3 velocity;

            public MovementParams(Vector3 from, Vector3 velocity) {
                this.from = from;
                this.velocity = velocity;
            }

            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("Projectile Movement")]
            [UnityEngine.Scripting.Preserve]
            public class FromVelocityUnit : ARUnit {
                protected override void Definition() {
                    var from = InlineARValueInput("from", Vector3.zero);
                    var velocity = InlineARValueInput("velocity", Vector3.zero);
                    ValueOutput("movement", flow => new MovementParams(
                        from.Value(flow),
                        velocity.Value(flow)
                    ));
                }
            }
            
            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("Projectile Arc")]
            [UnityEngine.Scripting.Preserve]
            public class FromToUnit : ARUnit {
                protected override void Definition() {
                    var fromPort = InlineARValueInput("from", Vector3.zero);
                    var toPort = InlineARValueInput("to", Vector3.zero);
                    var targetVelocityPort = InlineARValueInput("targetVelocity", Vector3.zero);
                    var projectileSpeedPort = InlineARValueInput("projectileSpeed", 50f);
                    var highShotPort = InlineARValueInput("highShot", false);
                    ValueOutput("movement", flow => {
                        var from = fromPort.Value(flow);
                        var to = toPort.Value(flow);
                        var targetVelocity = targetVelocityPort.Value(flow);
                        var projectileSpeed = projectileSpeedPort.Value(flow);
                        var highShot = highShotPort.Value(flow);
                        var velocity = ArcherUtils.ShotVelocity(new ShotData(from, to, targetVelocity, projectileSpeed, highShot));
                        return new MovementParams(from, velocity);
                    });
                }
            }
        }

        public class DamageParams {
            public float strength = 1;
            public DamageType? type = null;
            public float? amount = null;
            public EquipmentSlotType slotType = null;

            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("Projectile Damage")]
            [UnityEngine.Scripting.Preserve]
            public class CreatorUnit : ARUnit {
                [Serialize, Inspectable] public bool overrideType;
                [Serialize, Inspectable] public bool overrideAmount;
                [Serialize, Inspectable] public bool overrideSlotType;
                
                protected override void Definition() {
                    var fireStrengthPort = InlineARValueInput("fireStrength", 1f);

                    var typePort = overrideType ? InlineARValueInput("type", DamageType.PhysicalHitSource) : null;
                    var amountPort = overrideAmount ? InlineARValueInput("amount", 0f) : null;
                    var slotTypePort = overrideSlotType ? RequiredARValueInput<EquipmentSlotType>("ItemStatsFromSlotOfType") : null;

                    ValueOutput("params", flow => {
                        var damage = new DamageParams {
                            strength = fireStrengthPort.Value(flow)
                        };

                        damage.type = typePort?.Value(flow);
                        damage.amount = amountPort?.Value(flow);
                        damage.slotType = slotTypePort?.Value(flow);
                        
                        return damage;
                    });
                }
            }
            
            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("With Type")]
            [UnityEngine.Scripting.Preserve]
            public class WithTypeUnit : ARUnit {
                protected override void Definition() {
                    var paramsPort = RequiredARValueInput<DamageParams>("params");
                    var damageTypePort = InlineARValueInput("damageType", DamageType.PhysicalHitSource);
                    ValueOutput("output", flow => {
                        var damageParams = paramsPort.Value(flow);
                        damageParams.type = damageTypePort.Value(flow);
                        return damageParams;
                    });
                }
            }
            
            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("With Amount")]
            [UnityEngine.Scripting.Preserve]
            public class WithAmountUnit : ARUnit {
                protected override void Definition() {
                    var paramsPort = RequiredARValueInput<DamageParams>("params");
                    var amountPort = InlineARValueInput("amount", 1f);
                    ValueOutput("output", flow => {
                        var damageParams = paramsPort.Value(flow);
                        damageParams.amount = amountPort.Value(flow);
                        return damageParams;
                    });
                }
            }
            
            [TypeIcon(typeof(FlowGraph))]
            [UnitCategory(UnitCategory)]
            [UnitTitle("With Slot")]
            [UnityEngine.Scripting.Preserve]
            public class WithSlotTypeUnit : ARUnit {
                [Serialize, Inspectable, UnitHeaderInspectable] 
                [RichEnumExtends(typeof(EquipmentSlotType))]
                public RichEnumReference slot;
                
                protected override void Definition() {
                    var paramsPort = RequiredARValueInput<DamageParams>("params");
                    ValueOutput("output", flow => {
                        var damageParams = paramsPort.Value(flow);
                        damageParams.slotType = slot.EnumAs<EquipmentSlotType>();
                        return damageParams;
                    });
                }
            }
        }
    }
}