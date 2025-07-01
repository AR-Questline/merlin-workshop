using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.VisualScripts.Units;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SpawnVFX : ARUnit, ISkillUnit {
        public enum SpawnType {
            WithDuration = 0,
            Unlimited= 1,
        }
        
        [Serialize, Inspectable, UnitHeaderInspectable]
        public SpawnType VFXSpawnType { get; set; } = SpawnType.WithDuration;
        
        protected override void Definition() {
            FallbackValueInput<int> duration = null;
            if (VFXSpawnType == SpawnType.WithDuration) {
                duration = FallbackARValueInput("duration", _ => 3);
            }
            var pos = FallbackARValueInput("position", _ => Vector3.zero);
            var rot = FallbackARValueInput("rotation", _ => Quaternion.identity);
            var parent = FallbackARValueInput("parent", f => this.Skill(f).Owner.ParentTransform);
            var vfxEffect = RequiredARValueInput<ShareableARAssetReference>("vfxAssetReference");
            
            var vfxEffectWrapper = ValueOutput(typeof(MagicVFXWrapper), "vfxEffectWrapper");
            
            DefineSimpleAction("Enter", "Exit", flow => {
                ShareableARAssetReference vfx = vfxEffect.Value(flow);
                Vector3 position = pos.Value(flow);
                Quaternion rotation = rot.Value(flow);
                Transform par = parent.Value(flow);

                if (!vfx.IsSet) {
                    return;
                }

                UniTask<IPooledInstance> pooledInstanceUniTask;
                if (VFXSpawnType == SpawnType.Unlimited) {
                    pooledInstanceUniTask = PrefabPool.Instantiate(vfx, position, rotation, par);
                } else {
                    float dur = duration!.Value(flow);
                    pooledInstanceUniTask = PrefabPool.InstantiateAndReturn(vfx, position, rotation, dur, par);
                }
                var magicVfxWrapper = new MagicVFXWrapper(pooledInstanceUniTask);
                flow.SetValue(vfxEffectWrapper, magicVfxWrapper);
            });
        }
    }
}