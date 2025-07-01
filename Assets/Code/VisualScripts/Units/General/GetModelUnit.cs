using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    public abstract class GetModelUnit<T> : ARUnit where T : class, IModel {
        protected override void Definition() {
            var gameObject = FallbackARValueInput("", flow => flow.stack.self);
            ValueOutput("", flow => VGUtils.TryGetModel<T>(gameObject.Value(flow)));
        }
    }
    
    [UnityEngine.Scripting.Preserve] public class GetCharacterUnit : GetModelUnit<ICharacter> { }
    [UnityEngine.Scripting.Preserve] public class GetIWithStatsUnit : GetModelUnit<IWithStats> { }
    [UnityEngine.Scripting.Preserve] public class GetIdleBehavioursUnit : GetModelUnit<IdleBehaviours> { }
    [UnityEngine.Scripting.Preserve] public class GetLocationUnit : GetModelUnit<Location> { }
    [UnityEngine.Scripting.Preserve] public class GetPortalUnit : GetModelUnit<Portal> { }
    [UnityEngine.Scripting.Preserve] public class GetShopUnit : GetModelUnit<Shop> { }

    [UnityEngine.Scripting.Preserve]
    public class GetHeroUnit : ARUnit {
        protected override void Definition() {
            ValueOutput("", _ => Hero.Current);
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    public class GetNpcLocationUnit : ARUnit {
        protected override void Definition() {
            var gameObject = FallbackARValueInput("", flow => flow.stack.self);
            ValueOutput("", flow => {
                Location location = VGUtils.GetModel<Location>(gameObject.Value(flow));
                if (location != null) {
                    return location.TryGetElement(out NpcPresence presence) 
                        ? presence.AliveNpc?.ParentModel ?? location 
                        : location;
                }
                Log.Important?.Error($"Failed to fetch Npc from: {gameObject}");
                return null;
            });
        }
    }
}