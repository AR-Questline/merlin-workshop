using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Skills.Units.Effects {
    public partial class DeadBodiesHighlight : Element<Skill> {
        public sealed override bool IsNotSaved => true;

        readonly float _range;
        readonly Dictionary<NpcDummy, IPooledInstance> _dummyToVfxInstance = new();

        bool _updating;

        public DeadBodiesHighlight(float range) {
            _range = range;
        }

        protected override void OnInitialize() {
            UpdateVFXs().Forget();
        }

        async UniTaskVoid UpdateVFXs() {
            while (!this.HasBeenDiscarded) {
                var dummiesInRange = Services.Get<NpcGrid>()
                    .GetNpcDummiesInSphere(ParentModel.Owner.Coords, _range).ToArray();
                
                foreach ((NpcDummy npcDummy, IPooledInstance instance) in _dummyToVfxInstance
                             .Where(d => !dummiesInRange.Contains(d.Key)).ToArray()) {
                    instance.Return();
                    _dummyToVfxInstance.Remove(npcDummy);
                }

                foreach (var npcDummy in dummiesInRange) {
                    if (_dummyToVfxInstance.ContainsKey(npcDummy)) {
                        continue;
                    }

                    IPooledInstance pooledInstance = await PrefabPool.Instantiate(
                        CommonReferences.Get.deadBodyHighlightVfx,
                        Vector3.zero, Quaternion.identity, npcDummy.ParentTransform);

                    if (this.HasBeenDiscarded) {
                        pooledInstance?.Return();
                        return;
                    }

                    _dummyToVfxInstance[npcDummy] = pooledInstance;
                }

                await UniTask.DelayFrame(1);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _dummyToVfxInstance.Values.ForEach(i => i.Return());
            _dummyToVfxInstance.Clear();
        }
    }
}