using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillSpawnLocation : ARUnit, ISkillUnit {
        protected override void Definition() {
            var locTemplate = RequiredARValueInput<TemplateWrapper<LocationTemplate>>("locationTemplate");
            var pos = FallbackARValueInput("position", _ => Vector3.zero);
            var rot = FallbackARValueInput("rotation", _ => Quaternion.identity);
            var assetReferenceOverride = FallbackARValueInput<ARAssetReference>("assetReferenceOverride", _ => null);
            var overridenLocationName = FallbackARValueInput("overridenLocationName", _ => string.Empty);
            var spawnVFX = FallbackARValueInput<ShareableARAssetReference>("spawnVFX", _ => null);
            var vfxDuration = FallbackARValueInput("vfxDuration", _ => 5f);

            ValueOutput spawnedLocation = ValueOutput<Location>("spawnedLocation");

            DefineSimpleAction("Enter", "Exit", flow => {
                Vector3 position = pos.Value(flow);
                LocationTemplate template = locTemplate.Value(flow).Template;
                if (template.TryGetComponent(out LocationSpec spec) && spec.snapToGround && AstarPath.active != null) {
                    NNInfo nnInfo = AstarPath.active.GetNearest(position);
                    position = nnInfo.node == null ? position : nnInfo.position;
                }
                Quaternion rotation = rot.Value(flow);
                Location location = template.SpawnLocation(position, rotation, null,
                    assetReferenceOverride.Value(flow), overridenLocationName.Value(flow));
                RepetitiveNpcUtils.Check(location);

                var vfx = spawnVFX.Value(flow);
                if (vfx != null) {
                    PrefabPool.InstantiateAndReturn(vfx, position, rotation, vfxDuration.Value(flow)).Forget();
                }
                
                flow.SetValue(spawnedLocation, location);
            });
        }
    }
}