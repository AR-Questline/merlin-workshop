using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Fights;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ExplodeDeadBody : DealDamageInSphereOverTimeUnit, ISkillUnit {
        RequiredValueInput<ShareableARAssetReference> _explosionVFX;
        RequiredValueInput<NpcDummy> _npcDummy;
        FallbackValueInput<bool> _dropOnlyImportantItems;

        protected override void Definition() {
            _npcDummy = RequiredARValueInput<NpcDummy>("npcDummy");
            _explosionVFX = RequiredARValueInput<ShareableARAssetReference>("explosionVFX");
            _dropOnlyImportantItems = FallbackARValueInput<bool>("dropOnlyImportantItems", _ => false);
            base.Definition();
        }
        
        protected override ControlOutput Enter(Flow flow) {
            var exit = base.Enter(flow);
            NpcDummy npcDummy = _npcDummy.Value(flow);
            Location location = npcDummy.ParentModel;
            PrefabPool.InstantiateAndReturn(_explosionVFX.Value(flow), location.Coords, location.Rotation).Forget();
            location.TryGetElement<SearchAction>()?.DropAllItemsAndDiscard(_dropOnlyImportantItems.Value(flow));
            location.Discard();
            return exit;
        }
    }
}