using System.Linq;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillHomingProjectilesLimit : Unit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public int maxHomingProjectiles = 3;
        
        protected override void Definition() {
            ValueOutput("canSpawnHomingProjectile",
                _ => Hero.Current.VHeroController.FirePoint.GetComponentsInChildren<HeroHomingProjectile>().Length < maxHomingProjectiles);
        }
    }
    
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillHeroLightsLimit : Unit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public int maxHeroLights = 1;
        
        protected override void Definition() {
            ValueOutput("canSpawnHeroLight",
                _ => Hero.Current.VHeroController.FirePoint.GetComponentsInChildren<HeroLight>().Length < maxHeroLights);
        }
    }

    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillHeroSummonsLimit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var input = FallbackARValueInput("maxSummonsSpawned", _ => 1);
            
            ValueOutput("canSpawnSummon",
                flow => {
                    Item item = ((IItemSkillOwner)this.Skill(flow).ParentModel).Item;
                    var spawnedSummons = World.All<NpcHeroSummon>().Count(summon => summon.Item == item);
                    return spawnedSummons < input.Value(flow);
                });
        }
    }
}